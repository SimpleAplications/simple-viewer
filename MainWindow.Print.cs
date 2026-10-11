using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReiksViewer;

// Printing: the picture as it is shown, or for a video the frame on screen. The print view
// takes over the window, with the page as it will come out on the left and the choices on
// the right. The printing itself is in PrintEngine.
public partial class MainWindow
{
    PrintView? _printView;

    void PrintCurrent()
    {
        if (_printView != null || _hidden || Img.Source is not BitmapSource shown) return;
        if (_videoPlaying) SetPlaying(false);
        ClosePanels();
        // "Play the whole folder" waits while the print view is open.
        _slideTimer.Stop();
        // The job's name shows in the printer's queue, so "hide file names" covers it too.
        bool named = !_settings.HideFileNames && _index >= 0 && _index < _files.Count;
        _printView = new PrintView(this, PrintablePicture(shown), named ? Path.GetFileName(_files[_index]) : L.T("app.name"));
        // Over the picture and the bar; the title bar stays.
        Grid.SetRow(_printView.View, 1);
        Grid.SetRowSpan(_printView.View, 2);
        Root.Children.Add(_printView.View);
    }

    /// <summary>
    /// Back to the picture. While a job is on its way to Windows the view stays, so that a
    /// failure can still be shown, unless <paramref name="evenWhileSending"/>.
    /// </summary>
    bool ClosePrintView(bool evenWhileSending = false)
    {
        if (_printView == null) return true;
        if (_printView.Sending && !evenWhileSending) return false;
        Root.Children.Remove(_printView.View);
        _printView.Closed = true;
        _printView = null;
        if (_settings.PlayFolder) _slideTimer.Start();
        return true;
    }

    /// <summary>Keys while the print view is open. Nothing reaches the picture behind it.</summary>
    void PrintViewKey(KeyEventArgs e)
    {
        if (e.Key == _settings.Keys[KeyAction.QuickHide])
        {
            // Hiding can't wait for a print job.
            ClosePrintView(evenWhileSending: true);
            ToggleQuickHide();
        }
        else if (e.Key == Key.Escape) ClosePrintView();
        else if (e.Key == Key.Return) _ = _printView!.Send();
        // Alt+F4 and the window's own menu.
        else if (e.Key == Key.System) e.Handled = false;
    }

