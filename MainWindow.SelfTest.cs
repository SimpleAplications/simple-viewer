using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReiksViewer;

// Developer check: "ReiksViewer.exe --selftest <video> <output folder>" drives the player
// through play, pause, stepping and seeking, and writes a log plus screenshots.
public partial class MainWindow
{
    async Task RunSelfTest(string file, string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);
        var log = new List<string>();
        void Log(string label) => log.Add(FormattableString.Invariant(
            $"{label,-28} pos={_player?.Position ?? -1:0.0000} playing={_videoPlaying}"));

        try
        {
            // Start from neutral playback options whatever the user has saved (memory only).
            _settings.PlayFolder = _settings.Shuffle = _settings.RepeatFolder = _settings.RepeatFile = false;
            _slideTimer.Stop();
            OpenPath(file);
            log.Add(FormattableString.Invariant(
                $"opened: player={_player != null} {_player?.Width}x{_player?.Height} dur={_player?.Duration:0.000} frame={_player?.FrameDuration:0.00000} rot={_player?.Rotation} audio={_player?.HasAudio} hint={Hint.Text.Replace('\n', ' ')}"));
            if (_player == null) return;

            await Task.Delay(2500);
            Log("after 2.5 s playing");
            log.Add($"gpu decoding={_player.HardwareDecoding} presented={_player.PresentedFrames} dropped={_player.DroppedFrames}");
            Snapshot(Path.Combine(outputFolder, "1-playing.png"));

            SetPlaying(false);
            await Task.Delay(300);
            Log("paused");
            for (int i = 0; i < 5; i++) { _player.Step(1); await Task.Delay(150); Log($"step +1 (#{i + 1})"); }
            for (int i = 0; i < 8; i++) { _player.Step(-1); await Task.Delay(200); Log($"step -1 (#{i + 1})"); }
            Snapshot(Path.Combine(outputFolder, "2-stepped.png"));

            _player.Seek(5.0); await Task.Delay(800); Log("seek 5.0 paused");
            _player.Seek(5.1); await Task.Delay(300); Log("seek 5.1 (short hop)");
            _player.Seek(1.0); await Task.Delay(800); Log("seek 1.0 paused");
            for (int i = 0; i < 3; i++) { _player.Step(-1); await Task.Delay(300); Log($"step -1 (#{i + 1})"); }
            Snapshot(Path.Combine(outputFolder, "3-seeked.png"));

            ToggleImagePanel();
            var sliders = ((System.Windows.Controls.Panel)((System.Windows.Controls.ScrollViewer)_imagePanel!.Child).Content).Children
                .OfType<System.Windows.Controls.Grid>()
                .Select(row => row.Children.OfType<System.Windows.Controls.Slider>().First()).ToList();
            sliders[0].Value = 20;  // brightness
            sliders[2].Value = 30;  // saturation
            sliders[4].Value = 60;  // warmth
            await Task.Delay(300);
            log.Add($"adjust: effect on={Img.Effect != null}");
            Snapshot(Path.Combine(outputFolder, "4-adjusted.png"));
            _adjustResets.ForEach(reset => reset());
            log.Add($"adjust reset: effect on={Img.Effect != null}");

            RotateBy(90);
            FlipBy(-1, 1);
            await Task.Delay(300);
            log.Add($"rotate panel: angle={Rot.Angle} flip={Flip.ScaleX},{Flip.ScaleY}");
            Snapshot(Path.Combine(outputFolder, "5-rotated.png"));
            ResetOrientation();
            log.Add($"orientation reset: angle={Rot.Angle} flip={Flip.ScaleX},{Flip.ScaleY}");

            // A click inside the open menu leaves it open; a click on the picture closes it.
            void ClickOn(UIElement target) => target.RaiseEvent(
                new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left)
                {
                    RoutedEvent = System.Windows.Input.Mouse.PreviewMouseDownEvent,
                });
            ClickOn(sliders[0]);
            log.Add($"click inside menu: open={AnyPanelOpen}");
            ClickOn(Viewport);
            log.Add($"click on picture: open={AnyPanelOpen}");

