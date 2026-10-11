using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FFmpeg.AutoGen;

namespace ReiksViewer;

/// <summary>Decodes still pictures and short animations (GIF, APNG, ...) with FFmpeg.</summary>
public static unsafe class FfmpegImage
{
    /// <param name="TooLong">
    /// The animation would not fit in memory as separate frames; play the file as a video instead.
    /// </param>
    public sealed record Result(BitmapSource[] Frames, TimeSpan[] Delays, bool TooLong);

    internal const long AnimationBudgetBytes = 400L * 1024 * 1024;
    const long MaxPixels = 400L * 1000 * 1000;

    public static Result Load(string path)
    {
        if (!FfmpegPlayer.EnsureLoaded())
            throw new InvalidOperationException(L.T("err.noffmpeg", FfmpegPlayer.LoadError));
        if (AnimatedWebP.IsAnimated(path)) return AnimatedWebP.Load(path);

        var format = FfmpegPlayer.OpenInput(path);
        try
        {
            FfmpegPlayer.Check(ffmpeg.avformat_find_stream_info(format, null), "Can't read the file");
            var group = FindTileGrid(format);
            return group != null
                ? new Result(new[] { DecodeTileGrid(format, group) }, new[] { TimeSpan.Zero }, false)
                : DecodeStream(format);
        }
        finally
        {
            ffmpeg.avformat_close_input(&format);
        }
    }

    static Result DecodeStream(AVFormatContext* format)
    {
        AVCodec* codec = null;
        int index = ffmpeg.av_find_best_stream(format, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &codec, 0);
        FfmpegPlayer.Check(index, "No picture found in the file");
        var stream = format->streams[index];
        double timeBase = ffmpeg.av_q2d(stream->time_base);
        int rotation = StreamRotation(stream->codecpar);

        var context = FfmpegPlayer.OpenCodec(codec, stream);
        var packet = ffmpeg.av_packet_alloc();
        var frame = ffmpeg.av_frame_alloc();
        using var scaler = new FrameScaler(assumeHdMatrix: false);
        var frames = new List<BitmapSource>();
        var delays = new List<TimeSpan>();
        int width = 0, height = 0;
        bool drained = false, tooLong = false;
        try
        {
            while (true)
            {
                int result = ffmpeg.avcodec_receive_frame(context, frame);
                if (result == 0)
                {
                    if (frames.Count == 0)
                    {
                        width = frame->width;
                        height = frame->height;
                        CheckSize(width, height);
                        int fromFrame = FrameRotation(frame);
                        if (fromFrame >= 0) rotation = fromFrame;
                    }
                    else if ((long)width * height * 4 * (frames.Count + 1) > AnimationBudgetBytes)
                    {
                        tooLong = true;
                        break;
                    }
                    frames.Add(ToBitmap(scaler, frame, width, height, rotation));
                    double milliseconds = frame->duration > 0 ? frame->duration * timeBase * 1000 : 100;
                    delays.Add(TimeSpan.FromMilliseconds(Math.Clamp(milliseconds, 20, 60_000)));
                    ffmpeg.av_frame_unref(frame);
                    continue;
                }
                if (result == ffmpeg.AVERROR_EOF || drained) break;

                // The decoder wants more data: feed it the next packet of our stream.
                while (true)
                {
                    if (ffmpeg.av_read_frame(format, packet) < 0)
                    {
                        ffmpeg.avcodec_send_packet(context, null);
                        drained = true;
                        break;
                    }
                    bool ours = packet->stream_index == index;
                    if (ours) ffmpeg.avcodec_send_packet(context, packet);
                    ffmpeg.av_packet_unref(packet);
                    if (ours) break;
                }
            }
        }
        finally
        {
            ffmpeg.av_frame_free(&frame);
            ffmpeg.av_packet_free(&packet);
            ffmpeg.avcodec_free_context(&context);
        }

        if (tooLong) return new Result(Array.Empty<BitmapSource>(), Array.Empty<TimeSpan>(), true);
        if (frames.Count == 0) throw new InvalidOperationException(L.T("err.decode"));
        return new Result(frames.ToArray(), delays.ToArray(), false);
    }

