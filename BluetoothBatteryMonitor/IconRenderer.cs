using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace XboxControllerBattery;

/// <summary>
/// Renders a tray icon showing the battery percentage as text on a color-coded background.
/// Color conveys state (green/amber/red/charging/unknown); the number conveys the level.
/// </summary>
public static class IconRenderer
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Icon Render(int? percent, BatteryState state)
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);

        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            using (var path = RoundedRect(new Rectangle(1, 1, size - 2, size - 2), 7))
            using (var brush = new SolidBrush(ColorFor(percent, state)))
            {
                g.FillPath(brush, path);
            }

            string text = percent.HasValue ? percent.Value.ToString() : "?";
            DrawCenteredText(g, text, size);
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone(); // Clone so we can free the native handle below.
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private static void DrawCenteredText(Graphics g, string text, int size)
    {
        // Shrink the font for 3-digit values ("100") so it still fits.
        float emSize = text.Length >= 3 ? 13f : 17f;
        using var font = new Font("Segoe UI", emSize, FontStyle.Bold, GraphicsUnit.Pixel);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        var rect = new RectangleF(0, 0, size, size);
        using var shadow = new SolidBrush(Color.FromArgb(110, 0, 0, 0));
        g.DrawString(text, font, shadow, new RectangleF(0.5f, 1f, size, size), format);
        g.DrawString(text, font, Brushes.White, rect, format);
    }

    private static Color ColorFor(int? percent, BatteryState state)
    {
        if (state == BatteryState.Charging)
            return Color.FromArgb(0x1E, 0x88, 0xE5); // blue
        if (state == BatteryState.NotPresent || !percent.HasValue)
            return Color.FromArgb(0x6E, 0x6E, 0x6E); // gray
        if (percent.Value <= 20)
            return Color.FromArgb(0xD3, 0x2F, 0x2F); // red
        if (percent.Value <= 50)
            return Color.FromArgb(0xF5, 0x9E, 0x0B); // amber
        return Color.FromArgb(0x2E, 0x7D, 0x32);     // green
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
