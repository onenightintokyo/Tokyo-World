using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TokyoWorldLauncher;

/// <summary>Original vector art inspired by the existing Tokyo World red-sun,
/// torii and neon-city visual identity. Embedded in the EXE; no image download.</summary>
internal sealed class TokyoArtPanel : Panel
{
    public TokyoArtPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(9, 12, 22);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        float sx = Width / 900f, sy = Height / 310f;
        var state = g.Save();
        g.ScaleTransform(sx, sy);

        using (var sky = new LinearGradientBrush(new Rectangle(0, 0, 900, 310),
                   Color.FromArgb(6, 10, 23), Color.FromArgb(35, 19, 44), 90f))
            g.FillRectangle(sky, 0, 0, 900, 310);

        // Neon crimson sun, with a dark-center gradient.
        for (int ring = 4; ring >= 1; ring--)
            using (var halo = new SolidBrush(Color.FromArgb(9 + ring * 3, 235, 28, 66)))
                g.FillEllipse(halo, 447 - ring * 13, 4 - ring * 13, 246 + ring * 26, 246 + ring * 26);
        using (var sun = new LinearGradientBrush(new Rectangle(460, 5, 225, 225),
                   Color.FromArgb(255, 73, 97), Color.FromArgb(141, 24, 62), 90f))
            g.FillEllipse(sun, 460, 5, 225, 225);
        using (var darkBand = new SolidBrush(Color.FromArgb(36, 7, 15, 28)))
            for (int y = 135; y < 216; y += 11)
                g.FillRectangle(darkBand, 468, y, 215, 3);

        // Mt Fuji on the right.
        PointF[] mountain = { new(615, 262), new(743, 88), new(867, 262) };
        using (var mt = new SolidBrush(Color.FromArgb(27, 39, 62)))
            g.FillPolygon(mt, mountain);
        using (var snow = new SolidBrush(Color.FromArgb(183, 194, 210)))
            g.FillPolygon(snow, new PointF[] {
                new(713, 131), new(743, 88), new(774, 131),
                new(756, 123), new(744, 140), new(729, 120) });

        // Tokyo skyline, fixed seed to keep it stable between redraws.
        var rnd = new Random(56);
        for (int x = 0; x < 900;)
        {
            int w = rnd.Next(16, 38);
            int top = rnd.Next(109, 224);
            using (var building = new SolidBrush(Color.FromArgb(10, 15, 28)))
                g.FillRectangle(building, x, top, w, 310 - top);
            using (var windows = new SolidBrush(Color.FromArgb(55, 255, 77, 117)))
                for (int wx = x + 6; wx < x + w - 3; wx += 9)
                    for (int wy = top + 13; wy < 265; wy += 17)
                        if (rnd.Next(4) > 0)
                            g.FillRectangle(windows, wx, wy, 3, 5);
            x += w + rnd.Next(1, 6);
        }

        // Tokyo Tower silhouette.
        using (var tower = new Pen(Color.FromArgb(7, 10, 20), 8))
        {
            g.DrawLine(tower, 397, 182, 425, 47);
            g.DrawLine(tower, 449, 182, 425, 47);
            g.DrawLine(tower, 413, 127, 437, 127);
            g.DrawLine(tower, 404, 163, 444, 163);
            g.DrawLine(tower, 411, 95, 438, 95);
            g.DrawLine(tower, 425, 47, 425, 27);
        }

        // Torii gates, a recurring Tokyo World motif.
        using (var torii = new Pen(Color.FromArgb(230, 37, 62), 12))
        {
            torii.StartCap = LineCap.Square;
            torii.EndCap = LineCap.Square;
            g.DrawLine(torii, 130, 206, 130, 304);
            g.DrawLine(torii, 244, 206, 244, 304);
            g.DrawLine(torii, 101, 203, 275, 203);
            g.DrawLine(torii, 115, 226, 259, 226);
        }

        // Cherry petals in the brand's pink palette.
        for (int i = 0; i < 24; i++)
        {
            int x = rnd.Next(18, 883);
            int y = rnd.Next(22, 287);
            using var petal = new SolidBrush(Color.FromArgb(rnd.Next(90, 175), 244, 94, 151));
            var petalState = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(rnd.Next(-45, 75));
            g.FillEllipse(petal, 0, 0, rnd.Next(4, 9), rnd.Next(9, 17));
            g.Restore(petalState);
        }

        using (var veil = new LinearGradientBrush(new Rectangle(0, 0, 900, 310),
                   Color.FromArgb(72, 7, 11, 23), Color.FromArgb(50, 12, 13, 27), 0))
            g.FillRectangle(veil, 0, 0, 900, 310);

        using (var name = new Font("Segoe UI Black", 64f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (var tagline = new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (var glow = new SolidBrush(Color.FromArgb(115, 255, 50, 76)))
        using (var white = new SolidBrush(Color.FromArgb(247, 247, 249)))
        using (var cyan = new SolidBrush(Color.FromArgb(83, 247, 235)))
        {
            var t = "TOKYO";
            var w = g.MeasureString(t, name).Width;
            g.DrawString(t, name, glow, (900 - w) / 2 + 3, 83);
            g.DrawString(t, name, white, (900 - w) / 2, 81);
            t = "WORLD";
            w = g.MeasureString(t, name).Width;
            g.DrawString(t, name, glow, (900 - w) / 2 + 3, 157);
            g.DrawString(t, name, cyan, (900 - w) / 2, 154);
            t = "T E C H N O L O G Y   •   A D V E N T U R E";
            w = g.MeasureString(t, tagline).Width;
            g.DrawString(t, tagline, white, (900 - w) / 2, 246);
        }
        g.Restore(state);
    }
}
