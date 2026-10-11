using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;

namespace ReiksViewer;

/// <summary>
/// Printing through the part of Windows that every printer driver works with (GDI). A driver
/// can take seconds to answer, so everything here except <see cref="Printer.ShowSettings"/>
/// is meant to be called away from the window's thread.
/// </summary>
static class PrintEngine
{
    // Beyond this the picture is made smaller before it goes to the printer, which still
    // leaves more detail than a printer puts on a page.
    internal const double MaxPixels = 40_000_000;

    /// <summary>A paper size a printer offers: upright, in hundredths of an inch.</summary>
    internal sealed record Paper(string Name, int Kind, double Width, double Height);

    /// <summary>
    /// A page in hundredths of an inch: its size, and the part of it the printer can reach.
    /// </summary>
    internal sealed record PageLayout(double Width, double Height, Rect Printable)
    {
        /// <summary>The same page lying on its side, or stood up again.</summary>
        internal PageLayout Turned() =>
            new(Height, Width, new Rect(Printable.Y, Printable.X, Printable.Height, Printable.Width));

        /// <summary>Another paper size with the same unreachable edges.</summary>
        internal PageLayout WithPaper(double width, double height)
        {
            double right = Width - Printable.Right, bottom = Height - Printable.Bottom;
            return new(width, height, new Rect(Printable.X, Printable.Y,
                                               Math.Max(1, width - Printable.X - right), Math.Max(1, height - Printable.Y - bottom)));
        }
    }

