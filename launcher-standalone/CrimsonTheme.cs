using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TokyoWorldLauncher;

internal static class CrimsonTheme
{
    public static readonly Color Background = Color.FromArgb(10, 11, 23);
    public static readonly Color Surface = Color.FromArgb(21, 21, 38);
    public static readonly Color Surface2 = Color.FromArgb(29, 28, 49);
    public static readonly Color Foreground = Color.FromArgb(248, 247, 252);
    public static readonly Color Muted = Color.FromArgb(168, 164, 187);
    public static readonly Color Pink = Color.FromArgb(243, 48, 117);
    public static readonly Color SoftPink = Color.FromArgb(255, 111, 170);
    public static readonly Color Border = Color.FromArgb(100, 62, 101);
    public static readonly Color Success = Color.FromArgb(74, 217, 159);

    public static Button Button(string name, int width = 140, int height = 38, bool primary = false)
    {
        var result = new Button
        {
            Text = name, Size = new Size(width, height),
            ForeColor = Foreground,
            BackColor = primary ? Pink : Surface2,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 10),
            Cursor = Cursors.Hand,
            Margin = new Padding(5)
        };
        result.FlatAppearance.BorderSize = 1;
        result.FlatAppearance.BorderColor = primary ? SoftPink : Border;
        result.FlatAppearance.MouseOverBackColor = Color.FromArgb(109, 35, 87);
        return result;
    }

    public static Label Label(string text, float size = 10, bool bold = false)
        => new()
        {
            Text = text, AutoSize = true,
            ForeColor = bold ? Foreground : Muted,
            BackColor = Color.Transparent,
            Font = new Font(bold ? "Segoe UI Semibold" : "Segoe UI", size,
                bold ? FontStyle.Bold : FontStyle.Regular),
        };

    public static TextBox SearchBox(string placeholder)
        => new()
        {
            PlaceholderText = placeholder,
            BackColor = Surface2, ForeColor = Foreground,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 11)
        };
}

internal sealed class CrimsonCard : Panel
{
    public CrimsonCard()
    {
        DoubleBuffered = true;
        BackColor = CrimsonTheme.Surface;
        ForeColor = CrimsonTheme.Foreground;
        Padding = new Padding(18);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using var path = Rounded(bounds, 14);
        using var pen = new Pen(Color.FromArgb(134, 69, 113), 1.5f);
        g.DrawPath(pen, path);
        using var highlight = new Pen(Color.FromArgb(164, 239, 65, 127), 2);
        g.DrawLine(highlight, 20, 1, Math.Min(145, Width - 20), 1);
    }

