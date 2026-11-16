using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TokyoWorldLauncher;

internal sealed partial class LauncherWindow
{
    private CrimsonCard? accountCard;
    private CrimsonCard? performanceCard;
    private Panel? contentHolder;
    private readonly Dictionary<string, Button> navButtons = new(StringComparer.Ordinal);
    private CheckBox? lightweightToggle;
    private CheckBox? graphicsToggle;
    private CheckBox? visualExtrasToggle;
    private CheckBox? shaderToggle;

    /// <summary>
    /// Crimson Tokyo v1.7 — functional sidebar pages, redesigned hero and glass-like cards.
    /// Uses original MinecraftEngine, save settings, login flow and play pipeline.
    /// </summary>
    private void BuildCrimsonUI()
    {
        SuspendLayout();
        Text = "TOKYO WORLD  •  LAUNCHER v1.7";
        ClientSize = new Size(1280, 790);
        MinimumSize = new Size(1280, 790);
        MaximumSize = new Size(1280, 790);
        BackColor = CrimsonTheme.Background;
        ForeColor = CrimsonTheme.Foreground;

        // Remove the old low-resolution image; the TW emblem is painted as scalable vectors.
        foreach (var old in Controls.OfType<TokyoArtPanel>().ToArray())
        {
            Controls.Remove(old);
            old.Dispose();
        }
        foreach (var label in Controls.OfType<Label>().ToArray())
        {
            if (label != ramValue && label != status && label != gear)
                label.Visible = false;
        }
        gear.Visible = false;
        status.Visible = false;

        var side = new CrimsonSidebar
        {
            Bounds = new Rectangle(0, 0, 202, 790)
        };
        Controls.Add(side);
        side.BringToFront();

        var smallMark = new Label
        {
            Text = "TW",
            Font = new Font("Segoe UI Black", 25, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = CrimsonTheme.SoftPink,
            BackColor = Color.Transparent,
            Bounds = new Rectangle(28, 18, 140, 55)
        };
        side.Controls.Add(smallMark);
        var subtitle = CrimsonTheme.Label("TOKYO WORLD", 10, true);
        subtitle.ForeColor = CrimsonTheme.Foreground;
        subtitle.Location = new Point(39, 78);
        side.Controls.Add(subtitle);
        var brandLine = new Panel
        {
            BackColor = CrimsonTheme.Pink,
            Bounds = new Rectangle(25, 109, 153, 2)
        };
        side.Controls.Add(brandLine);

        AddNavigation(side, "GŁÓWNA", "⌂", 142, "home");
        AddNavigation(side, "MODY", "▣", 201, "mods");
        AddNavigation(side, "SCREENSHOTY", "▧", 260, "screenshots");
        AddNavigation(side, "SHADERY", "✧", 319, "shaders");
        AddNavigation(side, "USTAWIENIA", "⚙", 378, "settings");

        var version = CrimsonTheme.Label("FORGE 1.20.1", 9);
        version.SetBounds(32, 738, 151, 22);
        version.AutoSize = false;
        side.Controls.Add(version);

        var hero = new CrimsonHero
        {
            Bounds = new Rectangle(203, 0, 1077, 248)
        };
        Controls.Add(hero);
        hero.BringToFront();

        var heading = CrimsonTheme.Label("LAUNCHER v1.7", 10, true);
        heading.SetBounds(1125, 255, 137, 26);
        heading.AutoSize = false;
        heading.TextAlign = ContentAlignment.MiddleRight;
        Controls.Add(heading);

        accountCard = new CrimsonCard { Bounds = new Rectangle(225, 286, 499, 303) };
        performanceCard = new CrimsonCard { Bounds = new Rectangle(746, 286, 508, 303) };
        Controls.Add(accountCard);
        Controls.Add(performanceCard);

        var accountTitle = CrimsonTheme.Label("KONTO", 16, true);
        accountTitle.SetBounds(28, 23, 340, 36);
        accountTitle.AutoSize = false;
        accountCard.Controls.Add(accountTitle);
        var accountSub = CrimsonTheme.Label("Minecraft Java Edition", 10);
        accountSub.SetBounds(30, 61, 340, 24);
        accountSub.AutoSize = false;
        accountCard.Controls.Add(accountSub);

        nickname.Parent = accountCard;
        nickname.SetBounds(29, 96, 438, 42);
        nickname.Font = new Font("Segoe UI Semibold", 15);
        nickname.BackColor = CrimsonTheme.Surface2;
        nickname.ForeColor = CrimsonTheme.Foreground;
        premium.Parent = accountCard;
        premium.SetBounds(30, 158, 239, 30);
        premium.ForeColor = CrimsonTheme.SoftPink;
        premium.BackColor = CrimsonTheme.Surface;
        offline.Parent = accountCard;
        offline.SetBounds(282, 158, 178, 30);
        offline.BackColor = CrimsonTheme.Surface;
        var profile = CrimsonTheme.Label("PROFIL PACZKI", 10, true);
        profile.SetBounds(30, 210, 260, 23);
        profile.AutoSize = false;
        accountCard.Controls.Add(profile);
        var edition = CrimsonTheme.Label("Tokyo World  •  Forge 1.20.1", 12);
        edition.ForeColor = CrimsonTheme.Foreground;
        edition.SetBounds(30, 239, 390, 29);
        edition.AutoSize = false;
        accountCard.Controls.Add(edition);

        var perfTitle = CrimsonTheme.Label("USTAWIENIA GRY", 16, true);
        perfTitle.SetBounds(26, 23, 400, 34);
        perfTitle.AutoSize = false;
        performanceCard.Controls.Add(perfTitle);
        var ramTitle = CrimsonTheme.Label("PAMIĘĆ RAM", 11, true);
        ramTitle.SetBounds(29, 71, 175, 27);
        ramTitle.AutoSize = false;
        performanceCard.Controls.Add(ramTitle);
        ramSlider.Parent = performanceCard;
        ramSlider.SetBounds(23, 110, 338, 39);
        ramSlider.BackColor = CrimsonTheme.Surface;
        ramValue.Parent = performanceCard;
        ramValue.SetBounds(359, 101, 121, 50);
        ramValue.ForeColor = CrimsonTheme.SoftPink;
        ramValue.Font = new Font("Segoe UI Black", 17, FontStyle.Bold);

        lightweightToggle = AddToggle(performanceCard, "Tryb lekki (mniej obciążających modów)",
            170, settings.LightweightMode, value =>
            {
                settings.LightweightMode = value;
                settings.Save();
            });
        graphicsToggle = AddToggle(performanceCard, "Optymalizacja grafiki i renderowania",
            199, settings.OptimizeGraphics, value =>
            {
                settings.OptimizeGraphics = value;
                settings.Save();
            });
        visualExtrasToggle = AddToggle(performanceCard, "Dodatkowe efekty i modele",
            228, settings.ExtraVisualMods, value =>
            {
                settings.ExtraVisualMods = value;
                settings.Save();
            });
        shaderToggle = AddToggle(performanceCard, "Shadery Oculus (wybierz w zakładce SHADERY)",
            257, settings.EnableShaders, value =>
            {
                settings.EnableShaders = value;
                settings.Save();
            });

        play.Text = "▶  GRAJ";
        play.Font = new Font("Segoe UI Black", 23, FontStyle.Bold);
        play.SetBounds(268, 619, 927, 86);
        play.FlatAppearance.BorderSize = 0;
        play.BringToFront();

        progress.SetBounds(300, 728, 862, 26);
        progress.BringToFront();
        progress.Progress = 0;

        contentHolder = new Panel
        {
            Bounds = new Rectangle(212, 272, 1054, 499),
            BackColor = CrimsonTheme.Background,
            Visible = false
        };
        Controls.Add(contentHolder);
        contentHolder.BringToFront();

        // Restore these after the sidebar page overlay was built.
        ChangePage("home");
        ResumeLayout(true);
    }

    private static CheckBox AddToggle(Control parent, string title, int y,
        bool initial, Action<bool> changed)
    {
        var checkbox = new CheckBox
        {
            Text = title,
            Bounds = new Rectangle(31, y, parent.Width - 50, 29),
            ForeColor = CrimsonTheme.Foreground,
            BackColor = CrimsonTheme.Surface,
            Font = new Font("Segoe UI", 10),
            Checked = initial,
            Cursor = Cursors.Hand
        };
        checkbox.CheckedChanged += (_, _) => changed(checkbox.Checked);
        parent.Controls.Add(checkbox);
        return checkbox;
    }

    private void AddNavigation(Control sidebar, string label, string icon, int y, string route)
    {
        var button = CrimsonTheme.Button(icon + "   " + label, 177, 49);
        button.SetBounds(12, y, 178, 48);
        button.Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold);
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Padding = new Padding(11, 0, 0, 0);
        button.Click += (_, _) => ChangePage(route);
        sidebar.Controls.Add(button);
        navButtons[route] = button;
    }

