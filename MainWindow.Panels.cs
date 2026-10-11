using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MediaViewer;

// The pop-up panels over the picture (Picture, Rotate, Options) and what they control:
// picture adjustments, orientation, the user's keys and the window colour.
public partial class MainWindow
{
    readonly Settings _settings = Settings.Load();
    readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    readonly List<Border> _panels = new();
    Border? _adjustPanel, _rotatePanel, _optionsPanel;

    // ---------- Window colour ----------

    /// <summary>
    /// Sets the brushes everything in the app is drawn with. Text and icons switch between
    /// white and black so they stay readable on the chosen colour.
    /// </summary>
    internal static void ApplyTheme(Color window)
    {
        bool light = 0.2126 * window.R + 0.7152 * window.G + 0.0722 * window.B > 140;
        var text = light ? Colors.Black : Colors.White;
        var resources = Application.Current.Resources;
        resources["WindowBrush"] = Frozen(window);
        resources["TextBrush"] = Frozen(text);
        resources["HoverBrush"] = Frozen(Color.FromArgb(0x33, text.R, text.G, text.B));
        resources["PanelBrush"] = Frozen(Color.FromArgb(0xF2, window.R, window.G, window.B));
        resources["PanelBorderBrush"] = Frozen(Color.FromArgb(0x66, text.R, text.G, text.B));
        resources["ActiveBrush"] = Frozen(Color.FromArgb(0x59, text.R, text.G, text.B));
    }

    static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    static Color ParseColor(string text)
    {
        try
        {
            return ColorConverter.ConvertFromString(text) is Color color ? Color.FromRgb(color.R, color.G, color.B) : Colors.Black;
        }
        catch (FormatException)
        {
            return Colors.Black;
        }
    }

