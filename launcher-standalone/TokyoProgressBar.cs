using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TokyoWorldLauncher;

internal sealed class TokyoProgressBar : Control
{
    private int progress;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Progress
    {
        get => progress;
        set { progress = Math.Clamp(value, 0, 100); Invalidate(); }
    }

    public TokyoProgressBar()
    {
        DoubleBuffered = true;
        Height = 22;
        Cursor = Cursors.Hand; // Click to view full logs.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Math.Max(2, Width - 1), Math.Max(2, Height - 1));
        using (var path = Rounded(bounds, 9))
        using (var bg = new SolidBrush(Color.FromArgb(32, 37, 55)))
            g.FillPath(bg, path);
        if (progress > 0)
        {
            int w = Math.Max(9, bounds.Width * progress / 100);
            using var clip = Rounded(new Rectangle(0, 0, w, bounds.Height), 9);
            using var gradient = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, w), bounds.Height),
                Color.FromArgb(254, 55, 133), Color.FromArgb(127, 31, 100), LinearGradientMode.Horizontal);
            g.FillPath(gradient, clip);
        }
        using var font = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
        string caption = progress + "%";
        var width = g.MeasureString(caption, font).Width;
        using var brush = new SolidBrush(Color.White);
        g.DrawString(caption, font, brush, (Width - width) / 2f, (Height - font.Height) / 2f - 1f);
    }

    private static GraphicsPath Rounded(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Min(Math.Min(r.Width, r.Height), radius * 2);
        if (d < 2) { path.AddRectangle(r); return path; }
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