    static BitmapSource ToBitmap(FrameScaler scaler, AVFrame* frame, int width, int height, int rotation)
    {
        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        bitmap.Lock();
        try
        {
            scaler.Convert(frame, (byte*)bitmap.BackBuffer, bitmap.BackBufferStride, width, height);
            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }
        finally
        {
            bitmap.Unlock();
        }
        return Finish(bitmap, rotation);
    }

    static BitmapSource Finish(WriteableBitmap bitmap, int rotation)
    {
        bitmap.Freeze();
        if (rotation == 0) return bitmap;
        var rotated = new TransformedBitmap(bitmap, new RotateTransform(rotation));
        rotated.Freeze();
        return rotated;
    }

    internal static void CheckSize(int width, int height)
    {
        if (width <= 0 || height <= 0 || (long)width * height > MaxPixels)
            throw new InvalidOperationException($"The picture size {width} x {height} can't be shown.");
    }

    // ---------- Rotation metadata ----------

    /// <summary>Clockwise degrees (0, 90, 180, 270) from a display matrix, or -1 if there is none.</summary>
    static int Rotation(byte* matrix, ulong size)
    {
        if (matrix == null || size < 9 * sizeof(int)) return -1;
        double clockwise = -ffmpeg.av_display_rotation_get(in *(int_array9*)matrix);
        if (double.IsNaN(clockwise)) return -1;
        return (((int)Math.Round(clockwise / 90) * 90) % 360 + 360) % 360;
    }

    internal static int StreamRotation(AVCodecParameters* parameters) =>
        SideDataRotation(parameters->coded_side_data, parameters->nb_coded_side_data);

    static int SideDataRotation(AVPacketSideData* sideData, int count)
    {
        var matrix = ffmpeg.av_packet_side_data_get(sideData, count, AVPacketSideDataType.AV_PKT_DATA_DISPLAYMATRIX);
        return matrix == null ? 0 : Math.Max(Rotation(matrix->data, matrix->size), 0);
    }

    // Photos carry their orientation (EXIF) on the decoded frame, not on the stream.
    static int FrameRotation(AVFrame* frame)
    {
        var matrix = ffmpeg.av_frame_get_side_data(frame, AVFrameSideDataType.AV_FRAME_DATA_DISPLAYMATRIX);
        return matrix == null ? -1 : Rotation(matrix->data, matrix->size);
    }

    // ---------- Tiled pictures (HEIC photos are stored as a grid of small tiles) ----------

    static AVStreamGroup* FindTileGrid(AVFormatContext* format)
    {
        for (uint i = 0; i < format->nb_stream_groups; i++)
        {
            var group = format->stream_groups[i];
            if (group->type == AVStreamGroupParamsType.AV_STREAM_GROUP_PARAMS_TILE_GRID &&
                group->@params.tile_grid != null)
            {
                return group;
            }
        }
        return null;
    }

