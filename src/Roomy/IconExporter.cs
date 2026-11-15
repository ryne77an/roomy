using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Roomy.Controls;

namespace Roomy;

/// <summary>
/// Renders the logo control into a multi-resolution .ico plus a 512px PNG.
/// Small frames are stored as uncompressed 32bpp DIBs and only 128/256 as PNG: the shell,
/// GDI (+ <c>System.Drawing.Icon</c> used for the tray) and installers all read DIB frames,
/// while PNG-only icons are ignored by some Windows surfaces.
/// </summary>
internal static class IconExporter
{
    public static void Export(string icoPath)
    {
        int[] sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256];
        var frames = sizes.Select(s => s <= 64 ? Dib(Render(s)) : Png(Render(s))).ToList();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(icoPath))!);
        using (var fs = File.Create(icoPath))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write((short)0);
            bw.Write((short)1);
            bw.Write((short)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                byte dim = (byte)(sizes[i] >= 256 ? 0 : sizes[i]);
                bw.Write(dim);
                bw.Write(dim);
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((short)1);
                bw.Write((short)32);
                bw.Write(frames[i].Length);
                bw.Write(offset);
                offset += frames[i].Length;
            }
            foreach (var f in frames) bw.Write(f);
        }

        File.WriteAllBytes(Path.ChangeExtension(icoPath, ".png"), Png(Render(512)));
    }

    /// <summary>32bpp BGRA DIB frame in the layout .ico expects (BITMAPINFOHEADER, bottom-up XOR, AND mask).</summary>
    static byte[] Dib(BitmapSource source)
    {
        int w = source.PixelWidth, h = source.PixelHeight;
        var straight = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[w * h * 4];
        straight.CopyPixels(pixels, w * 4, 0);

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(40);              // biSize
        bw.Write(w);               // biWidth
        bw.Write(h * 2);           // biHeight: XOR image + AND mask
        bw.Write((short)1);        // biPlanes
        bw.Write((short)32);       // biBitCount
        bw.Write(0);               // biCompression = BI_RGB
        bw.Write(0);               // biSizeImage
        bw.Write(0);               // biXPelsPerMeter
        bw.Write(0);               // biYPelsPerMeter
        bw.Write(0);               // biClrUsed
        bw.Write(0);               // biClrImportant

        for (int y = h - 1; y >= 0; y--)
            bw.Write(pixels, y * w * 4, w * 4);

        // The AND mask is unused with 32bpp alpha, but must be present and padded to 32 bits.
        int maskStride = ((w + 31) / 32) * 4;
        bw.Write(new byte[maskStride * h]);
        bw.Flush();
        return ms.ToArray();
    }

    /// <summary>Store display images: transparent tile icons plus graphite box/poster art.</summary>
    public static void ExportStoreArt(string dir)
    {
        Directory.CreateDirectory(dir);
        foreach (var s in new[] { 300, 150, 71 })
            File.WriteAllBytes(Path.Combine(dir, $"tile-{s}x{s}.png"), Png(Render(s)));
        File.WriteAllBytes(Path.Combine(dir, "boxart-1080x1080.png"), Png(Art(1080, 1080, 470, false)));
        File.WriteAllBytes(Path.Combine(dir, "poster-720x1080.png"), Png(Art(720, 1080, 300, true)));
    }

    static BitmapSource Art(int w, int h, int logo, bool withName)
    {
        var bg = (Brush)Application.Current.FindResource("BgBrush");
        var grid = new System.Windows.Controls.Grid { Width = w, Height = h, Background = bg };
        var stack = new System.Windows.Controls.StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        stack.Children.Add(new LogoMark { Width = logo, Height = logo });
        if (withName)
            stack.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Roomy", FontSize = 96, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 48, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontFamily = (FontFamily)Application.Current.FindResource("DisplayFont"),
            });
        grid.Children.Add(stack);
        grid.Measure(new Size(w, h));
        grid.Arrange(new Rect(0, 0, w, h));
        grid.UpdateLayout();
        var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(grid);
        return rtb;
    }

    static BitmapSource Render(int size)
    {
        var logo = new LogoMark { Width = size, Height = size };
        logo.Measure(new Size(size, size));
        logo.Arrange(new Rect(0, 0, size, size));
        logo.UpdateLayout();
        var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(logo);
        return rtb;
    }

    static byte[] Png(BitmapSource img)
    {
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(img));
        using var ms = new MemoryStream();
        enc.Save(ms);
        return ms.ToArray();
    }
}
