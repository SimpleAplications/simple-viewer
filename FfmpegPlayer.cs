using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FFmpeg.AutoGen;

namespace MediaViewer;

/// <summary>
/// Video player built on the FFmpeg libraries in the app's codecs folder.
/// One worker thread reads the file, decodes, keeps audio and video in sync and
/// answers seek/step commands. Decoded frames are kept in a small cache so that
/// stepping backwards and forwards around the current frame is instant.
/// </summary>
public sealed unsafe class FfmpegPlayer : IDisposable
{
    // ---------- Library loading ----------

    public static string CodecsFolder => Path.Combine(AppContext.BaseDirectory, "codecs");
    public static bool Available { get; private set; }
    public static string Version { get; private set; } = "";
    public static string License { get; private set; } = "";
    public static string LoadError { get; private set; } = "";

    static bool _loadAttempted;

    public static bool EnsureLoaded()
    {
        if (_loadAttempted) return Available;
        _loadAttempted = true;
        try
        {
            var probe = Path.Combine(CodecsFolder, $"avcodec-{ffmpeg.LibraryVersionMap["avcodec"]}.dll");
            if (!File.Exists(probe))
            {
                LoadError = $"{Path.GetFileName(probe)} was not found in {CodecsFolder}";
                return false;
            }
            ffmpeg.RootPath = CodecsFolder;
            Version = ffmpeg.av_version_info();
            License = ffmpeg.avutil_license();
            Available = true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or NotSupportedException
                                       or EntryPointNotFoundException or BadImageFormatException)
        {
            LoadError = ex.Message;
        }
        return Available;
    }

    // ---------- State ----------

    const int MaxQueuedPackets = 400;
    const long FrameCacheBytes = 256L * 1024 * 1024;
    const double AudioAhead = 0.2, RollForwardSeconds = 3, Epsilon = 1e-4;

    struct CachedFrame
    {
        public AVFrame* Frame;
        public double Pts;
    }

    [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint ms);
    [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint ms);

    readonly Dispatcher _dispatcher;
    readonly Thread _thread;
    readonly object _gate = new();
    readonly object _pixelLock = new();

    AVFormatContext* _format;
    AVCodecContext* _video, _audioCodec;
    AVPacket* _packet;
    AVFrame* _scratch, _audioFrame, _download;
    AVBufferRef* _hardware;
    bool _hardwareActive;
    int _presentedFrames, _droppedFrames;
    SwrContext* _swr;
    int _videoIndex, _audioIndex = -1;
    int _swrRate, _swrFormat, _swrChannels;
    double _videoTimeBase, _audioTimeBase, _timeOffset, _frameDuration;
    AudioOut? _audio;

    readonly Queue<IntPtr> _videoPackets = new();
    readonly List<CachedFrame> _frames = new();
    int _cursor = -1, _frameCap;
    bool _demuxEof, _drainSent, _decodeAudio, _playing, _needResync = true, _ended, _timerRaised;

    // Clock: the audio device's sample counter when there is sound, a stopwatch otherwise.
    readonly Stopwatch _wall = new();
    double _audioBase, _audioNextPts, _audioSkipUntil, _lastClock, _wallBase;
    bool _audioBaseSet, _wallActive;
    int _dropped;

    // Commands from the UI thread, guarded by _gate.
    bool _quit, _wantPlaying;
    double? _seekRequest;
    int _stepRequest;

    // Output picture, guarded by _pixelLock.
    readonly FrameScaler _scaler = new(assumeHdMatrix: true);
    byte* _pixels;
    bool _disposed;
    int _uiPending;
    double _position;
    double _volume = 1;
    bool _muted;

    public WriteableBitmap Bitmap { get; }
    public int Width { get; }
    public int Height { get; }
    /// <summary>Clockwise rotation in degrees the picture needs, from the file's metadata.</summary>
    public int Rotation { get; }
    public double Duration { get; }
    public double FrameDuration => _frameDuration;
    public double Position => Volatile.Read(ref _position);
    public bool HasAudio => _audio != null;
    /// <summary>True once a frame has actually been decoded by the graphics card.</summary>
    public bool HardwareDecoding => Volatile.Read(ref _hardwareActive);
    public int PresentedFrames => Volatile.Read(ref _presentedFrames);
    public int DroppedFrames => Volatile.Read(ref _droppedFrames);

