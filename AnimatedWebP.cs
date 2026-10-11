using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FFmpeg.AutoGen;

namespace MediaViewer;

/// <summary>
/// Animated WebP. FFmpeg decodes WebP pictures but not the animation container, so this reads
/// the container itself, has FFmpeg decode each frame's picture, and composes the frames.
/// </summary>
internal static unsafe class AnimatedWebP
{
    const int AnimationFlag = 0x02, AlphaFlag = 0x10;

    public static bool IsAnimated(string path)
    {
        Span<byte> header = stackalloc byte[30];
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length) return false;
        return Tag(header, 0) == "RIFF" && Tag(header, 8) == "WEBP" && Tag(header, 12) == "VP8X" &&
               (header[20] & AnimationFlag) != 0;
    }

    public static FfmpegImage.Result Load(string path)
    {
        byte[] file = File.ReadAllBytes(path);
        int canvasWidth = 1 + U24(file, 24), canvasHeight = 1 + U24(file, 27);
        FfmpegImage.CheckSize(canvasWidth, canvasHeight);
        var canvas = new byte[(long)canvasWidth * canvasHeight * 4];
        var frames = new List<BitmapSource>();
        var delays = new List<TimeSpan>();

        var codec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_WEBP);
        var context = ffmpeg.avcodec_alloc_context3(codec);
        var packet = ffmpeg.av_packet_alloc();
        var frame = ffmpeg.av_frame_alloc();
        using var scaler = new FrameScaler(assumeHdMatrix: false);
        try
        {
            context->thread_count = 1;
            FfmpegPlayer.Check(ffmpeg.avcodec_open2(context, codec, null), "Can't start the WebP decoder");

            long position = 12;
            while (position + 8 <= file.Length)
            {
                long size = BitConverter.ToUInt32(file, (int)position + 4), data = position + 8;
                if (size > file.Length - data) break;
                if (Tag(file, (int)position) == "ANMF" && size > 16)
                {
                    if (frames.Count > 0 && canvas.LongLength * (frames.Count + 1) > FfmpegImage.AnimationBudgetBytes)
                        return new FfmpegImage.Result(Array.Empty<BitmapSource>(), Array.Empty<TimeSpan>(), true);

                    int at = (int)data;
                    int x = 2 * U24(file, at), y = 2 * U24(file, at + 3);
                    int width = 1 + U24(file, at + 6), height = 1 + U24(file, at + 9);
                    int duration = U24(file, at + 12);
                    bool disposeToBackground = (file[at + 15] & 1) != 0, overwrite = (file[at + 15] & 2) != 0;

                    var picture = Decode(context, packet, frame, scaler, file, at + 16, (int)size - 16, width, height);
                    if (picture != null) Compose(canvas, canvasWidth, canvasHeight, picture, x, y, width, height, overwrite);

                    var bitmap = BitmapSource.Create(canvasWidth, canvasHeight, 96, 96, PixelFormats.Bgra32, null,
                                                     canvas, canvasWidth * 4);
                    bitmap.Freeze();
                    frames.Add(bitmap);
                    // Browsers treat near-zero delays as 100 ms; do the same.
                    delays.Add(TimeSpan.FromMilliseconds(duration <= 10 ? 100 : duration));

                    if (disposeToBackground) Clear(canvas, canvasWidth, canvasHeight, x, y, width, height);
                }
                position = data + size + (size & 1);
            }
        }
        finally
        {
            ffmpeg.av_frame_free(&frame);
            ffmpeg.av_packet_free(&packet);
            ffmpeg.avcodec_free_context(&context);
        }

        if (frames.Count == 0) throw new InvalidOperationException("The animation has no frames that can be decoded.");
        return new FfmpegImage.Result(frames.ToArray(), delays.ToArray(), false);
    }

    /// <summary>Wraps one frame's chunks as a stand-alone WebP file and decodes it to BGRA.</summary>
    static byte[]? Decode(AVCodecContext* context, AVPacket* packet, AVFrame* frame, FrameScaler scaler,
                          byte[] file, int start, int length, int width, int height)
    {
        // A frame with a separate alpha chunk needs the extended header in front of it.
        bool extended = Tag(file, start) is not ("VP8 " or "VP8L");
        int headerLength = 12 + (extended ? 18 : 0);
        if (ffmpeg.av_new_packet(packet, headerLength + length) < 0) return null;

        var target = new Span<byte>(packet->data, headerLength + length);
        Encoding.ASCII.GetBytes("RIFF", target);
        BitConverter.TryWriteBytes(target[4..], (uint)(headerLength - 8 + length));
        Encoding.ASCII.GetBytes("WEBP", target[8..]);
        if (extended)
        {
            Encoding.ASCII.GetBytes("VP8X", target[12..]);
            BitConverter.TryWriteBytes(target[16..], 10u);
            target[20] = AlphaFlag;
            target[21] = target[22] = target[23] = 0;
            WriteU24(target[24..], width - 1);
            WriteU24(target[27..], height - 1);
        }
        file.AsSpan(start, length).CopyTo(target[headerLength..]);

        int sent = ffmpeg.avcodec_send_packet(context, packet);
        ffmpeg.av_packet_unref(packet);
        if (sent < 0 || ffmpeg.avcodec_receive_frame(context, frame) < 0) return null;

        var picture = new byte[(long)width * height * 4];
        fixed (byte* pixels = picture) scaler.Convert(frame, pixels, width * 4, width, height);
        ffmpeg.av_frame_unref(frame);
        return picture;
    }

    static void Compose(byte[] canvas, int canvasWidth, int canvasHeight,
                        byte[] picture, int left, int top, int width, int height, bool overwrite)
    {
        int rows = Math.Min(height, canvasHeight - top), columns = Math.Min(width, canvasWidth - left);
        for (int row = 0; row < rows; row++)
        {
            int source = row * width * 4, target = ((top + row) * canvasWidth + left) * 4;
            for (int column = 0; column < columns; column++, source += 4, target += 4)
            {
                int alpha = picture[source + 3];
                if (overwrite || alpha == 255)
                {
                    Buffer.BlockCopy(picture, source, canvas, target, 4);
                }
                else if (alpha != 0)
                {
                    // Standard "over" blending with straight (not premultiplied) alpha.
                    int below = canvas[target + 3] * (255 - alpha) / 255;
                    int outAlpha = alpha + below;
                    for (int c = 0; c < 3; c++)
                        canvas[target + c] = (byte)((picture[source + c] * alpha + canvas[target + c] * below) / outAlpha);
                    canvas[target + 3] = (byte)outAlpha;
                }
            }
        }
    }

    static void Clear(byte[] canvas, int canvasWidth, int canvasHeight, int left, int top, int width, int height)
    {
        int rows = Math.Min(height, canvasHeight - top), columns = Math.Min(width, canvasWidth - left);
        for (int row = 0; row < rows; row++)
            Array.Clear(canvas, ((top + row) * canvasWidth + left) * 4, columns * 4);
    }

    static string Tag(ReadOnlySpan<byte> bytes, int offset) => Encoding.ASCII.GetString(bytes.Slice(offset, 4));

    static int U24(byte[] bytes, int offset) => bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16;

    static void WriteU24(Span<byte> target, int value)
    {
        target[0] = (byte)value;
        target[1] = (byte)(value >> 8);
        target[2] = (byte)(value >> 16);
    }
}