    /// <summary>
    /// A fixed copy of the picture with what is done to it on screen: rotation, flips and the
    /// image settings. Zoom is left out; the whole picture is printed.
    /// </summary>
    BitmapSource PrintablePicture(BitmapSource shown)
    {
        if (Img.Effect == null && Rot.Angle % 360 == 0 && Flip.ScaleX == 1 && Flip.ScaleY == 1)
        {
            if (shown.IsFrozen) return shown;
            // A video's picture keeps changing.
            var copy = new WriteableBitmap(shown);
            copy.Freeze();
            return copy;
        }

        // The same picture element again, off screen, at the picture's own size.
        double shrink = Math.Min(1, Math.Sqrt(PrintEngine.MaxPixels / ((double)shown.PixelWidth * shown.PixelHeight)));
        var element = new Image
        {
            Source = shown,
            Stretch = Stretch.Fill,
            Width = shown.PixelWidth * shrink,
            Height = shown.PixelHeight * shrink,
            LayoutTransform = Img.LayoutTransform,
            Effect = Img.Effect,
        };
        element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        element.Arrange(new Rect(element.DesiredSize));
        var bitmap = new RenderTargetBitmap((int)Math.Round(element.DesiredSize.Width), (int)Math.Round(element.DesiredSize.Height),
                                            96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        bitmap.Freeze();
        return bitmap;
    }

    sealed class PrintView
    {
        // The page as it was last time, upright, for the first preview: the printer hasn't
        // answered yet when the view opens. Starts as A4 with a few millimetres of edge.
        static PrintEngine.PageLayout s_upright = new(827, 1169, new Rect(17, 17, 793, 1135));

        readonly MainWindow _window;
        readonly BitmapSource _picture;
        readonly string _jobName;
        readonly Canvas _page = new() { Background = Brushes.White, ClipToBounds = true };
        readonly Image _image = new() { Stretch = Stretch.Fill };
        readonly TextBlock _copiesText = Label("1"), _status = Label("");
        readonly Button _fewer, _more, _driver, _send, _cancel;
        PrintEngine.Printer? _printer;
        PrintEngine.PageLayout _upright = s_upright;
        int _copies = 1, _loading, _measuring, _waiting;
        bool _syncing, _sent;

        internal readonly Grid View = new();
        internal readonly ComboBox Printers = Choice(), Papers = Choice(), Orientations = Choice(), Sizes = Choice();
        internal bool Closed;
        internal bool Sending { get; private set; }
        /// <summary>Nothing is being asked of a printer or sent to one.</summary>
        internal bool Idle => _waiting == 0 && !Sending;
        internal string Status => _status.Text;
        internal Button More => _more;
        internal bool CanSend => _send.IsEnabled;
        /// <summary>For the built-in checks: the driver's output goes to this file instead of the printer.</summary>
        internal string? OutputFile;

        bool Landscape => Orientations.SelectedIndex switch
        {
            1 => false,
            2 => true,
            _ => _picture.PixelWidth > _picture.PixelHeight,
        };

        bool Fill => Sizes.SelectedIndex == 1;

        internal PrintView(MainWindow window, BitmapSource picture, string jobName)
        {
            _window = window;
            _picture = picture;
            _jobName = jobName;
            View.SetResourceReference(Panel.BackgroundProperty, "WindowBrush");
            View.ColumnDefinitions.Add(new ColumnDefinition());
            View.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // The page, drawn in hundredths of an inch and scaled to the room there is.
            _image.Source = picture;
            RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.HighQuality);
            _page.Children.Add(_image);
            var sheet = new Border { Child = _page, BorderThickness = new Thickness(2) };
            sheet.SetResourceReference(Border.BorderBrushProperty, "PanelBorderBrush");
            View.Children.Add(new Viewbox { Child = sheet, Margin = new Thickness(24) });

            var rows = new StackPanel { Width = 250, HorizontalAlignment = HorizontalAlignment.Left };
            rows.Children.Add(Heading(L.T("panel.print"), first: true));
            AddChoice(rows, "print.printer", Printers);
            AddChoice(rows, "print.paper", Papers);
            AddChoice(rows, "print.orientation", Orientations, "print.auto", "print.portrait", "print.landscape");
            AddChoice(rows, "print.size", Sizes, "print.whole", "print.fill");
            Sizes.SelectedIndex = window._settings.PrintFill ? 1 : 0;

            _fewer = TextButton("−", () => SetCopies(_copies - 1), 30);
            _more = TextButton("+", () => SetCopies(_copies + 1), 30);
            _fewer.Margin = _more.Margin = new Thickness(0);
            // One sign each: no need for the side margins a word gets.
            ((TextBlock)_fewer.Content).Margin = ((TextBlock)_more.Content).Margin = new Thickness(0);
            _copiesText.Width = 36;
            _copiesText.TextAlignment = TextAlignment.Center;
            var stepper = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            stepper.Children.Add(_fewer);
            stepper.Children.Add(_copiesText);
            stepper.Children.Add(_more);
            var copies = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            copies.Children.Add(Label(L.T("print.copies")));
            copies.Children.Add(stepper);
            rows.Children.Add(copies);

            _driver = TextButton(L.T("print.driver"), ShowDriverSettings);
            _driver.ToolTip = L.T("print.driver.tip");
            _driver.HorizontalAlignment = HorizontalAlignment.Left;
            _driver.Margin = new Thickness(0, 14, 0, 0);
            rows.Children.Add(_driver);

            _send = TextButton(L.T("panel.print"), () => _ = Send());
            _send.SetResourceReference(BackgroundProperty, "ActiveBrush");
            _cancel = TextButton(L.T("print.cancel"), () => window.ClosePrintView());
            _send.Margin = new Thickness(0, 0, 6, 0);
            _cancel.Margin = new Thickness(0);
            var buttons = ButtonRow(_send, _cancel);
            _status.FontSize = 12;
            _status.Margin = new Thickness(0, 10, 0, 10);
            _status.MinHeight = 34;
            _status.VerticalAlignment = VerticalAlignment.Bottom;
            var foot = new StackPanel { Width = rows.Width, HorizontalAlignment = HorizontalAlignment.Left };
            foot.Children.Add(_status);
            foot.Children.Add(buttons);

            // The buttons keep their place at the bottom; the choices scroll if the window is short.
            var column = new DockPanel();
            DockPanel.SetDock(foot, Dock.Bottom);
            column.Children.Add(foot);
            column.Children.Add(new ScrollViewer
            {
                Content = rows,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Focusable = false,
                MinWidth = rows.Width + SystemParameters.VerticalScrollBarWidth + 4,
            });
            var side = new Border { Child = column, Padding = new Thickness(16, 14, 16, 14), BorderThickness = new Thickness(1, 0, 0, 0) };
            side.SetResourceReference(Border.BorderBrushProperty, "PanelBorderBrush");
            Grid.SetColumn(side, 1);
            View.Children.Add(side);

            Printers.SelectionChanged += (_, _) =>
            {
                if (!_syncing && Printers.SelectedItem is string name) _ = LoadPrinter(name);
            };
            Papers.SelectionChanged += (_, _) =>
            {
                if (!_syncing) _ = Measure();
            };
            Orientations.SelectionChanged += (_, _) => _ = Measure();
            Sizes.SelectionChanged += (_, _) => DrawPage();

            DrawPage();
            Refresh();
            _ = FindPrinters();
        }

        static ComboBox Choice() => new() { Focusable = false, Margin = new Thickness(0, 3, 0, 0) };

        static void AddChoice(Panel rows, string name, ComboBox box, params string[] choices)
        {
            var label = Label(L.T(name));
            label.FontSize = 12;
            label.Opacity = 0.75;
            label.Margin = new Thickness(0, rows.Children.Count > 1 ? 10 : 0, 0, 0);
            rows.Children.Add(label);
            foreach (var choice in choices) box.Items.Add(L.T(choice));
            if (choices.Length > 0) box.SelectedIndex = 0;
            rows.Children.Add(box);
        }

        void Say(string text) => _status.Text = text;

        void SetCopies(int copies)
        {
            _copies = Math.Clamp(copies, 1, 99);
            Refresh();
        }

        /// <summary>Greys out what can't be used right now.</summary>
        void Refresh()
        {
            static void Enable(UIElement element, bool on)
            {
                element.IsEnabled = on;
                element.Opacity = on ? 1 : 0.45;
            }
            bool free = !Sending && !_sent, ready = free && _printer != null;
            Enable(Printers, free && Printers.Items.Count > 0);
            Enable(Papers, ready);
            Enable(Orientations, free);
            Enable(Sizes, free);
            Enable(_fewer, free && _copies > 1);
            Enable(_more, free && _copies < 99);
            Enable(_driver, ready);
            Enable(_send, ready);
            Enable(_cancel, !Sending);
            _copiesText.Text = _copies.ToString();
        }

        /// <summary>Puts the picture on the page the way it will be printed.</summary>
        void DrawPage()
        {
            var layout = Landscape ? _upright.Turned() : _upright;
            _page.Width = layout.Width;
            _page.Height = layout.Height;
            PrintEngine.Place(_picture.PixelWidth, _picture.PixelHeight, layout.Printable, Fill, out var target, out var source);
            double scale = target.Width / source.Width;
            _image.Width = _picture.PixelWidth * scale;
            _image.Height = _picture.PixelHeight * scale;
            Canvas.SetLeft(_image, target.X - source.X * scale);
            Canvas.SetTop(_image, target.Y - source.Y * scale);
            _image.Clip = new RectangleGeometry(new Rect(source.X * scale, source.Y * scale, target.Width, target.Height));
        }

        async Task FindPrinters()
        {
            Say(L.T("print.looking"));
            _waiting++;
            try
            {
                var (names, standard) = await Task.Run(PrintEngine.ListPrinters);
                if (Closed) return;
                if (names.Count == 0)
                {
                    Say(L.T("print.none"));
                    return;
                }
                // The one used last time, or else the one Windows has as its default.
                string wanted = names.Contains(_window._settings.PrintPrinter) ? _window._settings.PrintPrinter : standard ?? names[0];
                _syncing = true;
                foreach (var name in names) Printers.Items.Add(name);
                Printers.SelectedItem = wanted;
                _syncing = false;
                await LoadPrinter(wanted);
            }
            catch (Exception ex)
            {
                if (!Closed) Say(ex.Message);
            }
            finally
            {
                _waiting--;
            }
        }

        async Task LoadPrinter(string name)
        {
            int turn = ++_loading;
            _printer = null;
            Say(L.T("print.asking"));
            Refresh();
            _waiting++;
            try
            {
                var printer = await Task.Run(() => new PrintEngine.Printer(name));
                if (Closed || turn != _loading) return;
                _printer = printer;
                ShowPapers(printer.StartingPaper(_window._settings.PrintPaper));
                Say("");
                Refresh();
                await Measure();
            }
            catch (Exception ex)
            {
                if (Closed || turn != _loading) return;
                Say(L.T("print.unusable", ex.Message));
                Refresh();
            }
            finally
            {
                _waiting--;
            }
        }

        void ShowPapers(int chosen)
        {
            _syncing = true;
            Papers.Items.Clear();
            foreach (var paper in _printer!.Papers) Papers.Items.Add(paper.Name);
            Papers.SelectedIndex = chosen;
            _syncing = false;
        }

        /// <summary>
        /// Redraws the page for the chosen paper and orientation: at once with the edges seen
        /// last, then again with the ones this printer reports.
        /// </summary>
        async Task Measure()
        {
            if (_printer is not { } printer || Papers.SelectedIndex < 0)
            {
                DrawPage();
                return;
            }
            int turn = ++_measuring, paper = Papers.SelectedIndex;
            bool landscape = Landscape;
            _upright = _upright.WithPaper(printer.Papers[paper].Width, printer.Papers[paper].Height);
            DrawPage();
            _waiting++;
            try
            {
                var layout = await Task.Run(() => printer.Layout(paper, landscape));
                if (Closed || turn != _measuring) return;
                _upright = s_upright = landscape ? layout.Turned() : layout;
                DrawPage();
            }
            catch (Exception)
            {
                // The estimate stays; the printer itself places the picture when it prints.
            }
            finally
            {
                _waiting--;
            }
        }

        /// <summary>The driver's own window, for what only this printer knows about: paper type, quality.</summary>
        internal void ShowDriverSettings()
        {
            if (_printer is not { } printer || Sending || Papers.SelectedIndex < 0) return;
            bool landscape = Landscape;
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                bool changed;
                try
                {
                    changed = printer.ShowSettings(new System.Windows.Interop.WindowInteropHelper(_window).Handle,
                                                   Papers.SelectedIndex, landscape, _copies);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
                if (!changed || Closed) return;
                // Paper, orientation and copies can be changed there too; show what it was left at.
                ShowPapers(printer.ChosenPaper);
                if (printer.ChosenLandscape != landscape) Orientations.SelectedIndex = printer.ChosenLandscape ? 2 : 1;
                SetCopies(printer.ChosenCopies);
                Say("");
                _ = Measure();
            }
            catch (Exception ex)
            {
                Say(L.T("print.unusable", ex.Message));
            }
        }

        internal async Task Send()
        {
            if (_printer is not { } printer || Sending || _sent || Papers.SelectedIndex < 0) return;
            int paper = Papers.SelectedIndex, copies = _copies;
            bool landscape = Landscape, fill = Fill;
            Sending = true;
            Say(L.T("print.sending"));
            Refresh();

            // A thread of its own that the app waits for: closing the window right after
            // Print must not cut the job short. It is the kind of thread a driver needs when
            // it asks something itself, such as where to save a PDF.
            var done = new TaskCompletionSource<Exception?>();
            var thread = new Thread(() =>
            {
                try
                {
                    printer.Print(_picture, paper, landscape, fill, copies, _jobName, OutputFile);
                    done.SetResult(null);
                }
                catch (Exception ex)
                {
                    done.SetResult(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            var error = await done.Task;

            Sending = false;
            if (Closed) return;
            const int CancelledByUser = 1223;
            if (error != null)
            {
                // Backing out of the driver's own question isn't a failure.
                Say(error is Win32Exception { NativeErrorCode: CancelledByUser } ? "" : L.T("msg.cantprint", error.Message));
                Refresh();
                return;
            }

            _sent = true;
            Say(L.T("print.sent"));
            Refresh();
            if (OutputFile == null)
            {
                _window._settings.PrintPrinter = printer.Name;
                _window._settings.PrintPaper = printer.Papers[paper].Kind;
                _window._settings.PrintFill = fill;
                _window._settings.Save();
            }
            await Task.Delay(1200);
            if (!Closed) _window.ClosePrintView();
        }
    }
}