    /// <summary>Raised on the UI thread when playback reaches the end.</summary>
    public event Action? Ended;
    /// <summary>Raised on the UI thread if decoding fails part-way.</summary>
    public event Action<string>? Failed;

    public FfmpegPlayer(string path, Dispatcher dispatcher)
    {
        if (!EnsureLoaded()) throw new InvalidOperationException("FFmpeg is not available: " + LoadError);
        _dispatcher = dispatcher;
        try
        {
            AVFormatContext* format = null;
            Check(ffmpeg.avformat_open_input(&format, path, null, null), "Can't open the file");
            _format = format;
            Check(ffmpeg.avformat_find_stream_info(_format, null), "Can't read the file's streams");

            AVCodec* videoCodec = null;
            _videoIndex = ffmpeg.av_find_best_stream(_format, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &videoCodec, 0);
            Check(_videoIndex, "No playable video track");
            var videoStream = _format->streams[_videoIndex];
            _hardware = TryCreateHardware(videoCodec);
            _video = OpenCodec(videoCodec, videoStream, 0, _hardware);
            _videoTimeBase = ffmpeg.av_q2d(videoStream->time_base);

            Width = _video->width;
            Height = _video->height;
            if (Width <= 0 || Height <= 0) throw new InvalidOperationException("The video has no picture size.");

            OpenAudio();
            UpdateGain();

            _timeOffset = _format->start_time != ffmpeg.AV_NOPTS_VALUE
                ? _format->start_time / (double)ffmpeg.AV_TIME_BASE : 0;
            Duration = _format->duration > 0 ? _format->duration / (double)ffmpeg.AV_TIME_BASE : 0;

            var rate = ffmpeg.av_guess_frame_rate(_format, videoStream, null);
            _frameDuration = rate.num > 0 && rate.den > 0 ? (double)rate.den / rate.num : 1.0 / 30;
            _frameDuration = Math.Clamp(_frameDuration, 1.0 / 240, 1);

            long frameBytes = ffmpeg.av_image_get_buffer_size(_video->pix_fmt, Width, Height, 1);
            if (frameBytes <= 0) frameBytes = (long)Width * Height * 3 / 2;
            _frameCap = (int)Math.Clamp(FrameCacheBytes / frameBytes, 4, 120);

            Rotation = FfmpegImage.StreamRotation(videoStream->codecpar);

            _packet = ffmpeg.av_packet_alloc();
            _scratch = ffmpeg.av_frame_alloc();
            _audioFrame = ffmpeg.av_frame_alloc();
            _download = ffmpeg.av_frame_alloc();

            _pixels = (byte*)NativeMemory.AllocZeroed((nuint)((long)Width * 4 * Height));
            Bitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Bgr32, null);
        }
        catch
        {
            FreeNative();
            throw;
        }