    /// <summary>Colours a standard Windows title bar (used by secondary windows) to match.</summary>
    internal static void ApplyTitleBarColors(Window window)
    {
        // The colour attributes need Windows 11; older versions fall back to the dark-mode
        // title bar or ignore the calls.
        const int UseImmersiveDarkMode = 20, BorderColor = 34, CaptionColor = 35, TextColor = 36;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        var back = ((SolidColorBrush)Application.Current.Resources["WindowBrush"]).Color;
        var text = ((SolidColorBrush)Application.Current.Resources["TextBrush"]).Color;
        // Windows wants 0x00BBGGRR.
        int backRef = back.R | back.G << 8 | back.B << 16, textRef = text.R | text.G << 8 | text.B << 16;
        int dark = text == Colors.White ? 1 : 0;
        DwmSetWindowAttribute(hwnd, UseImmersiveDarkMode, ref dark, sizeof(int));
        DwmSetWindowAttribute(hwnd, CaptionColor, ref backRef, sizeof(int));
        DwmSetWindowAttribute(hwnd, BorderColor, ref backRef, sizeof(int));
        DwmSetWindowAttribute(hwnd, TextColor, ref textRef, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    // ---------- Panel plumbing ----------

    void TogglePanel(ref Border? panel, Func<Border> build)
    {
        panel ??= build();
        bool show = panel.Visibility != Visibility.Visible;
        foreach (var other in _panels) other.Visibility = Visibility.Collapsed;
        CancelKeyCapture();
        if (show) panel.Visibility = Visibility.Visible;
        UpdateButtonStates();
    }

    /// <summary>
    /// Keeps a button lit for as long as what it does is on: its menu is open, its mode is
    /// active, or (for play forward and backward) it is being held.
    /// </summary>
    void UpdateButtonStates()
    {
        static void Mark(Button button, bool active) => button.Tag = active ? "Active" : null;
        static bool Open(Border? panel) => panel?.Visibility == Visibility.Visible;

        Mark(AdjustButton, Open(_adjustPanel));
        Mark(RotateButton, Open(_rotatePanel));
        Mark(OptionsButton, Open(_optionsPanel));
        Mark(FolderButton, _settings.PlayFolder && !_settings.RepeatFile);
        Mark(MuteButton, _muted);
        Mark(StepBackButton, _scrubbing && _scrubDirection < 0);
        Mark(StepForwardButton, _scrubbing && _scrubDirection > 0);
    }

    Border CreatePanel(string title, out StackPanel rows, double width = 300)
    {
        rows = new StackPanel { Width = width };
        rows.Children.Add(Heading(title, first: true));
        var panel = new Border
        {
            // Scrolls when the window is too short to show the whole panel.
            Child = new ScrollViewer
            {
                Content = rows,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Focusable = false,
                // Room for the scroll bar, so it never covers the right-hand column.
                MinWidth = width + SystemParameters.VerticalScrollBarWidth + 4,
            },
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(12),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Visibility = Visibility.Collapsed,
        };
        panel.SetResourceReference(Border.BackgroundProperty, "PanelBrush");
        panel.SetResourceReference(Border.BorderBrushProperty, "PanelBorderBrush");
        // Clicks and scrolling on a panel must not pan, zoom or toggle fullscreen behind it.
        panel.MouseLeftButtonDown += (_, e) => e.Handled = true;
        panel.MouseWheel += (_, e) => e.Handled = true;
        Viewport.Children.Add(panel);
        _panels.Add(panel);
        return panel;
    }

    static TextBlock Heading(string text, bool first = false)
    {
        var heading = Label(text);
        heading.FontSize = 14;
        heading.FontWeight = FontWeights.SemiBold;
        heading.Margin = new Thickness(0, first ? 0 : 14, 0, 8);
        return heading;
    }

    static TextBlock Label(string text)
    {
        var label = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        label.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        return label;
    }

    static Button TextButton(string text, Action click, double width = double.NaN)
    {
        var button = new Button
        {
            Content = text,
            FontFamily = SystemFonts.MessageFontFamily,
            FontSize = 13,
            Width = width,
            Height = 28,
            Margin = new Thickness(0, 0, 6, 6),
            Padding = new Thickness(10, 0, 10, 0),
        };
        button.SetResourceReference(BackgroundProperty, "HoverBrush");
        button.SetResourceReference(BorderBrushProperty, "PanelBorderBrush");
        button.Click += (_, _) => click();
        return button;
    }

    static UniformGrid ButtonRow(params Button[] buttons)
    {
        var row = new UniformGrid { Rows = 1 };
        foreach (var button in buttons) row.Children.Add(button);
        return row;
    }

    // ---------- Picture panel ----------

    readonly AdjustEffect _adjust = new();
    readonly List<Action> _adjustResets = new();

    void ToggleAdjustPanel() => TogglePanel(ref _adjustPanel, BuildAdjustPanel);

    Border BuildAdjustPanel()
    {
        var panel = CreatePanel("Picture", out var rows);
        // Slider range, neutral value, how the number is shown, and what it sets on the effect.
        AddAdjustSlider(rows, "Brightness", -100, 100, 0, v => $"{v:+0;-0;0}", v => _adjust.Brightness = v / 200);
        AddAdjustSlider(rows, "Contrast", 0, 200, 100, v => $"{v:0}%", v => _adjust.Contrast = v / 100);
        AddAdjustSlider(rows, "Saturation", 0, 200, 100, v => $"{v:0}%", v => _adjust.Saturation = v / 100);
        // Gamma runs from about 0.4 to 2.5 on a doubling scale, so that 1.0 sits in the middle.
        AddAdjustSlider(rows, "Gamma", -100, 100, 0, v => $"{Math.Pow(2, v / 75):0.00}", v => _adjust.Gamma = Math.Pow(2, v / 75));
        AddAdjustSlider(rows, "Warmth", -100, 100, 0, v => $"{v:+0;-0;0}", v => _adjust.Warmth = v / 100);

        var reset = TextButton("Reset", () => _adjustResets.ForEach(r => r()), 80);
        reset.HorizontalAlignment = HorizontalAlignment.Right;
        reset.Margin = new Thickness(0, 8, 0, 0);
        rows.Children.Add(reset);
        return panel;
    }

    void AddAdjustSlider(Panel rows, string name, double minimum, double maximum, double neutral,
                         Func<double, string> format, Action<double> apply)
    {
        var slider = AddSlider(rows, name, minimum, maximum, neutral, format, value =>
        {
            apply(value);
            // With everything neutral the effect is removed, so the picture is shown untouched.
            Img.Effect = _adjust.IsNeutral ? null : _adjust;
        });
        _adjustResets.Add(() => slider.Value = neutral);
    }

    static Slider AddSlider(Panel rows, string name, double minimum, double maximum, double start,
                            Func<double, string> format, Action<double> changed)
    {
        var value = Label(format(start));
        value.TextAlignment = TextAlignment.Right;
        var slider = new Slider
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = start,
            Focusable = false,
            IsMoveToPointEnabled = true,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 8, 0),
        };
        slider.ValueChanged += (_, e) =>
        {
            double rounded = Math.Round(e.NewValue);
            value.Text = format(rounded);
            changed(rounded);
        };

        var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        Grid.SetColumn(slider, 1);
        Grid.SetColumn(value, 2);
        row.Children.Add(Label(name));
        row.Children.Add(slider);
        row.Children.Add(value);
        rows.Children.Add(row);
        return slider;
    }