    private static GraphicsPath Rounded(Rectangle r, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.Left, r.Top, diameter, diameter, 180, 90);
        path.AddArc(r.Right - diameter, r.Top, diameter, diameter, 270, 90);
        path.AddArc(r.Right - diameter, r.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(r.Left, r.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class CrimsonGradientButton : Button
{
    public CrimsonGradientButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new Rectangle(1, 2, Math.Max(2, Width - 3), Math.Max(2, Height - 5));
        using var brush = new LinearGradientBrush(r,
            Enabled ? Color.FromArgb(244, 40, 116) : Color.FromArgb(83, 47, 66),
            Enabled ? Color.FromArgb(159, 24, 87) : Color.FromArgb(71, 45, 65),
            LinearGradientMode.Horizontal);
        using var path = new GraphicsPath();
        int d = Math.Min(24, r.Height);
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
        using var border = new Pen(Enabled ? CrimsonTheme.SoftPink : CrimsonTheme.Border, 2f);
        g.DrawPath(border, path);
        using var text = new SolidBrush(CrimsonTheme.Foreground);
        using var font = new Font("Segoe UI Black", 24, FontStyle.Bold, GraphicsUnit.Pixel);
        var size = g.MeasureString(Text, font);
        g.DrawString(Text, font, text, (Width - size.Width) / 2,
            (Height - size.Height) / 2 - 2);
    }
}

internal sealed class CrimsonHero : Panel
{
    public CrimsonHero()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = CrimsonTheme.Background;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        var state = g.Save();
        g.ScaleTransform(Width / 1060f, Height / 246f);
        using (var sky = new LinearGradientBrush(new Rectangle(0, 0, 1060, 246),
                   Color.FromArgb(18, 13, 34), Color.FromArgb(65, 18, 53), 0f))
            g.FillRectangle(sky, 0, 0, 1060, 246);
        using (var stripes = new Pen(Color.FromArgb(30, 220, 55, 115), 1))
            for (int x = -130; x < 1150; x += 52)
                g.DrawLine(stripes, x, 0, x + 180, 246);

        // Bright moon to the right, with repeated restrained rings.
        for (int i = 5; i >= 1; i--)
        {
            using var haze = new SolidBrush(Color.FromArgb(i * 3, 244, 58, 143));
            g.FillEllipse(haze, 813 - i * 9, -62 - i * 9, 225 + i * 18, 225 + i * 18);
        }
        using (var moon = new LinearGradientBrush(new Rectangle(819, -45, 219, 219),
                   Color.FromArgb(255, 104, 165), Color.FromArgb(205, 41, 111), 90f))
            g.FillEllipse(moon, 819, -45, 219, 219);

        // Layered cyber-Tokyo skyline, drawn as vectors at any DPI.
        var rng = new Random(1707);
        for (int x = 630; x < 1090;)
        {
            int w = rng.Next(14, 37), top = rng.Next(90, 190);
            using (var building = new SolidBrush(Color.FromArgb(13, 15, 30)))
                g.FillRectangle(building, x, top, w, 246 - top);
            using (var windows = new SolidBrush(Color.FromArgb(110, 244, 70, 142)))
                for (int y = top + 13; y < 225; y += 16)
                    for (int wx = x + 6; wx < x + w - 2; wx += 10)
                        if (rng.Next(4) != 0) g.FillRectangle(windows, wx, y, 3, 5);
            x += w + rng.Next(3, 8);
        }
        using (var temple = new Pen(Color.FromArgb(243, 53, 121), 8))
        {
            temple.StartCap = LineCap.Square;
            temple.EndCap = LineCap.Square;
            g.DrawLine(temple, 857, 152, 857, 245);
            g.DrawLine(temple, 951, 152, 951, 245);
            g.DrawLine(temple, 836, 147, 974, 147);
            g.DrawLine(temple, 850, 168, 961, 168);
        }

        // Main vector logo: no low-resolution stretching.
        using (var outer = new Pen(Color.FromArgb(250, 80, 146), 4))
            g.DrawEllipse(outer, 42, 42, 151, 151);
        using (var glow = new Pen(Color.FromArgb(90, 253, 56, 133), 13))
            g.DrawEllipse(glow, 48, 48, 139, 139);
        using (var plate = new SolidBrush(Color.FromArgb(37, 17, 47)))
            g.FillEllipse(plate, 54, 54, 127, 127);
        using (var mark = new Font("Segoe UI Black", 66f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (var white = new SolidBrush(CrimsonTheme.Foreground))
        using (var pink = new SolidBrush(CrimsonTheme.SoftPink))
        {
            g.DrawString("TW", mark, pink, 56, 70);
            g.DrawString("TW", mark, white, 53, 67);
        }
        using (var title = new Font("Segoe UI Black", 55, FontStyle.Bold, GraphicsUnit.Pixel))
        using (var upper = new SolidBrush(Color.FromArgb(250, 250, 252)))
        using (var lower = new SolidBrush(Color.FromArgb(252, 63, 137)))
        {
            g.DrawString("TOKYO", title, upper, 226, 57);
            g.DrawString("WORLD", title, lower, 226, 124);
        }
        using (var small = new Font("Segoe UI Semibold", 13, FontStyle.Bold, GraphicsUnit.Pixel))
        using (var muted = new SolidBrush(CrimsonTheme.Muted))
            g.DrawString("MINECRAFT 1.20.1  •  FORGE  •  EXPERT MODPACK",
                small, muted, 233, 209);

        // Cherry blossom accents against the moon, skyline and left edge.
        for (int i = 0; i < 38; i++)
        {
            float px = (float)(rng.NextDouble() < .5 ? rng.Next(8, 260) : rng.Next(700, 1060));
            float py = rng.Next(3, 231);
            using var petal = new SolidBrush(Color.FromArgb(rng.Next(80, 185), 254, 113, 179));
            g.FillEllipse(petal, px, py, rng.Next(3, 7), rng.Next(6, 12));
        }
        using var frame = new Pen(CrimsonTheme.Pink, 2);
        g.DrawLine(frame, 5, 244, 1055, 244);
        g.Restore(state);
    }
}
