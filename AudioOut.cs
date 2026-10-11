using System.Runtime.InteropServices;

namespace MediaViewer;

/// <summary>
/// 16-bit PCM output through the Windows waveOut API. Blocks are queued without
/// copying, and the device's own sample counter serves as the playback clock.
/// </summary>
public sealed unsafe class AudioOut : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    struct WaveFormat
    {
        public ushort FormatTag, Channels;
        public uint SamplesPerSec, AvgBytesPerSec;
        public ushort BlockAlign, BitsPerSample, Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WaveHeader
    {
        public IntPtr Data;
        public uint BufferLength, BytesRecorded;
        public IntPtr User;
        public uint Flags, Loops;
        public IntPtr Next, Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MmTime
    {
        public uint Type, Value, Padding;
    }

    const uint WaveMapper = 0xFFFFFFFF, HeaderDone = 1, TimeMs = 1, TimeSamples = 2, TimeBytes = 4;

    [DllImport("winmm.dll")] static extern int waveOutOpen(out IntPtr handle, uint device, ref WaveFormat format, IntPtr callback, IntPtr instance, uint flags);
    [DllImport("winmm.dll")] static extern int waveOutPrepareHeader(IntPtr handle, WaveHeader* header, int size);
    [DllImport("winmm.dll")] static extern int waveOutUnprepareHeader(IntPtr handle, WaveHeader* header, int size);
    [DllImport("winmm.dll")] static extern int waveOutWrite(IntPtr handle, WaveHeader* header, int size);
    [DllImport("winmm.dll")] static extern int waveOutPause(IntPtr handle);
    [DllImport("winmm.dll")] static extern int waveOutRestart(IntPtr handle);
    [DllImport("winmm.dll")] static extern int waveOutReset(IntPtr handle);
    [DllImport("winmm.dll")] static extern int waveOutClose(IntPtr handle);
    [DllImport("winmm.dll")] static extern int waveOutGetPosition(IntPtr handle, ref MmTime time, int size);
    [DllImport("winmm.dll")] static extern int waveOutSetVolume(IntPtr handle, uint volume);

    readonly Queue<IntPtr> _blocks = new();
    readonly int _bytesPerSecond;
    IntPtr _handle;
    long _queuedBytes;

    public int SampleRate { get; }
    public int Channels { get; }

    AudioOut(IntPtr handle, int sampleRate, int channels)
    {
        _handle = handle;
        SampleRate = sampleRate;
        Channels = channels;
        _bytesPerSecond = sampleRate * channels * 2;
    }

    /// <summary>Returns null when no audio device is available.</summary>
    public static AudioOut? TryOpen(int sampleRate, int channels)
    {
        var format = new WaveFormat
        {
            FormatTag = 1,
            Channels = (ushort)channels,
            SamplesPerSec = (uint)sampleRate,
            AvgBytesPerSec = (uint)(sampleRate * channels * 2),
            BlockAlign = (ushort)(channels * 2),
            BitsPerSample = 16,
        };
        return waveOutOpen(out var handle, WaveMapper, ref format, IntPtr.Zero, IntPtr.Zero, 0) == 0
            ? new AudioOut(handle, sampleRate, channels)
            : null;
    }

    /// <summary>Allocates a block; the PCM data area starts at <see cref="DataOf"/>.</summary>
    public static IntPtr Allocate(int dataBytes) =>
        (IntPtr)NativeMemory.Alloc((nuint)(sizeof(WaveHeader) + dataBytes));

    public static byte* DataOf(IntPtr block) => (byte*)block + sizeof(WaveHeader);

    public static void Free(IntPtr block) => NativeMemory.Free((void*)block);

    /// <summary>Queues a block from <see cref="Allocate"/>; ownership passes to this object.</summary>
    public void Write(IntPtr block, int dataBytes)
    {
        var header = (WaveHeader*)block;
        *header = new WaveHeader { Data = (IntPtr)DataOf(block), BufferLength = (uint)dataBytes };
        if (waveOutPrepareHeader(_handle, header, sizeof(WaveHeader)) != 0 ||
            waveOutWrite(_handle, header, sizeof(WaveHeader)) != 0)
        {
            Free(block);
            return;
        }
        _blocks.Enqueue(block);
        _queuedBytes += dataBytes;
    }

    /// <summary>Releases blocks the device has finished playing.</summary>
    public void Collect()
    {
        while (_blocks.Count > 0)
        {
            var header = (WaveHeader*)_blocks.Peek();
            if ((header->Flags & HeaderDone) == 0) break;
            waveOutUnprepareHeader(_handle, header, sizeof(WaveHeader));
            _queuedBytes -= header->BufferLength;
            Free(_blocks.Dequeue());
        }
    }

    public double QueuedSeconds
    {
        get
        {
            Collect();
            return (double)_queuedBytes / _bytesPerSecond;
        }
    }

    /// <summary>Seconds of audio actually played since the last <see cref="Reset"/>.</summary>
    public double PlayedSeconds
    {
        get
        {
            var time = new MmTime { Type = TimeBytes };
            if (waveOutGetPosition(_handle, ref time, sizeof(MmTime)) != 0) return 0;
            return time.Type switch
            {
                TimeBytes => (double)time.Value / _bytesPerSecond,
                TimeSamples => (double)time.Value / SampleRate,
                TimeMs => time.Value / 1000.0,
                _ => 0,
            };
        }
    }

    /// <summary>
    /// Sets the output level, 0 to 1. It acts on sound that is already queued, so the change
    /// is heard at once. Windows shows it as this app's level in its volume mixer.
    /// </summary>
    public void SetVolume(double level)
    {
        uint channel = (uint)Math.Round(Math.Clamp(level, 0, 1) * 0xFFFF);
        waveOutSetVolume(_handle, channel | channel << 16);
    }

    public void Pause() => waveOutPause(_handle);

    public void Resume() => waveOutRestart(_handle);

    /// <summary>Drops everything queued and sets the played counter back to zero.</summary>
    public void Reset(bool paused)
    {
        waveOutReset(_handle);
        Collect();
        if (paused) waveOutPause(_handle); else waveOutRestart(_handle);
    }

    public void Dispose()
    {
        if (_handle == IntPtr.Zero) return;
        waveOutReset(_handle);
        Collect();
        waveOutClose(_handle);
        _handle = IntPtr.Zero;
    }
}