    // ---------- Rotate panel ----------

    // The orientation the file itself asks for (phone videos); Reset returns to it.
    double _baseRotation;

    void ToggleRotatePanel() => TogglePanel(ref _rotatePanel, BuildRotatePanel);

    Border BuildRotatePanel()
    {
        var panel = CreatePanel("Rotate and flip", out var rows, 280);
        rows.Children.Add(ButtonRow(TextButton("Rotate left", () => RotateBy(-90)),
                                    TextButton("Rotate right", () => RotateBy(90))));
        rows.Children.Add(ButtonRow(TextButton("Flip horizontally", () => FlipBy(-1, 1)),
                                    TextButton("Flip vertically", () => FlipBy(1, -1))));
        var reset = TextButton("Reset", ResetOrientation, 80);
        reset.HorizontalAlignment = HorizontalAlignment.Right;
        reset.Margin = new Thickness(0, 8, 6, 0);
        rows.Children.Add(reset);
        return panel;
    }

    void RotateBy(double degrees)
    {
        if (Img.Source == null) return;
        ResetView();
        // The flip is applied after the rotation, so with one flip active the turn has to go
        // the other way to look right on screen.
        bool mirrored = Flip.ScaleX * Flip.ScaleY < 0;
        Rot.Angle = ((Rot.Angle + (mirrored ? -degrees : degrees)) % 360 + 360) % 360;
        UpdateStatus();
    }

    void FlipBy(double horizontal, double vertical)
    {
        if (Img.Source == null) return;
        ResetView();
        Flip.ScaleX *= horizontal;
        Flip.ScaleY *= vertical;
        UpdateStatus();
    }

    void ResetOrientation()
    {
        ResetView();
        Rot.Angle = _baseRotation;
        Flip.ScaleX = Flip.ScaleY = 1;
        UpdateStatus();
    }

    // ---------- Options panel: keys and window colour ----------

    static readonly (KeyAction Action, string Name)[] KeyActions =
    {
        (KeyAction.PlayPause, "Play / pause"),
        (KeyAction.NextFile, "Next file"),
        (KeyAction.PreviousFile, "Previous file"),
        (KeyAction.PlayForward, "Play forward"),
        (KeyAction.PlayBackward, "Play backward"),
        (KeyAction.QuickHide, "Hide the picture"),
    };

    static readonly string[] ColorPresets =
    {
        "#000000", "#1E1E1E", "#3A3A3A", "#0B1E33", "#10291B", "#2B1020", "#C8C8C8", "#F4F4F4",
    };

    readonly Dictionary<KeyAction, Button> _keyButtons = new();
    readonly Slider[] _colorSliders = new Slider[3];
    KeyAction? _capturing;
    bool _syncingColor, _shuffleStarted;

    void ToggleOptionsPanel() => TogglePanel(ref _optionsPanel, BuildOptionsPanel);

