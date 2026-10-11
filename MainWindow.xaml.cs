using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ReiksViewer;

public partial class MainWindow : Window
{
    static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".jpe", ".jfif", ".png", ".apng", ".bmp", ".gif", ".tif", ".tiff",
        ".webp", ".ico", ".heic", ".heif", ".avif", ".jxl", ".jp2", ".j2k", ".tga", ".qoi", ".svg",
    };

    static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".m4v", ".mov", ".wmv", ".avi", ".mkv", ".webm", ".mpg", ".mpeg", ".3gp", ".ts", ".mts",
        ".m2ts", ".flv", ".f4v", ".ogv", ".vob", ".asf", ".gifv", ".mxf", ".divx",
    };

    const double MaxZoom = 40;
    const string PlayGlyph = "", PauseGlyph = "", VolumeGlyph = "", MuteGlyph = "";

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    static extern int StrCmpLogicalW(string a, string b);

    readonly DispatcherTimer _gifTimer = new(DispatcherPriority.Render);
    readonly DispatcherTimer _videoTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    readonly DispatcherTimer _slideTimer = new() { Interval = TimeSpan.FromSeconds(4) };

    List<string> _files = new();
    int _index = -1;
    FfmpegImage.Result? _animation;
    int _animationFrame;
    FfmpegPlayer? _player;
    bool _isVideo, _videoPlaying, _syncingSeek, _muted, _timeShowsHours, _scrubbing;
    int _statusDigits;
    bool _panning;
    Point _panStart;
    double _panX0, _panY0;
    const double ScrubHoldDelay = 0.3, ScrubMaxSpeed = 64;
    readonly DispatcherTimer _scrubTimer = new() { Interval = TimeSpan.FromMilliseconds(60) };
    readonly System.Diagnostics.Stopwatch _scrubWatch = new();
    Key _scrubKey = Key.None;
    int _scrubDirection;
    double _scrubLast, _scrubTarget;
    bool _fullscreen;
    WindowState _stateBeforeFullscreen;
    Rect _boundsBeforeFullscreen;

    public MainWindow()
    {
        // The colour brushes have to exist before the window's XAML asks for them.
        ApplyTheme(ParseColor(_settings.WindowColor));
        L.Use(_settings.Language);
        InitializeComponent();
        ApplyLanguage();
        UpdateButtonStates();
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            _settings.Save();
        };
        _gifTimer.Tick += (_, _) => AdvanceAnimation();
        _videoTimer.Tick += (_, _) => UpdateVideoProgress();
        _slideTimer.Tick += (_, _) =>
        {
            // Pictures move on by this timer; a video moves on when it ends (OnVideoEnded).
            if (!_isVideo && !_settings.RepeatFile) AutoAdvance();
        };
        if (_settings.PlayFolder) _slideTimer.Start();
        Closed += (_, _) =>
        {
            StopMedia();
            if (_saveTimer.IsEnabled) _settings.Save();
        };
        StateChanged += (_, _) => UpdateWindowFrame();
        _scrubTimer.Tick += (_, _) => ScrubTick();
        PreviewKeyUp += (_, e) =>
        {
            if (_scrubbing && e.Key == _scrubKey) StopScrub();
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyTitleBarColors(this);
        ApplyCaptureProtection();
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        var args = Environment.GetCommandLineArgs();
        int languageAt = Array.IndexOf(args, "--lang");
        if (languageAt > 0 && languageAt + 1 < args.Length)
        {
            // Start in a given language for this run only, without touching the saved options.
            _settings.Language = args[languageAt + 1];
            L.Use(_settings.Language);
            ApplyLanguage();
            args = args.Where((_, i) => i != languageAt && i != languageAt + 1).ToArray();
        }
        if (args.Contains("--software"))
        {
            // Troubleshooting switch: decode video on the processor only.
            FfmpegPlayer.UseHardware = false;
            args = args.Where(a => a != "--software").ToArray();
        }
        if (args.Length == 4 && args[1] == "--selftest") _ = RunSelfTest(args[2], args[3]);
        else if (args.Length == 4 && args[1] == "--imagetest") RunImageTest(args[2], args[3]);
        else if (args.Length == 5 && args[1] == "--capturetest") _ = RunCaptureTest(args[2], args[3], args[4] == "on");
        else if (args.Length == 6 && args[1] == "--screenshot")
            _ = RunScreenshot(args[2], args[3], double.Parse(args[4], System.Globalization.CultureInfo.InvariantCulture), args[5]);
        else if (args.Length > 1) OpenPath(args[1]);
    }

    // ---------- Opening and navigation ----------

    static bool IsSupported(string path)
    {
        var ext = Path.GetExtension(path);
        return ImageExtensions.Contains(ext) || VideoExtensions.Contains(ext);
    }

    static List<string> ListFolder(string folder)
    {
        try
        {
            var files = Directory.EnumerateFiles(folder).Where(IsSupported).ToList();
            files.Sort(StrCmpLogicalW);
            return files;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new List<string>();
        }
    }

    void OpenPath(string path)
    {
        path = Path.GetFullPath(path);
        if (Directory.Exists(path))
        {
            _files = ListFolder(path);
            _index = _files.Count > 0 ? 0 : -1;
        }
        else
        {
            _files = ListFolder(Path.GetDirectoryName(path) ?? ".");
            _index = _files.FindIndex(f => string.Equals(f, path, StringComparison.OrdinalIgnoreCase));
            if (_index < 0)
            {
                _files = new List<string> { path };
                _index = 0;
            }
        }
        _shuffleLeft.Clear();
        _shuffleStarted = false;
        ShowCurrent();
    }

    void Step(int delta)
    {
        if (_files.Count == 0) return;
        _index = ((_index + delta) % _files.Count + _files.Count) % _files.Count;
        ShowCurrent();
    }

    void GoTo(int index)
    {
        if (_files.Count == 0) return;
        _index = Math.Clamp(index, 0, _files.Count - 1);
        ShowCurrent();
    }

    void ShowOpenDialog()
    {
        var images = string.Join(";", ImageExtensions.Select(e => "*" + e));
        var videos = string.Join(";", VideoExtensions.Select(e => "*" + e));
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            // Keep the chosen file out of Windows' "recent files" lists.
            AddToRecent = false,
            Filter = $"{L.T("dlg.all")}|{images};{videos}|{L.T("dlg.pictures")}|{images}|{L.T("dlg.videos")}|{videos}|{L.T("dlg.allfiles")}|*.*",
        };
        if (dialog.ShowDialog(this) == true) OpenPath(dialog.FileName);
    }

    void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } dropped)
            OpenPath(dropped[0]);
    }

    // ---------- Showing a file ----------

    void ShowCurrent()
    {
        // Moving to another file ends quick hide.
        _hidden = false;
        Cover.Visibility = Visibility.Collapsed;
        // Every picture gets the full interval, whenever it was opened.
        if (_slideTimer.IsEnabled)
        {
            _slideTimer.Stop();
            _slideTimer.Start();
        }
        StopMedia();
        ResetView();
        Rot.Angle = _baseRotation = 0;
        Flip.ScaleX = Flip.ScaleY = 1;
        Hint.Visibility = Visibility.Collapsed;

        if (_index < 0)
        {
            ShowMessage(L.T("msg.nofiles"));
            UpdateStatus();
            return;
        }

        var path = _files[_index];
        try
        {
            if (VideoExtensions.Contains(Path.GetExtension(path)))
                PlayVideo(path);
            else
                LoadImage(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                       or NotSupportedException or FileFormatException
                                       or COMException or ArgumentException or OverflowException
                                       or InvalidOperationException)
        {
            StopMedia();
            ShowMessage(L.T("msg.cantopen", DisplayName(path)) + "\n" + ex.Message);
        }
        UpdateStatus();
    }

    void ShowMessage(string text)
    {
        _hintIsWelcome = false;
        Img.Source = null;
        Hint.Text = text;
        Hint.Visibility = Visibility.Visible;
    }

    void StopMedia()
    {
        _gifTimer.Stop();
        _animation = null;
        _videoTimer.Stop();
        StopScrub();
        _player?.Dispose();
        _player = null;
        VideoPanel.Visibility = Visibility.Collapsed;
        _isVideo = false;
        _videoPlaying = false;
    }

    void LoadImage(string path)
    {
        var image = FfmpegImage.Load(path);
        if (image.TooLong)
        {
            // Too many frames to hold in memory: treat it as the video it effectively is.
            PlayVideo(path);
        }
        else if (image.Frames.Length > 1)
        {
            _animation = image;
            _animationFrame = -1;
            AdvanceAnimation();
        }
        else
        {
            Img.Source = image.Frames[0];
        }
    }

    void AdvanceAnimation()
    {
        if (_animation == null) return;
        _animationFrame = (_animationFrame + 1) % _animation.Frames.Length;
        Img.Source = _animation.Frames[_animationFrame];
        _gifTimer.Interval = _animation.Delays[_animationFrame];
        _gifTimer.Start();
    }

    // ---------- Video ----------

    void PlayVideo(string path)
    {
        if (!FfmpegPlayer.EnsureLoaded())
        {
            ShowMessage(L.T("msg.noffmpeg") + "\n" + FfmpegPlayer.LoadError + "\n" + L.T("msg.pressf1"));
            return;
        }

        var player = new FfmpegPlayer(path, Dispatcher) { Volume = VolumeSlider.Value, Muted = _muted };
        player.Ended += () => { if (player == _player) OnVideoEnded(); };
        player.Failed += message =>
        {
            if (player != _player) return;
            StopMedia();
            ShowMessage(L.T("msg.cantplay", DisplayName(path)) + "\n" + message);
            UpdateStatus();
        };
        _player = player;
        _isVideo = true;
        Img.Source = player.Bitmap;
        Rot.Angle = _baseRotation = player.Rotation;
        VideoPanel.Visibility = Visibility.Visible;
        _syncingSeek = true;
        Seek.Maximum = Math.Max(player.Duration, 0.001);
        Seek.Value = 0;
        _syncingSeek = false;
        ReserveTimeWidth(player.Duration);
        SetPlaying(true);
        _videoTimer.Start();
        UpdateVideoProgress();
    }

    void SetPlaying(bool playing)
    {
        if (_player == null) return;
        _videoPlaying = playing;
        if (playing) _player.Play(); else _player.Pause();
        PlayButton.Content = playing ? PauseGlyph : PlayGlyph;
    }

    void SeekBy(double seconds) => _player?.Seek(_player.Position + seconds);

    // Z / X, or the two step buttons: a tap moves exactly one frame; holding moves
    // continuously, starting at normal speed and doubling every second up to ScrubMaxSpeed.
    // Key.None means the mouse button is what is being held.
    void StartScrub(Key key, int direction)
    {
        if (_player == null) return;
        SetPlaying(false);
        _scrubKey = key;
        _scrubbing = true;
        _scrubDirection = direction;
        UpdateButtonStates();
        _scrubDirection = direction;
        _scrubLast = ScrubHoldDelay;
        _scrubTarget = double.NaN;
        _player.Step(direction);
        _scrubWatch.Restart();
        _scrubTimer.Start();
    }

    void StopScrub()
    {
        _scrubTimer.Stop();
        _scrubKey = Key.None;
        _scrubbing = false;
        UpdateButtonStates();
    }

    void ScrubTick()
    {
        // Polled, because the release can be missed if the window loses focus mid-hold.
        bool held = _scrubKey == Key.None
            ? Mouse.LeftButton == MouseButtonState.Pressed
            : Keyboard.IsKeyDown(_scrubKey);
        if (_player == null || !_scrubbing || !held)
        {
            StopScrub();
            return;
        }
        double heldFor = _scrubWatch.Elapsed.TotalSeconds;
        if (heldFor < ScrubHoldDelay) return;
        if (double.IsNaN(_scrubTarget)) _scrubTarget = _player.Position;
        double speed = Math.Min(ScrubMaxSpeed, Math.Pow(2, heldFor - ScrubHoldDelay));
        _scrubTarget = Math.Clamp(_scrubTarget + _scrubDirection * speed * (heldFor - _scrubLast),
                                  0, Math.Max(_player.Duration, 0));
        _scrubLast = heldFor;
        _player.Seek(_scrubTarget);
    }

    void UpdateVideoProgress()
    {
        if (_player == null) return;
        double position = _player.Position;
        if (!Seek.IsMouseCaptureWithin)
        {
            _syncingSeek = true;
            Seek.Value = position;
            _syncingSeek = false;
        }
        // Paused is when single frames matter, so show milliseconds then.
        TimeText.Text = $"{FormatTime(position, !_videoPlaying)} / {FormatTime(_player.Duration, false)}";
    }

    // The time text gets the width of its longest form for this video (paused, with
    // milliseconds), so the seek bar beside it never changes length.
    void ReserveTimeWidth(double duration)
    {
        _timeShowsHours = duration >= 3600;
        TimeText.Width = double.NaN;
        TimeText.Text = $"{FormatTime(duration, true)} / {FormatTime(duration, false)}";
        TimeText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        TimeText.Width = Math.Ceiling(TimeText.DesiredSize.Width - TimeText.Margin.Left - TimeText.Margin.Right) + 2;
    }

    string FormatTime(double seconds, bool precise)
    {
        var t = TimeSpan.FromSeconds(Math.Max(seconds, 0));
        string text = _timeShowsHours ? $"{(int)t.TotalHours}:{t:mm\\:ss}" : $"{(int)t.TotalMinutes}:{t:ss}";
        return precise ? text + t.ToString(@"\.fff") : text;
    }

    void OnVideoEnded()
    {
        if (_settings.RepeatFile)
        {
            // The player starts over from the beginning when told to play after the end.
            SetPlaying(true);
            return;
        }
        if (_settings.PlayFolder && AutoAdvance()) return;
        _videoPlaying = false;
        PlayButton.Content = PlayGlyph;
    }

    void OnSeekChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncingSeek || _player == null) return;
        _player.Seek(e.NewValue);
    }

    void OnVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_player != null) _player.Volume = e.NewValue;
    }

    void ToggleMute()
    {
        _muted = !_muted;
        if (_player != null) _player.Muted = _muted;
        MuteButton.Content = _muted ? MuteGlyph : VolumeGlyph;
        UpdateButtonStates();
    }

    // ---------- Zoom, pan, rotate ----------

    void ResetView()
    {
        Zoom.ScaleX = Zoom.ScaleY = 1;
        Pan.X = Pan.Y = 0;
    }

    void ZoomAt(Point stagePoint, double factor)
    {
        if (Img.Source == null) return;
        double oldScale = Zoom.ScaleX;
        double newScale = Math.Clamp(oldScale * factor, 1, MaxZoom);
        if (newScale == 1)
        {
            ResetView();
        }
        else
        {
            // Keep the point under the cursor where it is.
            Pan.X += stagePoint.X * (oldScale - newScale);
            Pan.Y += stagePoint.Y * (oldScale - newScale);
            Zoom.ScaleX = Zoom.ScaleY = newScale;
        }
        UpdateStatus();
    }

    void ZoomAtCenter(double factor) =>
        ZoomAt(new Point(Stage.ActualWidth / 2, Stage.ActualHeight / 2), factor);

    void OnWheel(object sender, MouseWheelEventArgs e) =>
        ZoomAt(e.GetPosition(Stage), e.Delta > 0 ? 1.2 : 1 / 1.2);

    void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleFullscreen();
            return;
        }
        if (Zoom.ScaleX <= 1) return;
        _panning = true;
        _panStart = e.GetPosition(Viewport);
        _panX0 = Pan.X;
        _panY0 = Pan.Y;
        Viewport.CaptureMouse();
        Viewport.Cursor = Cursors.SizeAll;
    }

    void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_panning) return;
        var p = e.GetPosition(Viewport);
        Pan.X = _panX0 + p.X - _panStart.X;
        Pan.Y = _panY0 + p.Y - _panStart.Y;
    }

    void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_panning) return;
        _panning = false;
        Viewport.ReleaseMouseCapture();
        Viewport.Cursor = null;
    }

    // ---------- Window ----------

    [StructLayout(LayoutKind.Sequential)]
    struct ScreenRect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MonitorInfo
    {
        public int Size;
        public ScreenRect Monitor, Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll")]
    static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    void ToggleFullscreen()
    {
        const uint NearestMonitor = 2, NoZOrder = 0x4, FrameChanged = 0x20;
        const int CornerPreference = 33;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var chrome = System.Windows.Shell.WindowChrome.GetWindowChrome(this);
        _fullscreen = !_fullscreen;
        if (_fullscreen)
        {
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            GetMonitorInfo(MonitorFromWindow(hwnd, NearestMonitor), ref info);

            _stateBeforeFullscreen = WindowState;
            WindowState = WindowState.Normal;
            _boundsBeforeFullscreen = new Rect(Left, Top, Width, Height);
            ResizeMode = ResizeMode.NoResize;
            // No caption or resize strips, or the top of the picture would drag the window.
            chrome.CaptionHeight = 0;
            chrome.ResizeBorderThickness = new Thickness(0);
            Bar.Visibility = TitleBar.Visibility = Visibility.Collapsed;

            // Maximizing stops at the taskbar; covering the monitor's full rectangle hides it.
            int square = 1;
            DwmSetWindowAttribute(hwnd, CornerPreference, ref square, sizeof(int));
            SetWindowPos(hwnd, IntPtr.Zero, info.Monitor.Left, info.Monitor.Top,
                         info.Monitor.Right - info.Monitor.Left, info.Monitor.Bottom - info.Monitor.Top,
                         NoZOrder | FrameChanged);
        }
        else
        {
            int rounded = 0;
            DwmSetWindowAttribute(hwnd, CornerPreference, ref rounded, sizeof(int));
            ResizeMode = ResizeMode.CanResize;
            chrome.CaptionHeight = TitleBar.Height;
            chrome.ResizeBorderThickness = new Thickness(6);
            Left = _boundsBeforeFullscreen.Left;
            Top = _boundsBeforeFullscreen.Top;
            Width = _boundsBeforeFullscreen.Width;
            Height = _boundsBeforeFullscreen.Height;
            WindowState = _stateBeforeFullscreen;
            Bar.Visibility = TitleBar.Visibility = Visibility.Visible;
        }
        UpdateWindowFrame();
    }

    void UpdateWindowFrame()
    {
        // A maximized window with a custom title bar hangs over the screen edges by the
        // width of its (hidden) resize border; pad the content back onto the screen.
        bool maximized = WindowState == WindowState.Maximized;
        Root.Margin = new Thickness(maximized && !_fullscreen ? 7 : 0);
        MaximizeButton.Content = maximized ? "\uE923" : "\uE922";
        MaximizeButton.ToolTip = L.T(maximized ? "tip.restore" : "tip.maximize");
    }

    void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    void OnMaximizeClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    void ToggleSlideshow() => SetPlayFolder(!_settings.PlayFolder);

    void UpdateStatus()
    {
        if (_index < 0 || _files.Count == 0)
        {
            StatusText.Text = ModeText.Text = "";
            Title = TitleText.Text = L.T("app.name");
            return;
        }

        if (_hidden)
        {
            StatusText.Text = ModeText.Text = "";
            Title = TitleText.Text = L.T("app.name");
            return;
        }

        var name = DisplayName(_files[_index]);
        var parts = new List<string> { $"{_index + 1} / {_files.Count}" };
        if (_player != null)
            parts.Add($"{_player.Width} × {_player.Height}");
        else if (Img.Source is BitmapSource bitmap)
            parts.Add($"{bitmap.PixelWidth} × {bitmap.PixelHeight}");

        // Fixed width (room for the largest counter and five-digit sizes) so the seek bar
        // beside it keeps its length; zoom and slideshow are shown in the title bar instead.
        int digits = _files.Count.ToString().Length;
        if (digits != _statusDigits)
        {
            _statusDigits = digits;
            string nines = new string('9', digits);
            StatusText.Width = double.NaN;
            StatusText.Text = $"{nines} / {nines}  ·  99999 × 99999";
            StatusText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            StatusText.Width = Math.Ceiling(StatusText.DesiredSize.Width - StatusText.Margin.Left - StatusText.Margin.Right) + 2;
        }
        StatusText.Text = string.Join("  ·  ", parts);

        var modes = new List<string>();
        if (Zoom.ScaleX > 1) modes.Add(L.T("status.zoom", (Zoom.ScaleX * 100).ToString("0")));
        if (_settings.RepeatFile) modes.Add(L.T("status.repeat"));
        else if (_settings.PlayFolder) modes.Add(L.T(_settings.Shuffle ? "status.shuffled" : "status.folder"));
        ModeText.Text = string.Join("  ·  ", modes);
        TitleText.Text = _settings.HideFileNames ? L.T("app.name") : name;
        Title = _settings.HideFileNames ? L.T("app.name") : $"{name} - {L.T("app.name")}";
    }

    void OnKeyDown(object sender, KeyEventArgs e)
    {
        bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        e.Handled = true;

        // A key box in Options is waiting for its new key: this press is the answer.
        if (_capturing != null)
        {
            CaptureKey(e.Key == Key.System ? e.SystemKey : e.Key);
            return;
        }
        if (ctrl && _isVideo && e.Key is Key.Left or Key.Right)
        {
            SeekBy(e.Key == Key.Left ? -5 : 5);
            return;
        }
        // Keys chosen in Options win over the fixed ones below.
        if (RunKeyAction(e)) return;

        switch (e.Key)
        {
            case Key.Left or Key.PageUp: Step(-1); break;
            case Key.Right or Key.PageDown: Step(1); break;
            case Key.A: ToggleAdjustPanel(); break;
            case Key.Home: GoTo(0); break;
            case Key.End: GoTo(_files.Count - 1); break;
            case Key.Up when _isVideo: VolumeSlider.Value += 0.1; break;
            case Key.Down when _isVideo: VolumeSlider.Value -= 0.1; break;
            case Key.M when _isVideo: ToggleMute(); break;
            case Key.F or Key.F11: ToggleFullscreen(); break;
            case Key.Escape when _fullscreen: ToggleFullscreen(); break;
            case Key.O: ShowOpenDialog(); break;
            case Key.F1: ShowFormats(); break;
            case Key.R: RotateBy(90); break;
            case Key.S: ToggleSlideshow(); break;
            case Key.Add or Key.OemPlus: ZoomAtCenter(1.2); break;
            case Key.Subtract or Key.OemMinus: ZoomAtCenter(1 / 1.2); break;
            case Key.D0 or Key.NumPad0: ResetView(); UpdateStatus(); break;
            default: e.Handled = false; break;
        }
    }

    void OnOpenClick(object sender, RoutedEventArgs e) => ShowOpenDialog();
    void OnPrevClick(object sender, RoutedEventArgs e) => Step(-1);
    void OnNextClick(object sender, RoutedEventArgs e) => Step(1);
    void OnPlayClick(object sender, RoutedEventArgs e) => SetPlaying(!_videoPlaying);
    void OnMuteClick(object sender, RoutedEventArgs e) => ToggleMute();
    void OnStepBackDown(object sender, MouseButtonEventArgs e) => StartScrub(Key.None, -1);
    void OnStepForwardDown(object sender, MouseButtonEventArgs e) => StartScrub(Key.None, 1);
    void OnFormatsClick(object sender, RoutedEventArgs e) => ShowFormats();
    void OnAdjustClick(object sender, RoutedEventArgs e) => ToggleAdjustPanel();

    void ShowFormats()
    {
        FormatsButton.Tag = "Active";
        new FormatsWindow(this, ImageExtensions, VideoExtensions).ShowDialog();
        FormatsButton.Tag = null;
    }
    void OnViewClick(object sender, RoutedEventArgs e) => ToggleViewPanel();
    void OnOptionsClick(object sender, RoutedEventArgs e) => ToggleOptionsPanel();
    void OnSlideshowClick(object sender, RoutedEventArgs e) => ToggleSlideshow();
    void OnFullscreenClick(object sender, RoutedEventArgs e) => ToggleFullscreen();
}