        _thread = new Thread(Run) { IsBackground = true, Name = "FFmpeg player" };
        _thread.Start();
    }

    // ---------- Graphics card decoding ----------

    /// <summary>Set to false (the --software switch) to decode on the processor only.</summary>
    public static bool UseHardware { get; set; } = true;

    // Kept in a field so the callback FFmpeg holds a pointer to is never collected.
    static readonly AVCodecContext_get_format HardwareFormatCallback = ChooseHardwareFormat;

    /// <summary>
    /// Opens the graphics card's decoder (Direct3D 11) if FFmpeg can use it for this codec.
    /// Returns null when it can't; the video is then decoded on the processor.
    /// </summary>
    static AVBufferRef* TryCreateHardware(AVCodec* codec)
    {
        const int HardwareDeviceMethod = 0x01; // AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX
        if (!UseHardware) return null;
        for (int i = 0; ; i++)
        {
            var config = ffmpeg.avcodec_get_hw_config(codec, i);
            if (config == null) return null;
            if (config->device_type == AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA &&
                config->pix_fmt == AVPixelFormat.AV_PIX_FMT_D3D11 &&
                (config->methods & HardwareDeviceMethod) != 0)
            {
                break;
            }
        }
        AVBufferRef* device = null;
        return ffmpeg.av_hwdevice_ctx_create(&device, AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA, null, null, 0) >= 0
            ? device : null;
    }

    // FFmpeg offers the formats it can decode this stream to. Take the graphics card one when
    // it is on the list; when the card can't handle the stream it isn't, and software is used.
    static AVPixelFormat ChooseHardwareFormat(AVCodecContext* context, AVPixelFormat* formats)
    {
        for (var format = formats; *format != AVPixelFormat.AV_PIX_FMT_NONE; format++)
        {
            if (*format == AVPixelFormat.AV_PIX_FMT_D3D11) return *format;
        }
        return ffmpeg.avcodec_default_get_format(context, formats);
    }

    /// <summary>
    /// A frame decoded on the graphics card lives in video memory; copy it to normal memory so
    /// the rest of the player (frame cache, stepping, drawing) treats it like any other frame.
    /// </summary>
    bool DownloadFromGraphicsCard()
    {
        if (_scratch->format != (int)AVPixelFormat.AV_PIX_FMT_D3D11) return true;
        if (ffmpeg.av_hwframe_transfer_data(_download, _scratch, 0) < 0 ||
            ffmpeg.av_frame_copy_props(_download, _scratch) < 0)
        {
            ffmpeg.av_frame_unref(_download);
            ffmpeg.av_frame_unref(_scratch);
            return false;
        }
        ffmpeg.av_frame_unref(_scratch);
        ffmpeg.av_frame_move_ref(_scratch, _download);
        Volatile.Write(ref _hardwareActive, true);
        return true;
    }

    internal static AVCodecContext* OpenCodec(AVCodec* codec, AVStream* stream, int threads = 0,
                                              AVBufferRef* hardware = null)
    {
        var context = ffmpeg.avcodec_alloc_context3(codec);
        try
        {
            Check(ffmpeg.avcodec_parameters_to_context(context, stream->codecpar), "Can't read the codec settings");
            context->pkt_timebase = stream->time_base;
            context->thread_count = threads;
            if (hardware != null)
            {
                context->hw_device_ctx = ffmpeg.av_buffer_ref(hardware);
                context->get_format = HardwareFormatCallback;
            }
            Check(ffmpeg.avcodec_open2(context, codec, null), "Can't start the decoder");
            return context;
        }
        catch
        {
            ffmpeg.avcodec_free_context(&context);
            throw;
        }
    }

    void OpenAudio()
    {
        AVCodec* codec = null;
        int index = ffmpeg.av_find_best_stream(_format, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, _videoIndex, &codec, 0);
        if (index < 0) return;
        var stream = _format->streams[index];
        AVCodecContext* context;
        try
        {
            context = OpenCodec(codec, stream);
        }
        catch (InvalidOperationException)
        {
            return; // Unsupported sound track: play the picture silently.
        }
        _audio = context->sample_rate > 0 ? AudioOut.TryOpen(context->sample_rate, 2) : null;
        if (_audio == null)
        {
            ffmpeg.avcodec_free_context(&context);
            return;
        }
        _audioCodec = context;
        _audioIndex = index;
        _audioTimeBase = ffmpeg.av_q2d(stream->time_base);
    }

    internal static void Check(int result, string what)
    {
        if (result >= 0) return;
        var buffer = stackalloc byte[256];
        ffmpeg.av_strerror(result, buffer, 256);
        throw new InvalidOperationException($"{what}: {Marshal.PtrToStringUTF8((IntPtr)buffer)}");
    }

    // ---------- Commands (UI thread) ----------

    public bool IsPlaying
    {
        get { lock (_gate) return _wantPlaying; }
    }

    public void Play() => SetWantPlaying(true);

    public void Pause() => SetWantPlaying(false);

    void SetWantPlaying(bool value)
    {
        lock (_gate)
        {
            _wantPlaying = value;
            Monitor.PulseAll(_gate);
        }
    }

    /// <summary>Shows the frame that is on screen at the given time. Only the latest request is kept.</summary>
    public void Seek(double seconds)
    {
        lock (_gate)
        {
            _seekRequest = seconds;
            Monitor.PulseAll(_gate);
        }
    }

    /// <summary>Pauses and moves by whole frames; negative values go backwards.</summary>
    public void Step(int frames)
    {
        lock (_gate)
        {
            _wantPlaying = false;
            _stepRequest += frames;
            Monitor.PulseAll(_gate);
        }
    }

    public double Volume
    {
        get => _volume;
        set { _volume = Math.Clamp(value, 0, 1); UpdateGain(); }
    }

    public bool Muted
    {
        get => _muted;
        set { _muted = value; UpdateGain(); }
    }

    // Squared so the slider feels even to the ear. Set on the output device, so it is heard
    // immediately rather than after the queued sound has played out.
    void UpdateGain() => _audio?.SetVolume(_muted ? 0 : _volume * _volume);

    // ---------- Worker thread ----------

    void Run()
    {
        try
        {
            if (DecodeAppend())
            {
                _cursor = 0;
                Present();
            }
            while (true)
            {
                double? seek;
                int steps;
                bool want;
                lock (_gate)
                {
                    while (!_quit && !_playing && !CommandPending) Monitor.Wait(_gate);
                    if (_quit) return;
                    seek = _seekRequest;
                    _seekRequest = null;
                    steps = _stepRequest;
                    _stepRequest = 0;
                    want = _wantPlaying;
                }

                if (seek != null && DoSeek(seek.Value)) Present();
                if (steps != 0)
                {
                    if (_playing) StopPlaying();
                    for (int direction = Math.Sign(steps); steps != 0; steps -= direction) DoStep(direction);
                    Present();
                }
                if (want != _playing)
                {
                    if (want) StartPlaying(); else StopPlaying();
                }
                if (_playing) PlayTick();
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or OutOfMemoryException)
        {
            var message = ex.Message;
            _dispatcher.BeginInvoke(() => { if (!_disposed) Failed?.Invoke(message); });
        }
        finally
        {
            RaiseTimer(false);
        }
    }

    // Caller holds _gate.
    bool CommandPending => _quit || _seekRequest != null || _stepRequest != 0 || _wantPlaying != _playing;

    bool Interrupted
    {
        get { lock (_gate) return _quit || _seekRequest != null; }
    }

    void StartPlaying()
    {
        if (_cursor < 0)
        {
            lock (_gate) _wantPlaying = false;
            return;
        }
        _decodeAudio = _audio != null;
        if (_needResync || _ended)
        {
            // Frames were stepped through while paused, so the sound has to be lined up again.
            SeekDecode(_ended ? 0 : _frames[_cursor].Pts);
            Present();
        }
        _needResync = false;
        _ended = false;
        _dropped = 0;
        RaiseTimer(true);
        _audio?.Resume();
        _playing = true;
    }

    void StopPlaying()
    {
        _lastClock = Clock();
        _playing = false;
        _decodeAudio = false;
        _audio?.Pause();
        _wall.Stop();
        _wallActive = false;
        RaiseTimer(false);
    }

    // Windows sleeps in ~15 ms steps unless asked for finer timing; frames need better.
    void RaiseTimer(bool on)
    {
        if (on == _timerRaised) return;
        _timerRaised = on;
        if (on) timeBeginPeriod(1); else timeEndPeriod(1);
    }

    void PlayTick()
    {
        if (_audio != null)
        {
            while (!_demuxEof && _videoPackets.Count < MaxQueuedPackets && _audio.QueuedSeconds < AudioAhead)
                Demux();
        }

        if (_cursor + 1 >= _frames.Count && !DecodeAppend())
        {
            StopPlaying();
            _ended = true;
            lock (_gate) _wantPlaying = false;
            _dispatcher.BeginInvoke(() => { if (!_disposed) Ended?.Invoke(); });
            return;
        }

        double delay = _frames[_cursor + 1].Pts - Clock();
        if (delay > 0.001)
        {
            lock (_gate)
            {
                if (!CommandPending) Monitor.Wait(_gate, (int)Math.Clamp(delay * 1000, 1, 10));
            }
            return;
        }

        _cursor++;
        EvictBehindCursor();
        // When decoding can't keep up, skip drawing some frames instead of drifting behind the sound.
        if (delay > -0.1 || ++_dropped >= 8)
        {
            _dropped = 0;
            Present();
            Volatile.Write(ref _presentedFrames, _presentedFrames + 1);
        }
        else
        {
            Volatile.Write(ref _droppedFrames, _droppedFrames + 1);
        }
    }

    double Clock()
    {
        if (_audio != null)
        {
            bool finished = _demuxEof && _audio.QueuedSeconds <= 0;
            if (_audioBaseSet && !finished)
            {
                _wallActive = false;
                return _lastClock = _audioBase + _audio.PlayedSeconds;
            }
            // Sound is about to arrive: hold the picture until it does.
            if (!_audioBaseSet && !_demuxEof && _videoPackets.Count < MaxQueuedPackets)
                return _lastClock;
        }
        if (!_wallActive)
        {
            _wallActive = true;
            _wallBase = _lastClock;
            _wall.Restart();
        }
        return _lastClock = _wallBase + _wall.Elapsed.TotalSeconds;
    }

    bool DoSeek(double target)
    {
        target = Math.Max(target, 0);
        if (Duration > 0) target = Math.Min(target, Duration);
        _ended = false;

        if (!_playing && _frames.Count > 0)
        {
            _needResync = true;
            double first = _frames[0].Pts, last = _frames[^1].Pts;
            if (target >= first && target < last)
            {
                // Already decoded: just move within the cache.
                for (int i = _frames.Count - 1; i >= 0; i--)
                {
                    if (_frames[i].Pts > target + Epsilon) continue;
                    _cursor = i;
                    break;
                }
                return true;
            }
            if (target >= last && target - last < RollForwardSeconds)
            {
                // A short hop ahead: keep decoding instead of jumping to a keyframe.
                while (true)
                {
                    if (!DecodeAppend())
                    {
                        _cursor = _frames.Count - 1;
                        return true;
                    }
                    EvictOldest();
                    if (_frames[^1].Pts > target + Epsilon)
                    {
                        _cursor = _frames.Count - 2;
                        return true;
                    }
                    if (Interrupted)
                    {
                        _cursor = _frames.Count - 1;
                        return false;
                    }
                }
            }
        }

        if (!_playing) _needResync = true;
        return SeekDecode(target);
    }

    void DoStep(int direction)
    {
        _ended = false;
        _needResync = true;
        if (direction > 0)
        {
            if (_cursor + 1 < _frames.Count || DecodeAppend())
            {
                _cursor++;
                EvictBehindCursor();
            }
        }
        else if (_cursor > 0)
        {
            _cursor--;
        }
        else if (_frames.Count > 0)
        {
            // Out of cached frames: decode the stretch before this one.
            SeekDecode(_frames[0].Pts - 1e-3);
        }
    }

    /// <summary>
    /// Jumps to the keyframe before <paramref name="target"/> and decodes forward until the frame
    /// shown at that time is reached, so the picture is always complete. Returns false if a newer
    /// request cut it short.
    /// </summary>
    bool SeekDecode(double target)
    {
        // Files are searched in decode order, and with reordered frames that can land on a
        // keyframe that is *shown* after the target. When that happens, start further back.
        double from = Math.Max(target, 0);
        for (double backoff = 0.5; ; backoff *= 2)
        {
            bool complete = SeekDecodeFrom(from, target, out double firstPts);
            if (!complete || firstPts <= target + Epsilon || from <= 0) return complete;
            from = Math.Max(Math.Min(from, firstPts) - backoff, 0);
        }
    }

    bool SeekDecodeFrom(double from, double target, out double firstPts)
    {
        ClearFrames();
        ClearPackets();
        firstPts = double.NegativeInfinity;

        double seconds = from + _timeOffset;
        if (ffmpeg.av_seek_frame(_format, _videoIndex, (long)Math.Floor(seconds / _videoTimeBase),
                                 ffmpeg.AVSEEK_FLAG_BACKWARD) < 0)
        {
            ffmpeg.av_seek_frame(_format, -1, (long)(seconds * ffmpeg.AV_TIME_BASE), ffmpeg.AVSEEK_FLAG_BACKWARD);
        }
        ffmpeg.avcodec_flush_buffers(_video);
        if (_audioCodec != null) ffmpeg.avcodec_flush_buffers(_audioCodec);
        _demuxEof = false;
        _drainSent = false;

        _audio?.Reset(paused: !_playing);
        _audioBaseSet = false;
        _audioSkipUntil = target;
        _wallActive = false;

        bool complete = true;
        while (DecodeAppend())
        {
            if (double.IsNegativeInfinity(firstPts)) firstPts = _frames[^1].Pts;
            EvictOldest();
            if (_frames[^1].Pts > target + Epsilon) break;
            if (Interrupted)
            {
                complete = false;
                break;
            }
        }

        // The last frame decoded is the first one past the target; the one before it is on screen.
        _cursor = _frames.Count >= 2 && _frames[^1].Pts > target + Epsilon ? _frames.Count - 2 : _frames.Count - 1;
        _lastClock = _cursor >= 0 ? _frames[_cursor].Pts : target;
        return complete;
    }

    // ---------- Reading and decoding ----------

    bool Demux()
    {
        if (ffmpeg.av_read_frame(_format, _packet) < 0)
        {
            _demuxEof = true;
            return false;
        }
        if (_packet->stream_index == _videoIndex)
        {
            var copy = ffmpeg.av_packet_clone(_packet);
            if (copy != null) _videoPackets.Enqueue((IntPtr)copy);
        }
        else if (_packet->stream_index == _audioIndex && _decodeAudio)
        {
            DecodeAudio(_packet);
        }
        ffmpeg.av_packet_unref(_packet);
        return true;
    }

    /// <summary>Decodes the next video frame and adds it to the end of the cache.</summary>
    bool DecodeAppend()
    {
        while (true)
        {
            int result = ffmpeg.avcodec_receive_frame(_video, _scratch);
            if (result == 0)
            {
                if (!DownloadFromGraphicsCard()) continue;
                var frame = ffmpeg.av_frame_alloc();
                ffmpeg.av_frame_move_ref(frame, _scratch);
                long timestamp = frame->best_effort_timestamp;
                double pts = timestamp != ffmpeg.AV_NOPTS_VALUE
                    ? timestamp * _videoTimeBase - _timeOffset
                    : _frames.Count > 0 ? _frames[^1].Pts + _frameDuration : _lastClock;
                _frames.Add(new CachedFrame { Frame = frame, Pts = pts });
                return true;
            }
            if (result == ffmpeg.AVERROR_EOF) return false;

            // The decoder wants more data.
            if (_videoPackets.Count > 0)
            {
                var packet = (AVPacket*)_videoPackets.Dequeue();
                ffmpeg.avcodec_send_packet(_video, packet);
                ffmpeg.av_packet_free(&packet);
            }
            else if (!_demuxEof)
            {
                Demux();
            }
            else if (!_drainSent)
            {
                _drainSent = true;
                ffmpeg.avcodec_send_packet(_video, null);
            }
            else
            {
                return false;
            }
        }
    }

    void DecodeAudio(AVPacket* packet)
    {
        if (ffmpeg.avcodec_send_packet(_audioCodec, packet) < 0) return;
        while (ffmpeg.avcodec_receive_frame(_audioCodec, _audioFrame) == 0)
        {
            var frame = _audioFrame;
            long timestamp = frame->best_effort_timestamp;
            double pts = timestamp != ffmpeg.AV_NOPTS_VALUE ? timestamp * _audioTimeBase - _timeOffset : _audioNextPts;
            double length = frame->sample_rate > 0 ? (double)frame->nb_samples / frame->sample_rate : 0;
            _audioNextPts = pts + length;

            // After a seek, sound from before the frame on screen is thrown away.
            if (pts + length > _audioSkipUntil && EnsureResampler(frame))
            {
                int capacity = ffmpeg.swr_get_out_samples(_swr, frame->nb_samples);
                if (capacity > 0)
                {
                    var block = AudioOut.Allocate(capacity * 4);
                    byte* data = AudioOut.DataOf(block);
                    int samples = ffmpeg.swr_convert(_swr, &data, capacity, frame->extended_data, frame->nb_samples);
                    if (samples > 0)
                    {
                        if (!_audioBaseSet)
                        {
                            _audioBase = pts;
                            _audioBaseSet = true;
                        }
                        _audio!.Write(block, samples * 4);
                    }
                    else
                    {
                        AudioOut.Free(block);
                    }
                }
            }
            ffmpeg.av_frame_unref(frame);
        }
    }

    bool EnsureResampler(AVFrame* frame)
    {
        int channels = frame->ch_layout.nb_channels;
        if (_swr != null && frame->sample_rate == _swrRate && frame->format == _swrFormat && channels == _swrChannels)
            return true;

        var resampler = _swr;
        ffmpeg.swr_free(&resampler);
        _swr = null;
        if (channels <= 0 || frame->sample_rate <= 0) return false;

        AVChannelLayout output, input = frame->ch_layout;
        ffmpeg.av_channel_layout_default(&output, 2);
        if (input.order == AVChannelOrder.AV_CHANNEL_ORDER_UNSPEC)
            ffmpeg.av_channel_layout_default(&input, channels);

        if (ffmpeg.swr_alloc_set_opts2(&resampler, &output, AVSampleFormat.AV_SAMPLE_FMT_S16, _audio!.SampleRate,
                                       &input, (AVSampleFormat)frame->format, frame->sample_rate, 0, null) < 0 ||
            ffmpeg.swr_init(resampler) < 0)
        {
            ffmpeg.swr_free(&resampler);
            return false;
        }
        _swr = resampler;
        _swrRate = frame->sample_rate;
        _swrFormat = frame->format;
        _swrChannels = channels;
        return true;
    }

    // ---------- Frame cache ----------

    void EvictOldest()
    {
        while (_frames.Count > _frameCap) RemoveFirstFrame();
    }

    void EvictBehindCursor()
    {
        while (_frames.Count > _frameCap && _cursor > 0)
        {
            RemoveFirstFrame();
            _cursor--;
        }
    }

    void RemoveFirstFrame()
    {
        var frame = _frames[0].Frame;
        ffmpeg.av_frame_free(&frame);
        _frames.RemoveAt(0);
    }

    void ClearFrames()
    {
        foreach (var cached in _frames)
        {
            var frame = cached.Frame;
            ffmpeg.av_frame_free(&frame);
        }
        _frames.Clear();
        _cursor = -1;
    }

    void ClearPackets()
    {
        while (_videoPackets.Count > 0)
        {
            var packet = (AVPacket*)_videoPackets.Dequeue();
            ffmpeg.av_packet_free(&packet);
        }
    }

    // ---------- Drawing ----------

    void Present()
    {
        if (_cursor < 0) return;
        var cached = _frames[_cursor];
        var frame = cached.Frame;
        lock (_pixelLock)
        {
            if (_disposed) return;
            _scaler.Convert(frame, _pixels, Width * 4, Width, Height);
        }
        Volatile.Write(ref _position, cached.Pts);
        if (Interlocked.Exchange(ref _uiPending, 1) == 0)
            _dispatcher.BeginInvoke(DispatcherPriority.Render, CopyToBitmap);
    }

    void CopyToBitmap()
    {
        Interlocked.Exchange(ref _uiPending, 0);
        lock (_pixelLock)
        {
            if (_disposed) return;
            Bitmap.WritePixels(new Int32Rect(0, 0, Width, Height), (IntPtr)_pixels,
                               Width * 4 * Height, Width * 4);
        }
    }

    // ---------- Shutdown ----------

    public void Dispose()
    {
        lock (_gate)
        {
            if (_quit) return;
            _quit = true;
            Monitor.PulseAll(_gate);
        }
        bool stopped = _thread.Join(5000);
        lock (_pixelLock) _disposed = true;
        // If the worker is stuck inside FFmpeg, leaking is safer than freeing under it.
        if (stopped) FreeNative();
    }

    void FreeNative()
    {
        _audio?.Dispose();
        _audio = null;
        ClearFrames();
        ClearPackets();

        _scaler.Dispose();
        var resampler = _swr;
        ffmpeg.swr_free(&resampler);
        _swr = null;
        var scratch = _scratch;
        ffmpeg.av_frame_free(&scratch);
        _scratch = null;
        var audioFrame = _audioFrame;
        ffmpeg.av_frame_free(&audioFrame);
        _audioFrame = null;
        var packet = _packet;
        ffmpeg.av_packet_free(&packet);
        _packet = null;
        var video = _video;
        ffmpeg.avcodec_free_context(&video);
        _video = null;
        var audioCodec = _audioCodec;
        ffmpeg.avcodec_free_context(&audioCodec);
        _audioCodec = null;
        var download = _download;
        ffmpeg.av_frame_free(&download);
        _download = null;
        var hardware = _hardware;
        ffmpeg.av_buffer_unref(&hardware);
        _hardware = null;
        var format = _format;
        ffmpeg.avformat_close_input(&format);
        _format = null;

        NativeMemory.Free(_pixels);
        _pixels = null;
    }
}