    Border BuildOptionsPanel()
    {
        var panel = CreatePanel("Playback", out var rows, 320);
        _playFolderBox = Check("Play the whole folder", _settings.PlayFolder, SetPlayFolder,
                               "Opens the next file when a video ends, or after 4 seconds on a picture (S)");
        _shuffleBox = Check("In random order", _settings.Shuffle, on =>
        {
            _settings.Shuffle = on;
            PlaybackChanged();
        }, indent: 22);
        _repeatFolderBox = Check("Start again after the last file", _settings.RepeatFolder, on =>
        {
            _settings.RepeatFolder = on;
            PlaybackChanged();
        }, indent: 22);
        _repeatFileBox = Check("Repeat the current file", _settings.RepeatFile, on =>
        {
            _settings.RepeatFile = on;
            PlaybackChanged();
        }, "Plays the same video again when it ends, and stays on this file");
        rows.Children.Add(_playFolderBox);
        rows.Children.Add(_shuffleBox);
        rows.Children.Add(_repeatFolderBox);
        rows.Children.Add(_repeatFileBox);
        RefreshPlaybackBoxes();

        rows.Children.Add(Heading("Keys"));
        foreach (var (action, name) in KeyActions)
        {
            var button = TextButton("", () => BeginKeyCapture(action), 120);
            button.Margin = new Thickness(0, 2, 0, 2);
            button.HorizontalAlignment = HorizontalAlignment.Right;
            _keyButtons[action] = button;

            var row = new Grid();
            row.Children.Add(Label(name));
            row.Children.Add(button);
            rows.Children.Add(row);
        }
        var hint = Label("Click a key, then press the new one. Esc cancels. The arrow keys always move between files.");
        hint.FontSize = 11;
        hint.Opacity = 0.75;
        hint.Margin = new Thickness(0, 6, 0, 6);
        rows.Children.Add(hint);
        var resetKeys = TextButton("Reset keys", () =>
        {
            _settings.Keys = Settings.DefaultKeys();
            KeysChanged();
        }, 100);
        resetKeys.HorizontalAlignment = HorizontalAlignment.Right;
        resetKeys.Margin = new Thickness(0);
        rows.Children.Add(resetKeys);

        rows.Children.Add(Heading("Privacy"));
        rows.Children.Add(Check("Hide from screenshots and screen sharing", _settings.HideFromCapture, on =>
        {
            _settings.HideFromCapture = on;
            _settings.Save();
            ApplyCaptureProtection();
        }, "The window appears black in screenshots, recordings, screen sharing and Windows Recall"));
        rows.Children.Add(Check("Hide file names", _settings.HideFileNames, on =>
        {
            _settings.HideFileNames = on;
            _settings.Save();
            UpdateStatus();
        }, "Shows \"Simple\" instead of the file's name in the title bar and taskbar"));

        rows.Children.Add(Heading("Window colour"));
        var swatches = new WrapPanel();
        foreach (var preset in ColorPresets)
        {
            var color = ParseColor(preset);
            var swatch = new Border
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(0, 0, 8, 8),
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                Background = Frozen(color),
                Cursor = Cursors.Hand,
                ToolTip = preset,
            };
            swatch.SetResourceReference(Border.BorderBrushProperty, "PanelBorderBrush");
            swatch.MouseLeftButtonDown += (_, _) => SetWindowColor(color);
            swatches.Children.Add(swatch);
        }
        rows.Children.Add(swatches);

        var current = ParseColor(_settings.WindowColor);
        _colorSliders[0] = AddSlider(rows, "Red", 0, 255, current.R, v => $"{v:0}", _ => ColorSliderMoved());
        _colorSliders[1] = AddSlider(rows, "Green", 0, 255, current.G, v => $"{v:0}", _ => ColorSliderMoved());
        _colorSliders[2] = AddSlider(rows, "Blue", 0, 255, current.B, v => $"{v:0}", _ => ColorSliderMoved());

