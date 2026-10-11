using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MediaViewer;

/// <summary>Lists what the app can open, where the video decoders come from, and the credits.</summary>
public sealed class FormatsWindow : Window
{
    public FormatsWindow(Window owner, IEnumerable<string> imageExtensions, IEnumerable<string> videoExtensions)
    {
        Owner = owner;
        Title = "Formats and credits";
        Width = 680;
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
            Content = "Open codecs folder",
            FontFamily = SystemFonts.MessageFontFamily,
            FontSize = 13,
            Width = 160,
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
            Content = "Default app settings",
            FontFamily = SystemFonts.MessageFontFamily,
            FontSize = 13,
            Width = 160,
            Height = 32,
            Margin = new Thickness(0, 8, 16, 12),
            ToolTip = "Opens Windows Settings, where you can make Simple the default for pictures and videos",
        };
        defaults.SetResourceReference(ForegroundProperty, "TextBrush");
        defaults.SetResourceReference(BackgroundProperty, "HoverBrush");
        defaults.Click += (_, _) => Process.Start(
            new ProcessStartInfo("ms-settings:defaultapps?registeredAppUser=Simple") { UseShellExecute = true });

        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        buttons.Children.Add(openFolder);
        buttons.Children.Add(defaults);

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
            ? $"""
               FFmpeg {FfmpegPlayer.Version} is loaded from:
               {FfmpegPlayer.CodecsFolder}
               Build licence: {FfmpegPlayer.License}
               """
            : $"""
               FFmpeg was not found, so nothing can be opened.
               ({FfmpegPlayer.LoadError})

               To add it: download an LGPL "shared" build of FFmpeg 8.1 for Windows from one of the
               build sites linked at https://ffmpeg.org/download.html, and copy the DLL files from
               its "bin" folder into:
               {FfmpegPlayer.CodecsFolder}
               """;

        return $"""
                DECODERS
                Pictures, animations and videos are all decoded by FFmpeg. Nothing is sent anywhere:
                the app has no network code.

                {decoders}

                PICTURES AND ANIMATIONS
                {List(imageExtensions)}
                Animated: gif, apng, webp.

                VIDEO
                {List(videoExtensions)}
                Video is decoded on the graphics card when it supports the format, otherwise on
                the processor. Start the app with --software to always use the processor.
                Whether a given file opens depends on the decoders in the FFmpeg build above.

                CREDITS
                This software uses libraries from the FFmpeg project (https://ffmpeg.org), licensed
                under the GNU Lesser General Public License. The licence text is in
                codecs\FFmpeg-LICENSE.txt, and THIRD-PARTY-NOTICES.txt says where to get the exact
                source code of the build shipped with this app.

                FFmpeg is used as separate DLL files and is not modified. You may replace the DLLs in
                the codecs folder with your own build of FFmpeg 8.x.

                FFmpeg.AutoGen (https://github.com/Ruslan-B/FFmpeg.AutoGen), MIT licence,
                (c) Ruslan Balanukhin.

                FFmpeg is a trademark of Fabrice Bellard, originator of the FFmpeg project.
                This app is not affiliated with or endorsed by the FFmpeg project.
                """;
    }
}
