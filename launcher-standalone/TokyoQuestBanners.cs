using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace TokyoWorldLauncher;

internal static class TokyoQuestBanners
{
    public static void Install(string gameRoot, IProgress<string> log)
    {
        string folder = Path.Combine(gameRoot, "kubejs", "assets", "tokyoworld",
            "textures", "gui", "quests");
        Directory.CreateDirectory(folder);
        Make(folder, "tokyo_early.png", "01", "EARLY GAME", "PRZETRWAJ · ZBUDUJ · ODKRYJ",
            Color.FromArgb(64, 231, 171), Color.FromArgb(27, 89, 94));
        Make(folder, "tokyo_mid.png", "02", "MID GAME", "ODKRYJ SIEĆ · URUCHOM PRODUKCJĘ",
            Color.FromArgb(246, 166, 74), Color.FromArgb(99, 52, 25));
        Make(folder, "tokyo_late.png", "03", "LATE GAME", "ZDOMINUJ ENERGIĘ · SIĘGNIJ DALEJ",
            Color.FromArgb(244, 82, 153), Color.FromArgb(75, 28, 102));
        log.Report("Questbook: szerokie bannery Early, Mid i Late Game gotowe.");
    }

    private static void Make(string folder, string file, string number, string title,
        string subtitle, Color accent, Color secondary)
    {
        using var canvas = new Bitmap(1200, 390);
        using var g = Graphics.FromImage(canvas);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using (var bg = new LinearGradientBrush(new Rectangle(0, 0, 1200, 390),
            Color.FromArgb(10, 15, 29), secondary, 0f))
            g.FillRectangle(bg, 0, 0, 1200, 390);
        using (var thin = new Pen(Color.FromArgb(48, accent), 2))
        {
            for (int i = -280; i <= 1600; i += 64)
                g.DrawLine(thin, i, 0, i - 285, 390);
            for (int y = 48; y < 390; y += 67)
                g.DrawLine(thin, 0, y, 1200, y);
        }
        using (var halo = new SolidBrush(Color.FromArgb(55, accent)))
            g.FillEllipse(halo, 780, -150, 510, 510);
        using (var line = new Pen(accent, 6))
        {
            g.DrawLine(line, 66, 62, 66, 326);
            g.DrawLine(line, 70, 326, 1100, 326);
        }
        using var brand = TokyoBranding.LoadLogo();
        g.DrawImage(brand, new Rectangle(870, 52, 275, 275));
        using var numberFont = new Font("Segoe UI Black", 30, FontStyle.Bold);
        using var titleFont = new Font("Segoe UI Black", 70, FontStyle.Bold);
        using var smallFont = new Font("Segoe UI Semibold", 23, FontStyle.Bold);
        using var white = new SolidBrush(Color.FromArgb(243, 245, 250));
        using var strong = new SolidBrush(accent);
        g.DrawString("TOKYO WORLD   /   ROZDZIAŁ " + number, numberFont, strong, 103, 62);
        g.DrawString(title, titleFont, white, 100, 122);
        g.DrawString(subtitle, smallFont, strong, 105, 254);
        canvas.Save(Path.Combine(folder, file), ImageFormat.Png);
    }
}
