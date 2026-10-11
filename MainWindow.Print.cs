using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReiksViewer;

// Printing: the picture as it is shown, as large as fits on the page. For a video, the frame
// on screen. Windows' own print dialog and print system do the rest.
public partial class MainWindow
{
    // An adjusted or turned picture is redrawn into a copy first; beyond this size the copy
    // is made smaller, which still leaves more detail than a printer puts on a page.
    const double MaxPrintPixels = 40_000_000;

    void PrintCurrent()
    {
        if (_hidden || Img.Source is not BitmapSource shown) return;
        if (_videoPlaying) SetPlaying(false);
        ClosePanels();
        try
        {
            // Taken before the dialog opens: a video's picture keeps changing.
            var picture = PrintablePicture(shown);
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true) return;

            // Where this printer can put ink, in 1/96 inch from the corner of the paper. A
            // driver that doesn't say gets a quarter-inch margin instead.
            var page = new Rect(24, 24, dialog.PrintableAreaWidth - 48, dialog.PrintableAreaHeight - 48);
            try
            {
                var area = dialog.PrintQueue.GetPrintCapabilities(dialog.PrintTicket).PageImageableArea;
                if (area != null) page = new Rect(area.OriginWidth, area.OriginHeight, area.ExtentWidth, area.ExtentHeight);
            }
            catch (PrintQueueException)
            {
            }
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen()) DrawOnPage(context, picture, page);

            // The job's name shows in the printer's queue, so "hide file names" covers it too.
            bool named = !_settings.HideFileNames && _index >= 0 && _index < _files.Count;
            dialog.PrintVisual(visual, named ? Path.GetFileName(_files[_index]) : L.T("app.name"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, L.T("msg.cantprint", ex.Message), L.T("app.name"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
            var copy = new WriteableBitmap(shown);
            copy.Freeze();
            return copy;
        }

        // The same picture element again, off screen, at the picture's own size.
        double shrink = Math.Min(1, Math.Sqrt(MaxPrintPixels / ((double)shown.PixelWidth * shown.PixelHeight)));
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

    /// <summary>
    /// Draws the picture centred and as large as fits in the page area. A wide picture on
    /// upright paper (or the other way round) is turned a quarter so that it fills the page.
    /// </summary>
    static void DrawOnPage(DrawingContext context, BitmapSource picture, Rect page)
    {
        bool turn = (picture.PixelWidth > picture.PixelHeight) != (page.Width > page.Height)
                    && picture.PixelWidth != picture.PixelHeight;
        double across = turn ? picture.PixelHeight : picture.PixelWidth, down = turn ? picture.PixelWidth : picture.PixelHeight;
        double scale = Math.Min(page.Width / across, page.Height / down);
        double width = picture.PixelWidth * scale, height = picture.PixelHeight * scale;
        var centre = new Point(page.X + page.Width / 2, page.Y + page.Height / 2);

        if (turn) context.PushTransform(new RotateTransform(90, centre.X, centre.Y));
        context.DrawImage(picture, new Rect(centre.X - width / 2, centre.Y - height / 2, width, height));
        if (turn) context.Pop();
    }
}