    static BitmapSource DecodeTileGrid(AVFormatContext* format, AVStreamGroup* group)
    {
        var grid = group->@params.tile_grid;
        int canvasWidth = grid->coded_width, canvasHeight = grid->coded_height;
        CheckSize(canvasWidth, canvasHeight);
        int canvasStride = canvasWidth * 4;

        // Decoder and position in the group for every tile stream, keyed by stream index.
        var decoders = new Dictionary<int, (IntPtr Context, uint Position)>();
        var packet = ffmpeg.av_packet_alloc();
        var frame = ffmpeg.av_frame_alloc();
        byte* canvas = (byte*)NativeMemory.AllocZeroed((nuint)((long)canvasStride * canvasHeight));
        byte* tile = null;
        long tileCapacity = 0;
        using var scaler = new FrameScaler(assumeHdMatrix: false);
        try
        {
            for (uint position = 0; position < group->nb_streams; position++)
            {
                var stream = group->streams[position];
                var codec = ffmpeg.avcodec_find_decoder(stream->codecpar->codec_id);
                decoders[stream->index] = ((IntPtr)FfmpegPlayer.OpenCodec(codec, stream, threads: 1), position);
            }

            void Drain(AVCodecContext* context, uint position)
            {
                while (ffmpeg.avcodec_receive_frame(context, frame) == 0)
                {
                    int width = frame->width, height = frame->height;
                    CheckSize(width, height);
                    long needed = (long)width * 4 * height;
                    if (needed > tileCapacity)
                    {
                        NativeMemory.Free(tile);
                        tile = (byte*)NativeMemory.Alloc((nuint)needed);
                        tileCapacity = needed;
                    }
                    scaler.Convert(frame, tile, width * 4, width, height);

                    for (uint k = 0; k < grid->nb_tiles; k++)
                    {
                        var offset = grid->offsets[k];
                        if (offset.idx != position) continue;
                        // Copy the part of the tile that falls inside the canvas.
                        int x0 = Math.Max(offset.horizontal, 0), y0 = Math.Max(offset.vertical, 0);
                        int x1 = Math.Min(offset.horizontal + width, canvasWidth);
                        int y1 = Math.Min(offset.vertical + height, canvasHeight);
                        if (x1 <= x0) continue;
                        for (int y = y0; y < y1; y++)
                        {
                            Buffer.MemoryCopy(
                                tile + (long)(y - offset.vertical) * width * 4 + (long)(x0 - offset.horizontal) * 4,
                                canvas + (long)y * canvasStride + (long)x0 * 4,
                                (x1 - x0) * 4, (x1 - x0) * 4);
                        }
                    }
                    ffmpeg.av_frame_unref(frame);
                }
            }

            while (ffmpeg.av_read_frame(format, packet) >= 0)
            {
                if (decoders.TryGetValue(packet->stream_index, out var decoder))
                {
                    ffmpeg.avcodec_send_packet((AVCodecContext*)decoder.Context, packet);
                    Drain((AVCodecContext*)decoder.Context, decoder.Position);
                }
                ffmpeg.av_packet_unref(packet);
            }
            foreach (var decoder in decoders.Values)
            {
                ffmpeg.avcodec_send_packet((AVCodecContext*)decoder.Context, null);
                Drain((AVCodecContext*)decoder.Context, decoder.Position);
            }

            // The visible picture is a window into the tile canvas.
            int left = grid->horizontal_offset, top = grid->vertical_offset;
            int outWidth = grid->width, outHeight = grid->height;
            if (left < 0 || top < 0 || outWidth <= 0 || outHeight <= 0 ||
                left + outWidth > canvasWidth || top + outHeight > canvasHeight)
            {
                left = top = 0;
                outWidth = canvasWidth;
                outHeight = canvasHeight;
            }

            var bitmap = new WriteableBitmap(outWidth, outHeight, 96, 96, PixelFormats.Bgra32, null);
            bitmap.Lock();
            try
            {
                byte* target = (byte*)bitmap.BackBuffer;
                for (int y = 0; y < outHeight; y++)
                {
                    Buffer.MemoryCopy(canvas + (long)(top + y) * canvasStride + (long)left * 4,
                                      target + (long)y * bitmap.BackBufferStride, outWidth * 4, outWidth * 4);
                }
                bitmap.AddDirtyRect(new Int32Rect(0, 0, outWidth, outHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
            return Finish(bitmap, SideDataRotation(grid->coded_side_data, grid->nb_coded_side_data));
        }
        finally
        {
            foreach (var decoder in decoders.Values)
            {
                var context = (AVCodecContext*)decoder.Context;
                ffmpeg.avcodec_free_context(&context);
            }
            NativeMemory.Free(tile);
            NativeMemory.Free(canvas);
            var scratch = frame;
            ffmpeg.av_frame_free(&scratch);
            ffmpeg.av_packet_free(&packet);
        }
    }
}
