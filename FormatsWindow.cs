using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReiksViewer;

/// <summary>Lists what the app can open, where the video decoders come from, and the credits.</summary>
public sealed class FormatsWindow : Window
{
    public FormatsWindow(Window owner, IEnumerable<string> imageExtensions, IEnumerable<string> videoExtensions)
    {
        Owner = owner;
        Title = L.T("formats.title");
        Width = 820;
        Height = 560;
        SetResourceReference(BackgroundProperty, "WindowBrush");
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        SourceInitialized += (_, _) => MainWindow.ApplyTitleBarColors(this);

        var text = new TextBox
        {
            Text = Describe(imageExtensions, videoExtensions),
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(16),
            FontSize = 13,
        };
        text.SetResourceReference(BackgroundProperty, "WindowBrush");
        text.SetResourceReference(ForegroundProperty, "TextBrush");

        var openFolder = new Button
        {
            Content = new TextBlock { Text = L.T("formats.codecs"), Margin = new Thickness(14, 0, 14, 0) },
            FontFamily = SystemFonts.MessageFontFamily,
            FontSize = 13,
            MinWidth = 160,
            Padding = new Thickness(12, 0, 12, 0),
            Height = 32,
            Margin = new Thickness(16, 8, 16, 12),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        openFolder.SetResourceReference(ForegroundProperty, "TextBrush");
        openFolder.SetResourceReference(BackgroundProperty, "HoverBrush");
        openFolder.Click += (_, _) =>
        {
            Directory.CreateDirectory(FfmpegPlayer.CodecsFolder);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{FfmpegPlayer.CodecsFolder}\""));
        };

        // Windows only lets the user choose default apps, so the most the app can do is
        // open the right Settings page.
        var defaults = new Button
        {
            Content = new TextBlock { Text = L.T("formats.defaults"), Margin = new Thickness(14, 0, 14, 0) },
            FontFamily = SystemFonts.MessageFontFamily,
            FontSize = 13,
            MinWidth = 160,
            Padding = new Thickness(12, 0, 12, 0),
            Height = 32,
            Margin = new Thickness(0, 8, 16, 12),
            ToolTip = L.T("formats.defaults.tip", L.T("app.name")),
        };
        defaults.SetResourceReference(ForegroundProperty, "TextBrush");
        defaults.SetResourceReference(BackgroundProperty, "HoverBrush");
        defaults.Click += (_, _) => Process.Start(
            new ProcessStartInfo("ms-settings:defaultapps?registeredAppUser=ReiksViewer") { UseShellExecute = true });

        // The app never goes online by itself. This only hands the address to your browser.
        var updates = new Button
        {
            Content = new TextBlock { Text = L.T("formats.updates"), Margin = new Thickness(14, 0, 14, 0) },
            FontFamily = SystemFonts.MessageFontFamily,
            FontSize = 13,
            MinWidth = 160,
            Padding = new Thickness(12, 0, 12, 0),
            Height = 32,
            Margin = new Thickness(0, 8, 16, 12),
            ToolTip = L.T("formats.updates.tip", L.T("app.name")),
        };
        updates.SetResourceReference(ForegroundProperty, "TextBrush");
        updates.SetResourceReference(BackgroundProperty, "HoverBrush");
        updates.Click += (_, _) => Process.Start(
            new ProcessStartInfo("https://github.com/reiks-apps/reiks-viewer/releases/latest") { UseShellExecute = true });

        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        buttons.Children.Add(openFolder);
        buttons.Children.Add(defaults);
        buttons.Children.Add(updates);

        var layout = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        layout.Children.Add(buttons);
        layout.Children.Add(text);
        Content = layout;
    }

    static string Describe(IEnumerable<string> imageExtensions, IEnumerable<string> videoExtensions)
    {
        string List(IEnumerable<string> extensions) =>
            string.Join("  ", extensions.Select(e => e.TrimStart('.')).OrderBy(e => e));

        string decoders = FfmpegPlayer.EnsureLoaded()
            ? L.T("f.loaded", FfmpegPlayer.Version, FfmpegPlayer.CodecsFolder, FfmpegPlayer.License)
            : L.T("f.missing", FfmpegPlayer.LoadError, FfmpegPlayer.CodecsFolder);
        string version = (typeof(FormatsWindow).Assembly.GetName().Version ?? new Version(1, 0, 0)).ToString(3);

        return string.Join("\n\n", new[]
        {
            $"{L.T("app.name").ToUpper()} {version}\n{L.T("f.intro", L.T("app.name"))}",
            $"{L.T("f.decoders")}\n{L.T("f.decoders.body")}\n\n{decoders}",
            $"{L.T("f.pictures")}\n{List(imageExtensions)}\n{L.T("f.animated")}",
            $"{L.T("f.video")}\n{List(videoExtensions)}\n{L.T("f.video.gpu")}\n{L.T("f.video.depends")}",
            $"{L.T("f.credits")}\n{L.T("f.credits.ffmpeg")}",
            L.T("f.credits.replace"),
            "FFmpeg.AutoGen (https://github.com/Ruslan-B/FFmpeg.AutoGen), MIT, (c) Ruslan Balanukhin.",
            L.T("f.credits.trademark"),
        });
    }
}