        RefreshKeyButtons();
        return panel;
    }

    static CheckBox Check(string text, bool value, Action<bool> changed, string? tip = null, double indent = 0)
    {
        var box = new CheckBox
        {
            Content = text,
            IsChecked = value,
            Focusable = false,
            ToolTip = tip,
            Margin = new Thickness(indent, 4, 0, 4),
        };
        box.SetResourceReference(ForegroundProperty, "TextBrush");
        // The text colour is set explicitly, so a disabled box has to be dimmed by hand.
        box.IsEnabledChanged += (_, _) => box.Opacity = box.IsEnabled ? 1 : 0.45;
        box.Click += (_, _) => changed(box.IsChecked == true);
        return box;
    }

    // ---------- Privacy: capture protection, hidden names, quick hide ----------

    [DllImport("user32.dll")]
    static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

    bool _hidden;

    /// <summary>
    /// Asks Windows to leave this window out of anything that captures the screen. The window
    /// then shows as black in screenshots, recordings, screen sharing and Recall snapshots.
    /// </summary>
    void ApplyCaptureProtection()
    {
        const uint None = 0, ExcludeFromCapture = 0x11;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero) SetWindowDisplayAffinity(hwnd, _settings.HideFromCapture ? ExcludeFromCapture : None);
    }

    string DisplayName(string path) => _settings.HideFileNames ? "this file" : System.IO.Path.GetFileName(path);

    /// <summary>One key to blank the window: pauses the video, covers the picture, hides the name.</summary>
    void ToggleQuickHide()
    {
        _hidden = !_hidden;
        if (_hidden)
        {
            if (_videoPlaying) SetPlaying(false);
            foreach (var panel in _panels) panel.Visibility = Visibility.Collapsed;
            CancelKeyCapture();
            UpdateButtonStates();
        }
        Cover.Visibility = _hidden ? Visibility.Visible : Visibility.Collapsed;
        UpdateStatus();
    }

    // ---------- Playback: whole folder, shuffle, repeat ----------

    readonly List<int> _shuffleLeft = new();
    CheckBox? _playFolderBox, _shuffleBox, _repeatFolderBox, _repeatFileBox;

    void SetPlayFolder(bool on)
    {
        _settings.PlayFolder = on;
        // Asking for the folder (S key or its button) ends "repeat the current file".
        if (on) _settings.RepeatFile = false;
        PlaybackChanged();
    }

    // While one file is being repeated the folder options have no effect, so they are greyed out.
    void RefreshPlaybackBoxes()
    {
        if (_playFolderBox == null) return;
        _playFolderBox.IsChecked = _settings.PlayFolder;
        _repeatFileBox!.IsChecked = _settings.RepeatFile;
        _playFolderBox.IsEnabled = !_settings.RepeatFile;
        _shuffleBox!.IsEnabled = _repeatFolderBox!.IsEnabled = _settings.PlayFolder && !_settings.RepeatFile;
    }

    void PlaybackChanged()
    {
        _settings.Save();
        _slideTimer.Stop();
        if (_settings.PlayFolder) _slideTimer.Start();
        _shuffleLeft.Clear();
        _shuffleStarted = false;
        RefreshPlaybackBoxes();
        UpdateButtonStates();
        UpdateStatus();
    }

    /// <summary>
    /// Opens the file that "play the whole folder" comes to next. Returns false at the end of
    /// the folder when it isn't set to start again.
    /// </summary>
    bool AutoAdvance()
    {
        if (_files.Count == 0) return false;
        if (_files.Count == 1)
        {
            if (_settings.RepeatFolder) ShowCurrent();
            return _settings.RepeatFolder;
        }

        if (!_settings.Shuffle)
        {
            if (_index == _files.Count - 1 && !_settings.RepeatFolder) return false;
            Step(1);
            return true;
        }

        // Random order without repeats: every other file once, then (if repeating) a new round.
        if (_shuffleLeft.Count == 0)
        {
            if (_shuffleStarted && !_settings.RepeatFolder) return false;
            _shuffleLeft.AddRange(Enumerable.Range(0, _files.Count).Where(i => i != _index));
            for (int i = _shuffleLeft.Count - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);
                (_shuffleLeft[i], _shuffleLeft[j]) = (_shuffleLeft[j], _shuffleLeft[i]);
            }
        }
        int next = _shuffleLeft[^1];
        _shuffleLeft.RemoveAt(_shuffleLeft.Count - 1);
        _shuffleStarted = true;
        GoTo(next);
        return true;
    }

    void ColorSliderMoved()
    {
        if (_syncingColor || _colorSliders[2] == null) return;
        SetWindowColor(Color.FromRgb((byte)_colorSliders[0].Value, (byte)_colorSliders[1].Value, (byte)_colorSliders[2].Value));
    }

    void SetWindowColor(Color color)
    {
        ApplyTheme(color);
        _settings.WindowColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        SaveSettingsSoon();
        if (_colorSliders[2] == null) return;
        _syncingColor = true;
        _colorSliders[0].Value = color.R;
        _colorSliders[1].Value = color.G;
        _colorSliders[2].Value = color.B;
        _syncingColor = false;
    }

    // Sliders fire many changes a second; write the file once they settle.
    void SaveSettingsSoon()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    void BeginKeyCapture(KeyAction action)
    {
        CancelKeyCapture();
        _capturing = action;
        _keyButtons[action].Content = "Press a key…";
    }

    void CancelKeyCapture()
    {
        if (_capturing == null) return;
        _capturing = null;
        RefreshKeyButtons();
    }

    /// <summary>Called for every key press while a key box is waiting for its new key.</summary>
    void CaptureKey(Key key)
    {
        // A modifier on its own isn't a choice; keep waiting for the real key.
        if (key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                or Key.LWin or Key.RWin or Key.System or Key.ImeProcessed or Key.DeadCharProcessed or Key.None)
        {
            return;
        }
        var action = _capturing!.Value;
        _capturing = null;
        if (key != Key.Escape)
        {
            // A key can only do one thing: whatever had it gets this action's old key.
            var previous = _settings.Keys[action];
            foreach (var other in _settings.Keys.Where(pair => pair.Value == key && pair.Key != action).Select(pair => pair.Key).ToList())
                _settings.Keys[other] = previous;
            _settings.Keys[action] = key;
        }
        KeysChanged();
    }

    void KeysChanged()
    {
        _settings.Save();
        RefreshKeyButtons();
        UpdateKeyHints();
    }

    void RefreshKeyButtons()
    {
        foreach (var (action, button) in _keyButtons) button.Content = KeyName(_settings.Keys[action]);
    }

    /// <summary>Puts the current keys into the tooltips of the buttons they belong to.</summary>
    void UpdateKeyHints()
    {
        string Name(KeyAction action) => KeyName(_settings.Keys[action]);
        PlayButton.ToolTip = $"Play / pause ({Name(KeyAction.PlayPause)})";
        StepBackButton.ToolTip = $"Play backward ({Name(KeyAction.PlayBackward)})";
        StepForwardButton.ToolTip = $"Play forward ({Name(KeyAction.PlayForward)})";
        PrevButton.ToolTip = $"Previous file (Left, {Name(KeyAction.PreviousFile)})";
        NextButton.ToolTip = $"Next file (Right, {Name(KeyAction.NextFile)})";
    }

    static string KeyName(Key key) => key switch
    {
        >= Key.D0 and <= Key.D9 => ((int)key - (int)Key.D0).ToString(),
        >= Key.NumPad0 and <= Key.NumPad9 => "Num " + ((int)key - (int)Key.NumPad0),
        Key.OemPlus => "+",
        Key.OemMinus => "-",
        Key.OemComma => ",",
        Key.OemPeriod => ".",
        Key.Return => "Enter",
        Key.Back => "Backspace",
        Key.Next => "Page Down",
        Key.Prior => "Page Up",
        Key.Capital => "Caps Lock",
        _ => key.ToString(),
    };

    /// <summary>Runs the action the user has put on this key, if any.</summary>
    bool RunKeyAction(KeyEventArgs e)
    {
        foreach (var (action, key) in _settings.Keys)
        {
            if (key != e.Key) continue;
            switch (action)
            {
                case KeyAction.PlayPause when _isVideo: SetPlaying(!_videoPlaying); break;
                case KeyAction.PlayPause or KeyAction.NextFile: Step(1); break;
                case KeyAction.PreviousFile: Step(-1); break;
                case KeyAction.PlayForward when _isVideo && !e.IsRepeat: StartScrub(e.Key, 1); break;
                case KeyAction.PlayBackward when _isVideo && !e.IsRepeat: StartScrub(e.Key, -1); break;
                case KeyAction.QuickHide when !e.IsRepeat: ToggleQuickHide(); break;
            }
            return true;
        }
        return false;
    }
}