    /// <summary>The printers set up in Windows and which of them is the default. Quick: no printer is contacted.</summary>
    internal static (List<string> Names, string? Standard) ListPrinters()
    {
        var names = PrinterSettings.InstalledPrinters.Cast<string>()
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase).ToList();
        string standard = new PrinterSettings().PrinterName;
        return (names, names.Contains(standard) ? standard : null);
    }

    /// <summary>
    /// Where the picture goes in the part of the page the printer can reach: whole and centred,
    /// or filling it with the overhang cut off. <paramref name="target"/> is in the units of
    /// <paramref name="area"/>, <paramref name="source"/> in the picture's pixels.
    /// </summary>
    internal static void Place(double width, double height, Rect area, bool fill, out Rect target, out Rect source)
    {
        double across = area.Width / width, down = area.Height / height;
        double scale = fill ? Math.Max(across, down) : Math.Min(across, down);
        double usedWidth = Math.Min(width, area.Width / scale), usedHeight = Math.Min(height, area.Height / scale);
        source = new Rect((width - usedWidth) / 2, (height - usedHeight) / 2, usedWidth, usedHeight);
        target = new Rect(area.X + (area.Width - usedWidth * scale) / 2, area.Y + (area.Height - usedHeight * scale) / 2,
                          usedWidth * scale, usedHeight * scale);
    }

    /// <summary>One printer, with what its driver says about it. Creating it asks the driver.</summary>
    internal sealed class Printer
    {
        // The driver is asked from one thread at a time.
        readonly object _gate = new();
        readonly PrinterSettings _settings;
        readonly PageSettings _page;
        readonly List<PaperSize> _sizes = new();
        readonly List<Paper> _papers = new();

        internal string Name { get; }
        internal IReadOnlyList<Paper> Papers => _papers;
        /// <summary>The paper the driver is set to.</summary>
        internal int ChosenPaper { get; private set; }
        internal bool ChosenLandscape { get; private set; }
        internal int ChosenCopies { get; private set; } = 1;
        internal int MaxCopies { get; }

        internal Printer(string name)
        {
            Name = name;
            _settings = new PrinterSettings { PrinterName = name };
            if (!_settings.IsValid) throw new InvalidPrinterException(_settings);
            _page = new PageSettings(_settings);
            foreach (PaperSize size in _settings.PaperSizes)
            {
                if (size.Width > 0 && size.Height > 0) Add(size);
            }
            ChosenPaper = IndexOf(_page.PaperSize);
            MaxCopies = Math.Max(1, _settings.MaximumCopies);

        }

        void Add(PaperSize size)
        {
            _sizes.Add(size);
            _papers.Add(new Paper(size.PaperName, size.RawKind, size.Width, size.Height));
        }

        int IndexOf(PaperSize size)
        {
            int index = _sizes.FindIndex(other => other.RawKind == size.RawKind && size.RawKind != 0);
            if (index < 0) index = _sizes.FindIndex(other => other.Width == size.Width && other.Height == size.Height);
            if (index >= 0) return index;
            // A size of the user's own making that the driver doesn't list.
            Add(size);
            return _sizes.Count - 1;
        }

        /// <summary>The page for this paper and orientation, with the edges this printer leaves blank.</summary>
        internal PageLayout Layout(int paper, bool landscape)
        {
            lock (_gate)
            {
                var size = _sizes[paper];
                _page.PaperSize = size;
                _page.Landscape = landscape;
                // Given for the upright page, whichever way the page is turned.
                var reach = _page.PrintableArea;
                var upright = new PageLayout(size.Width, size.Height,
                                             new Rect(reach.X, reach.Y, Math.Max(0, reach.Width), Math.Max(0, reach.Height)));
                var layout = landscape ? upright.Turned() : upright;
                var printable = Rect.Intersect(layout.Printable, new Rect(0, 0, layout.Width, layout.Height));
                // A driver that gives no sensible answer gets a quarter-inch edge instead.
                if (printable.IsEmpty || printable.Width < layout.Width / 2 || printable.Height < layout.Height / 2)
                    printable = new Rect(25, 25, layout.Width - 50, layout.Height - 50);
                return layout with { Printable = printable };
            }
        }

        /// <summary>
        /// Sends the picture as one page. Returns when Windows has taken the job, which is
        /// before the printer has printed it. <paramref name="toFile"/> writes the driver's
        /// output to a file instead of its port.
        /// </summary>
        internal void Print(BitmapSource picture, int paper, bool landscape, bool fill, int copies, string jobName, string? toFile = null)
        {
            lock (_gate)
            {
                _page.PaperSize = _sizes[paper];
                _page.Landscape = landscape;
                // A driver that can't count copies gets the page that many times instead.
                int pages = copies <= MaxCopies ? 1 : copies, printed = 0;
                _settings.Copies = (short)(pages == 1 ? copies : 1);
                _settings.PrintToFile = toFile != null;
                if (toFile != null) _settings.PrintFileName = toFile;

                Drawing.Bitmap? copy = null;
                using var document = new PrintDocument
                {
                    PrinterSettings = _settings,
                    DefaultPageSettings = _page,
                    DocumentName = jobName,
                    PrintController = new StandardPrintController(),
                };
                document.PrintPage += (_, e) =>
                {
                    var graphics = e.Graphics!;
                    // In the printer's own dots, starting at the corner of what it can reach.
                    graphics.PageUnit = Drawing.GraphicsUnit.Pixel;
                    var reach = graphics.VisibleClipBounds;
                    Place(picture.PixelWidth, picture.PixelHeight, new Rect(reach.X, reach.Y, reach.Width, reach.Height), fill,
                          out var target, out var source);
                    // No more of the picture than the printer has dots for.
                    double pixels = (double)picture.PixelWidth * picture.PixelHeight;
                    copy ??= PaperCopy(picture, Math.Min(1, Math.Min(target.Width / source.Width, Math.Sqrt(MaxPixels / pixels))));
                    double kept = (double)copy.Width / picture.PixelWidth;
                    graphics.DrawImage(copy,
                        new Drawing.RectangleF((float)target.X, (float)target.Y, (float)target.Width, (float)target.Height),
                        new Drawing.RectangleF((float)(source.X * kept), (float)(source.Y * kept),
                                               (float)(source.Width * kept), (float)(source.Height * kept)),
                        Drawing.GraphicsUnit.Pixel);
                    e.HasMorePages = ++printed < pages;
                };
                try
                {
                    document.Print();
                }
                finally
                {
                    copy?.Dispose();
                }
            }
        }

        /// <summary>
        /// Opens the driver's own settings window (paper type, quality and so on), starting
        /// from the given choices. Returns false if it was cancelled; otherwise the Chosen
        /// properties say what it was left at. Call this one from the window's thread.
        /// </summary>
        internal bool ShowSettings(IntPtr owner, int paper, bool landscape, int copies)
        {
            const int Out = 2, Prompt = 4, In = 8, Ok = 1;
            lock (_gate)
            {
                _page.PaperSize = _sizes[paper];
                _page.Landscape = landscape;
                _settings.Copies = (short)Math.Min(copies, MaxCopies);
                IntPtr handle = _settings.GetHdevmode(_page);
                try
                {
                    IntPtr mode = GlobalLock(handle);
                    int answer = DocumentProperties(owner, IntPtr.Zero, Name, mode, mode, In | Out | Prompt);
                    GlobalUnlock(handle);
                    if (answer != Ok) return false;
                    // Both keep what they understand; the printer's own extras travel along unread.
                    _settings.SetHdevmode(handle);
                    _page.SetHdevmode(handle);
                }
                finally
                {
                    GlobalFree(handle);
                }
                ChosenPaper = IndexOf(_page.PaperSize);
                ChosenLandscape = _page.Landscape;
                ChosenCopies = Math.Max(1, (int)_settings.Copies);
                return true;
            }
        }
    }

    /// <summary>
    /// The picture as Windows' printing wants it: opaque, on white as on paper, and made
    /// smaller by <paramref name="shrink"/> if that is below 1.
    /// </summary>
    static unsafe Drawing.Bitmap PaperCopy(BitmapSource picture, double shrink)
    {
        BitmapSource source = picture.Format == PixelFormats.Pbgra32
            ? picture
            : new FormatConvertedBitmap(picture, PixelFormats.Pbgra32, null, 0);
        if (shrink < 1) source = new TransformedBitmap(source, new ScaleTransform(shrink, shrink));
        int width = source.PixelWidth, height = source.PixelHeight;

        var copy = new Drawing.Bitmap(width, height, Drawing.Imaging.PixelFormat.Format24bppRgb);
        var bits = copy.LockBits(new Drawing.Rectangle(0, 0, width, height), Drawing.Imaging.ImageLockMode.WriteOnly,
                                 Drawing.Imaging.PixelFormat.Format24bppRgb);
        try
        {
            // A strip at a time, so that a large picture isn't in memory a third time over.
            const int StripRows = 64;
            var strip = new byte[width * 4 * StripRows];
            for (int top = 0; top < height; top += StripRows)
            {
                int rows = Math.Min(StripRows, height - top);
                source.CopyPixels(new Int32Rect(0, top, width, rows), strip, width * 4, 0);
                fixed (byte* first = strip)
                {
                    for (int row = 0; row < rows; row++)
                    {
                        byte* from = first + (long)row * width * 4;
                        byte* to = (byte*)bits.Scan0 + (long)(top + row) * bits.Stride;
                        for (int x = 0; x < width; x++, from += 4, to += 3)
                        {
                            // The colours are already multiplied by their opacity; the rest is paper.
                            int paper = 255 - from[3];
                            to[0] = (byte)Math.Min(255, from[0] + paper);
                            to[1] = (byte)Math.Min(255, from[1] + paper);
                            to[2] = (byte)Math.Min(255, from[2] + paper);
                        }
                    }
                }
            }
        }
        finally
        {
            copy.UnlockBits(bits);
        }
        return copy;
    }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode)]
    static extern int DocumentProperties(IntPtr owner, IntPtr printer, string deviceName, IntPtr output, IntPtr input, int mode);

    [DllImport("kernel32.dll")]
    static extern IntPtr GlobalLock(IntPtr handle);

    [DllImport("kernel32.dll")]
    static extern bool GlobalUnlock(IntPtr handle);

    [DllImport("kernel32.dll")]
    static extern IntPtr GlobalFree(IntPtr handle);
}