            ToggleOptionsPanel();
            // Window colour through its own controls. The colour is put back and the save they
            // schedule is cancelled, so the user's options file is untouched.
            string savedColor = _settings.WindowColor;
            _colorSliders[0].Value = 200;
            log.Add($"colour slider: {_settings.WindowColor}");
            var swatch = ((System.Windows.Controls.Panel)((System.Windows.Controls.ScrollViewer)_optionsPanel!.Child).Content).Children
                .OfType<System.Windows.Controls.WrapPanel>().First().Children.OfType<System.Windows.Controls.Border>().Last();
            swatch.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left)
            {
                RoutedEvent = MouseLeftButtonDownEvent,
            });
            log.Add($"colour swatch: {_settings.WindowColor} sliders={_colorSliders[0].Value},{_colorSliders[1].Value},{_colorSliders[2].Value}");
            await Task.Delay(200);
            Snapshot(Path.Combine(outputFolder, "5b-colour.png"));
            SetWindowColor(ParseColor(savedColor));
            _saveTimer.Stop();

            ApplyTheme(ParseColor("#F4F4F4"));
            await Task.Delay(300);
            Snapshot(Path.Combine(outputFolder, "6-options-light.png"));
            ApplyTheme(ParseColor(_settings.WindowColor));
            ToggleOptionsPanel();
            log.Add($"keys: {string.Join(", ", _settings.Keys.Select(pair => pair.Key + "=" + KeyName(pair.Value)))}");

            SetPlaying(true);
            await Task.Delay(1500);
            Log("resumed, +1.5 s");
            _player.Seek(Math.Max(_player.Duration - 1, 0));
            await Task.Delay(300);
            Log("seek to 1 s before end");
            await Task.Delay(2000);
            Log("2 s later (should be ended)");
            SetPlaying(true);
            await Task.Delay(1000);
            Log("play again (from start)");

            // Playback options, set in memory only so the user's options file is untouched.
            _settings.RepeatFile = true;
            _player!.Seek(Math.Max(_player.Duration - 0.5, 0));
            await Task.Delay(2000);
            log.Add(FormattableString.Invariant($"repeat file: index={_index} pos={_player?.Position:0.00} playing={_videoPlaying}"));
            _settings.RepeatFile = false;
            _settings.PlayFolder = true;
            int before = _index;
            _player!.Seek(Math.Max(_player.Duration - 0.5, 0));
            await Task.Delay(2500);
            log.Add($"play folder: index {before} -> {_index} playing={_videoPlaying} file={Path.GetFileName(_files[_index])}");
            _settings.Shuffle = true;
            var seen = new List<int> { _index };
            for (int i = 0; i < _files.Count; i++) { if (!AutoAdvance()) break; seen.Add(_index); await Task.Delay(150); }
            log.Add($"shuffle without repeat visited {string.Join(",", seen)} of {_files.Count} files, then stopped");
            _settings.PlayFolder = _settings.Shuffle = false;
            SetPlaying(false);
            // Shown with "repeat the current file" ticked, in memory only.
            _settings.PlayFolder = _settings.RepeatFile = true;
            ToggleOptionsPanel();
            RefreshPlaybackBoxes();
            UpdateStatus();
            await Task.Delay(300);
            Snapshot(Path.Combine(outputFolder, "7-options.png"));
            ToggleOptionsPanel();
            _settings.PlayFolder = _settings.RepeatFile = false;
            RefreshPlaybackBoxes();
            UpdateStatus();

            string Frame() => FormattableString.Invariant(
                $"{Left:0},{Top:0} {ActualWidth:0}x{ActualHeight:0} state={WindowState} bars={Bar.Visibility}/{TitleBar.Visibility}");
            log.Add($"screen {SystemParameters.PrimaryScreenWidth:0}x{SystemParameters.PrimaryScreenHeight:0}, window {Frame()}");
            ToggleFullscreen();
            await Task.Delay(500);
            log.Add("fullscreen: " + Frame());
            ToggleFullscreen();
            await Task.Delay(300);
            log.Add("restored:   " + Frame());
            WindowState = WindowState.Maximized;
            await Task.Delay(300);
            ToggleFullscreen();
            await Task.Delay(500);
            log.Add("fullscreen from maximized: " + Frame());
            ToggleFullscreen();
            await Task.Delay(300);
            log.Add("restored:   " + Frame());
        }
        catch (Exception ex)
        {
            log.Add("EXCEPTION: " + ex);
        }
        finally
        {
            File.WriteAllLines(Path.Combine(outputFolder, "selftest.log"), log);
            Close();
        }
    }

    // "ReiksViewer.exe --imagetest <folder> <output folder>" decodes every file in the folder as a picture.
    void RunImageTest(string folder, string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);
        var log = new List<string>();
        foreach (var file in Directory.GetFiles(folder).OrderBy(f => f))
        {
            var name = Path.GetFileName(file);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var image = FfmpegImage.Load(file);
                watch.Stop();
                if (image.TooLong) { log.Add($"{name,-16} too long -> video"); continue; }
                var first = image.Frames[0];
                log.Add(FormattableString.Invariant(
                    $"{name,-16} ok {first.PixelWidth}x{first.PixelHeight} frames={image.Frames.Length} delay0={image.Delays[0].TotalMilliseconds:0}ms {watch.ElapsedMilliseconds}ms"));
                if ((long)first.PixelWidth * first.PixelHeight <= 2_000_000)
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image.Frames[image.Frames.Length / 2]));
                    using var stream = File.Create(Path.Combine(outputFolder, name + ".png"));
                    encoder.Save(stream);
                }
            }
            catch (Exception ex)
            {
                log.Add($"{name,-16} FAILED {ex.GetType().Name}: {ex.Message}");
            }
        }
        File.WriteAllLines(Path.Combine(outputFolder, "imagetest.log"), log);
        Close();
    }

    // "ReiksViewer.exe --screenshot <file> <png> <seconds> <none|picture|options|hide>" opens a file,
    // pauses a video at the given time, optionally opens a panel, and saves a picture of the window.
    async Task RunScreenshot(string file, string output, double seconds, string panel)
    {
        try
        {
            _settings.PlayFolder = _settings.Shuffle = _settings.RepeatFolder = _settings.RepeatFile = false;
            _settings.HideFromCapture = _settings.HideFileNames = false;
            _slideTimer.Stop();
            ApplyTheme(ParseColor("#000000"));
            OpenPath(file);
            await Task.Delay(1200);
            if (_player != null)
            {
                SetPlaying(false);
                _player.Seek(seconds);
                await Task.Delay(1200);
                UpdateVideoProgress();
            }
            if (panel == "picture")
            {
                ToggleImagePanel();
                var sliders = ((System.Windows.Controls.Panel)((System.Windows.Controls.ScrollViewer)_imagePanel!.Child).Content).Children
                    .OfType<System.Windows.Controls.Grid>()
                    .Select(row => row.Children.OfType<System.Windows.Controls.Slider>().First()).ToList();
                sliders[1].Value = 125;  // contrast
                sliders[2].Value = 140;  // saturation
                sliders[4].Value = 25;   // warmth
            }
            else if (panel == "options")
            {
                // The whole menu, without its scroll bar.
                Height = 960;
                ToggleOptionsPanel();
            }
            else if (panel == "hide") ToggleQuickHide();
            await Task.Delay(500);
            Snapshot(output);
        }
        finally
        {
            Close();
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hwnd, out ScreenRect rect);

    // "ReiksViewer.exe --capturetest <file> <rect file> <on|off>" shows a file in a window kept on top,
    // with capture protection on or off, and writes the window's screen rectangle to a file so
    // that a separate screen grab can check what a capture of that area contains.
    async Task RunCaptureTest(string file, string rectFile, bool protect)
    {
        try
        {
            _settings.PlayFolder = _settings.RepeatFile = false;
            _slideTimer.Stop();
            _settings.HideFromCapture = protect;
            ApplyTheme(ParseColor("#000000"));
            WindowState = WindowState.Normal;
            Left = 80;
            Top = 80;
            Width = 800;
            Height = 600;
            Topmost = true;
            ApplyCaptureProtection();
            OpenPath(file);
            await Task.Delay(1500);
            GetWindowRect(new System.Windows.Interop.WindowInteropHelper(this).Handle, out var rect);
            File.WriteAllText(rectFile, $"{rect.Left} {rect.Top} {rect.Right} {rect.Bottom}");
            await Task.Delay(6000);
        }
        finally
        {
            Close();
        }
    }

    void Snapshot(string path)
    {
        // The window itself, so that its background colour is part of the picture.
        FrameworkElement root = this;
        var bitmap = new RenderTargetBitmap((int)root.ActualWidth, (int)root.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
