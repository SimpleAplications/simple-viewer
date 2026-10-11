using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace ReiksViewer;

/// <summary>Converts decoded FFmpeg frames of any pixel format to 32-bit BGRA for display.</summary>
internal sealed unsafe class FrameScaler : IDisposable
{
    readonly byte*[] _destination = new byte*[4];
    readonly int[] _destinationStride = new int[4];
    readonly bool _assumeHdMatrix;
    SwsContext* _context;
    int _width, _height, _format = -1, _outWidth, _outHeight;
    // swscale's fast paths may write a few pixels past the end of each row, so it never
    // writes straight into the caller's memory: it gets this padded buffer instead.
    byte* _buffer;
    int _bufferStride;

    /// <param name="assumeHdMatrix">
    /// For frames that don't say which colour matrix they use: video of 720 lines or more is
    /// taken as BT.709, everything else (and all still pictures) as BT.601.
    /// </param>
    public FrameScaler(bool assumeHdMatrix) => _assumeHdMatrix = assumeHdMatrix;

    public void Convert(AVFrame* frame, byte* destination, int stride, int outWidth, int outHeight)
    {
        if (_context == null || frame->width != _width || frame->height != _height || frame->format != _format ||
            outWidth != _outWidth || outHeight != _outHeight)
        {
            Create(frame, outWidth, outHeight);
        }
        _destination[0] = _buffer;
        _destinationStride[0] = _bufferStride;
        ffmpeg.sws_scale(_context, frame->data.ToArray(), frame->linesize.ToArray(), 0, frame->height,
                         _destination, _destinationStride);
        long rowBytes = (long)outWidth * 4;
        for (int y = 0; y < outHeight; y++)
            Buffer.MemoryCopy(_buffer + (long)y * _bufferStride, destination + (long)y * stride, rowBytes, rowBytes);
    }

    void Create(AVFrame* frame, int outWidth, int outHeight)
    {
        ffmpeg.sws_freeContext(_context);
        _context = ffmpeg.sws_getContext(frame->width, frame->height, (AVPixelFormat)frame->format,
                                         outWidth, outHeight, AVPixelFormat.AV_PIX_FMT_BGRA,
                                         (int)SwsFlags.SWS_BILINEAR, null, null, null);
        if (_context == null) throw new InvalidOperationException("This picture format can't be displayed.");
        _width = frame->width;
        _height = frame->height;
        _format = frame->format;
        _outWidth = outWidth;
        _outHeight = outHeight;
        NativeMemory.AlignedFree(_buffer);
        _bufferStride = (outWidth * 4 + 63) & ~63;
        _buffer = (byte*)NativeMemory.AlignedAlloc((nuint)((long)_bufferStride * outHeight + 64), 64);

        // Without this everything is converted with the SD colour matrix and HD video looks slightly off.
        int matrix = frame->colorspace switch
        {
            AVColorSpace.AVCOL_SPC_BT709 => ffmpeg.SWS_CS_ITU709,
            AVColorSpace.AVCOL_SPC_BT470BG or AVColorSpace.AVCOL_SPC_SMPTE170M => ffmpeg.SWS_CS_ITU601,
            AVColorSpace.AVCOL_SPC_BT2020_NCL or AVColorSpace.AVCOL_SPC_BT2020_CL => ffmpeg.SWS_CS_BT2020,
            _ => _assumeHdMatrix && frame->height >= 720 ? ffmpeg.SWS_CS_ITU709 : ffmpeg.SWS_CS_ITU601,
        };
        var source = *(int_array4*)ffmpeg.sws_getCoefficients(matrix);
        var target = *(int_array4*)ffmpeg.sws_getCoefficients(ffmpeg.SWS_CS_DEFAULT);
        int fullRange = frame->color_range == AVColorRange.AVCOL_RANGE_JPEG ? 1 : 0;
        ffmpeg.sws_setColorspaceDetails(_context, in source, fullRange, in target, 1, 0, 1 << 16, 1 << 16);
    }

    public void Dispose()
    {
        ffmpeg.sws_freeContext(_context);
        _context = null;
        NativeMemory.AlignedFree(_buffer);
        _buffer = null;
    }
}
