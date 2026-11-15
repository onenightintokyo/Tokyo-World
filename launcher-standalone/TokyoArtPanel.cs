using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TokyoWorldLauncher;

/// <summary>Branded dark-neon Tokyo skyline banner; intentionally vector-rendered.</summary>
internal sealed class TokyoArtPanel : Panel
{
    public TokyoArtPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(8, 11, 22);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        var state = g.Save();
        g.ScaleTransform(Width / 900f, Height / 310f);

        using (var sky = new LinearGradientBrush(new Rectangle(0, 0, 900, 310),
                   Color.FromArgb(9, 11, 24), Color.FromArgb(38, 15, 48), 0f))
            g.FillRectangle(sky, 0, 0, 900, 310);

        // Subtle geometric grid, in the same purple/red/cyan palette as the TW icon.
        using (var gridPen = new Pen(Color.FromArgb(34, 65, 204, 213), 1f))
        {
            for (int i = -380; i < 1300; i += 52)
                g.DrawLine(gridPen, i, 0, i + 240, 310);
            for (int y = 28; y < 310; y += 51)
                g.DrawLine(gridPen, 0, y, 900, y);
        }

        // Large electric sun behind the right-hand architecture.
        for (int i = 5; i > 0; i--)
        {
            using var haze = new SolidBrush(Color.FromArgb(8 + i * 2, 253, 45, 125));
            g.FillEllipse(haze, 640 - i * 16, 7 - i * 16, 255 + i * 32, 255 + i * 32);
        }
        using (var orb = new LinearGradientBrush(new Rectangle(660, 18, 238, 238),
                   Color.FromArgb(253, 83, 129), Color.FromArgb(145, 31, 90), 90f))
            g.FillEllipse(orb, 660, 18, 238, 238);
        using (var stripe = new SolidBrush(Color.FromArgb(40, 16, 26, 49)))
            for (int y = 146; y < 257; y += 11)
                g.FillRectangle(stripe, 650, y, 240, 4);

        var random = new Random(616);
        // Parallax skyline with cyan/pink window slits.
        for (int x = 520; x < 940;)
        {
            int width = random.Next(16, 42);
            int height = random.Next(86, 205);
            using var building = new SolidBrush(Color.FromArgb(15, 19, 33));
            g.FillRectangle(building, x, height, width, 310 - height);
            using var windows = new SolidBrush(Color.FromArgb(random.Next(65, 130),
                random.Next(2) == 0 ? 232 : 62, 174, 209));
            for (int wx = x + 7; wx < x + width - 3; wx += 10)
                for (int wy = height + 15; wy < 285; wy += 17)
                    if (random.Next(5) != 0)
                        g.FillRectangle(windows, wx, wy, 3, 6);
            x += width + random.Next(2, 9);
        }

        // Iconic Japanese torii, anchored to the skyline.
        using (var glow = new Pen(Color.FromArgb(50, 251, 55, 105), 24f))
        {
            g.DrawLine(glow, 704, 142, 704, 303);
            g.DrawLine(glow, 833, 142, 833, 303);
            g.DrawLine(glow, 676, 139, 860, 139);
        }
        using (var gate = new Pen(Color.FromArgb(255, 51, 98), 11f))
        {
            gate.StartCap = LineCap.Square;
            gate.EndCap = LineCap.Square;
            g.DrawLine(gate, 705, 149, 705, 306);
            g.DrawLine(gate, 834, 149, 834, 306);
            g.DrawLine(gate, 675, 140, 865, 140);
            g.DrawLine(gate, 690, 165, 849, 165);
        }

        // Legible left brand plate and restrained chapter typography.
        using (var plate = new LinearGradientBrush(new Rectangle(0, 0, 620, 310),
                   Color.FromArgb(246, 9, 13, 25), Color.FromArgb(15, 9, 13, 25), 0f))
            g.FillRectangle(plate, 0, 0, 620, 310);
        using (var accent = new Pen(Color.FromArgb(231, 45, 94), 4))
        {
            g.DrawLine(accent, 18, 18, 18, 288);
            g.DrawLine(accent, 18, 288, 518, 288);
        }

        using (var name = new Font("Segoe UI Black", 56f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (var sub = new Font("Segoe UI Semibold", 12f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (var white = new SolidBrush(Color.FromArgb(249, 248, 251)))
        using (var cyan = new SolidBrush(Color.FromArgb(81, 226, 222)))
        using (var muted = new SolidBrush(Color.FromArgb(190, 199, 216)))
        {
            g.DrawString("TOKYO", name, white, 223, 81);
            g.DrawString("WORLD", name, cyan, 223, 147);
            g.DrawString("A NEW WORLD BEYOND LIMITS", sub, muted, 227, 231);
        }

        // Glowing marker petals: subtle, not visual clutter.
        for (int i = 0; i < 18; i++)
        {
            using var brush = new SolidBrush(Color.FromArgb(random.Next(50, 130), 254, 91, 143));
            g.FillEllipse(brush, random.Next(525, 882), random.Next(15, 284), 4, 8);
        }
        g.Restore(state);
    }
}