    private void ChangePage(string route)
    {
        if (busy) return;
        if (route == "settings")
        {
            OpenSettings();
            if (lightweightToggle != null) lightweightToggle.Checked = settings.LightweightMode;
            if (graphicsToggle != null) graphicsToggle.Checked = settings.OptimizeGraphics;
            if (visualExtrasToggle != null) visualExtrasToggle.Checked = settings.ExtraVisualMods;
            if (shaderToggle != null) shaderToggle.Checked = settings.EnableShaders;
            return;
        }
        if (accountCard == null || performanceCard == null || contentHolder == null)
            return;
        foreach (var (id, button) in navButtons)
        {
            button.BackColor = id == route
                ? Color.FromArgb(93, 24, 59)
                : CrimsonTheme.Surface2;
            button.ForeColor = id == route ? CrimsonTheme.SoftPink : CrimsonTheme.Foreground;
        }
        bool home = route == "home";
        accountCard.Visible = home;
        performanceCard.Visible = home;
        play.Visible = home;
        progress.Visible = home;
        contentHolder.Visible = !home;
        if (home) return;

        foreach (Control child in contentHolder.Controls.Cast<Control>().ToArray())
        {
            contentHolder.Controls.Remove(child);
            child.Dispose();
        }
        try
        {
            string path = GameRoot();
            Control page = route switch
            {
                "mods" => new TokyoModBrowser(path),
                "screenshots" => new TokyoScreenshotGallery(path),
                "shaders" => new TokyoShaderBrowser(path, settings),
                _ => new Panel()
            };
            page.Dock = DockStyle.Fill;
            contentHolder.Controls.Add(page);
            contentHolder.BringToFront();
        }
        catch (Exception e)
        {
            MessageBox.Show(this, e.Message, "Tokyo World", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            ChangePage("home");
        }
    }
}

internal sealed class CrimsonSidebar : Panel
{
    public CrimsonSidebar()
    {
        DoubleBuffered = true;
        BackColor = CrimsonTheme.Background;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var gradient = new LinearGradientBrush(
            new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)),
            Color.FromArgb(31, 16, 46), Color.FromArgb(10, 12, 27), 90f);
        e.Graphics.FillRectangle(gradient, ClientRectangle);
        using var pen = new Pen(Color.FromArgb(168, 67, 110));
        e.Graphics.DrawLine(pen, Width - 1, 0, Width - 1, Height);
        using var accent = new Pen(CrimsonTheme.Pink, 3);
        e.Graphics.DrawLine(accent, 0, 0, Width, 0);
    }
}
