using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TokyoWorldLauncher;

/// <summary>
/// The approved Concept #4 is an ART-DIRECTED surface, not an approximation made of
/// several independent WinForms panels. Real interactive controls are layered over
/// the clean visual skin, keeping authentication and the existing game pipeline.
/// </summary>
internal sealed partial class LauncherWindow
{
    private readonly List<Control> conceptHomeControls = new();
    private readonly Dictionary<string, ConceptNavItem> conceptNavigation = new();
    private ConceptCanvas? conceptCanvas;
    private Panel? conceptPage;
    private ConceptDot? conceptPremiumDot;
    private ConceptDot? conceptOfflineDot;
    private ConceptSwitch? conceptShaderToggle;
    private ConceptSwitch? conceptVisualToggle;
    private ConceptRamSlider? conceptRam;
    private ConceptValueButton? conceptQuality;
    private Label? conceptUpdateStatus;
    // Read the version embedded in the currently running EXE, never hard-code
    // a historical release number into the launcher artwork or title bar.
    private static string CurrentLauncherVersion
    {
        get
        {
            Version version = typeof(LauncherWindow).Assembly.GetName().Version
                ?? new Version(0, 0, 0);
            return $"v{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";
        }
    }


    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr handle, int msg, IntPtr wparam, IntPtr lparam);

    private void StartWindowDrag()
    {
        ReleaseCapture();
        SendMessage(Handle, 0x00A1, (IntPtr)0x0002, IntPtr.Zero);
    }

    private void BuildConcept4Ui()
    {
        SuspendLayout();
        FormBorderStyle = FormBorderStyle.None;
        ClientSize = new Size(1280, 720);
        AutoScaleMode = AutoScaleMode.None;
        MaximizeBox = false;
        MinimizeBox = true;
        Text = "TOKYO WORLD — CRIMSON TOKYO";
        BackColor = Color.FromArgb(10, 10, 23);

        // Completely detach old v1.6-v1.7 WinForms layout. Reuse ONLY the model
        // controls needed by the original playing / Microsoft-login pipeline.
        var originalControls = Controls.Cast<Control>().ToArray();
        foreach (Control control in originalControls)
        {
            Controls.Remove(control);
            if (control != nickname && control != premium && control != offline &&
                control != ramSlider && control != ramValue &&
                control != status && control != gear &&
                control != play && control != progress)
                control.Dispose();
        }

        conceptCanvas = new ConceptCanvas { Dock = DockStyle.Fill };
        conceptCanvas.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left && e.Y <= 34 && e.X is > 210 and < 1152)
                StartWindowDrag();
        };
        Controls.Add(conceptCanvas);

        // Draggable title strip and working native window controls.
        var drag = new ConceptClickArea { Bounds = new Rectangle(210, 0, 915, 34), HoverEnabled = false };
        drag.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) StartWindowDrag(); };
        conceptCanvas.Controls.Add(drag);

        var minimize = new ConceptClickArea { Bounds = new Rectangle(1154, 0, 38, 33), HoverEnabled = true };
        minimize.Click += (_, _) => WindowState = FormWindowState.Minimized;
        conceptCanvas.Controls.Add(minimize);
        var close = new ConceptClickArea { Bounds = new Rectangle(1234, 0, 43, 34), HoverEnabled = true };
        close.Click += (_, _) => Close();
        conceptCanvas.Controls.Add(close);
        var versionTag = new Label
        {
            Text = CurrentLauncherVersion,
            Bounds = new Rectangle(168, 9, 66, 24),
            AutoSize = false,
            BackColor = Color.FromArgb(13, 12, 25),
            ForeColor = Color.FromArgb(254, 75, 147),
            Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        conceptCanvas.Controls.Add(versionTag);
        var maximize = new ConceptClickArea { Bounds = new Rectangle(1195, 0, 36, 33), HoverEnabled = true };
        var tooltip = new ToolTip();
        tooltip.SetToolTip(maximize, "Stały rozmiar — interfejs dopasowany do konceptu");
        conceptCanvas.Controls.Add(maximize);

        AddConceptNavigation("home", "⌂", "GŁÓWNA", 222);
        AddConceptNavigation("mods", "▣", "MODY", 274);
        AddConceptNavigation("screenshots", "▧", "SCREENSHOTY", 326);
        AddConceptNavigation("shaders", "✧", "SHADERY", 378);
        AddConceptNavigation("settings", "⚙", "USTAWIENIA", 430);

        // Real Minecraft nickname field on top of the artwork's blanked input.
        nickname.Parent = conceptCanvas;
        nickname.SetBounds(332, 285, 279, 31);
        nickname.BorderStyle = BorderStyle.None;
        nickname.BackColor = Color.FromArgb(26, 25, 43);
        nickname.ForeColor = Color.FromArgb(251, 247, 252);
        nickname.Font = new Font("Segoe UI Semibold", 14, FontStyle.Bold);
        nickname.MaxLength = 16;
        conceptHomeControls.Add(nickname);

        // Real radio selection; the underlying model still triggers UpdateMode.
        conceptPremiumDot = new ConceptDot
        {
            Bounds = new Rectangle(237, 356, 26, 26),
            Selected = premium.Checked
        };
        conceptOfflineDot = new ConceptDot
        {
            Bounds = new Rectangle(461, 356, 26, 26),
            Selected = offline.Checked
        };
        var premiumArea = new ConceptClickArea
        {
            Bounds = new Rectangle(267, 350, 186, 35),
            HoverEnabled = false
        };
        premiumArea.Click += (_, _) => SelectConceptPremium(true);
        var offlineArea = new ConceptClickArea
        {
            Bounds = new Rectangle(492, 350, 130, 35),
            HoverEnabled = false
        };
        offlineArea.Click += (_, _) => SelectConceptPremium(false);
        conceptPremiumDot.Cursor = Cursors.Hand;
        conceptOfflineDot.Cursor = Cursors.Hand;
        conceptPremiumDot.Click += (_, _) => SelectConceptPremium(true);
        conceptOfflineDot.Click += (_, _) => SelectConceptPremium(false);
        conceptCanvas.Controls.Add(conceptPremiumDot);
        conceptCanvas.Controls.Add(conceptOfflineDot);
        conceptCanvas.Controls.Add(premiumArea);
        conceptCanvas.Controls.Add(offlineArea);
        premiumArea.BringToFront();
        offlineArea.BringToFront();
        conceptHomeControls.Add(conceptPremiumDot);
        conceptHomeControls.Add(conceptOfflineDot);
        conceptHomeControls.Add(premiumArea);
        conceptHomeControls.Add(offlineArea);
        premium.CheckedChanged += (_, _) => UpdateConceptRadios();
        offline.CheckedChanged += (_, _) => UpdateConceptRadios();

        conceptRam = new ConceptRamSlider
        {
            Bounds = new Rectangle(882, 270, 236, 35),
            Minimum = 2,
            Maximum = 16,
            Value = ramSlider.Value,
        };
        conceptRam.ValueChanged += (_, _) =>
        {
            ramSlider.Value = conceptRam.Value;
            settings.RamMb = conceptRam.Value * 1024;
        };
        conceptCanvas.Controls.Add(conceptRam);
        conceptHomeControls.Add(conceptRam);

        ramValue.Parent = conceptCanvas;
        ramValue.SetBounds(1136, 269, 75, 35);
        ramValue.TextAlign = ContentAlignment.MiddleCenter;
        ramValue.BackColor = Color.FromArgb(22, 22, 35);
        ramValue.ForeColor = Color.FromArgb(255, 115, 172);
        ramValue.Font = new Font("Segoe UI Semibold", 13, FontStyle.Bold);
        conceptHomeControls.Add(ramValue);

        conceptQuality = new ConceptValueButton
        {
            Bounds = new Rectangle(923, 317, 280, 32),
            Caption = CurrentQualityLabel()
        };
        conceptQuality.Click += (_, _) => ShowQualityMenu(conceptQuality);
        conceptCanvas.Controls.Add(conceptQuality);
        conceptHomeControls.Add(conceptQuality);

        var shaderSelect = new ConceptValueButton
        {
            Bounds = new Rectangle(923, 366, 230, 31),
            Caption = "Wybierz shaderpack"
        };
        shaderSelect.Click += (_, _) => ChangeConceptPage("shaders");
        conceptCanvas.Controls.Add(shaderSelect);
        conceptHomeControls.Add(shaderSelect);

        conceptShaderToggle = new ConceptSwitch
        {
            Bounds = new Rectangle(1167, 370, 55, 24),
            IsOn = settings.EnableShaders
        };
        conceptShaderToggle.Changed += (_, _) =>
        {
            settings.EnableShaders = conceptShaderToggle.IsOn;
            settings.Save();
        };
        conceptCanvas.Controls.Add(conceptShaderToggle);
        conceptHomeControls.Add(conceptShaderToggle);

        var visualSelect = new ConceptValueButton
        {
            Bounds = new Rectangle(923, 412, 230, 31),
            Caption = "Dodatkowe tekstury"
        };
        visualSelect.Click += (_, _) => ChangeConceptPage("mods");
        conceptCanvas.Controls.Add(visualSelect);
        conceptHomeControls.Add(visualSelect);

        conceptVisualToggle = new ConceptSwitch
        {
            Bounds = new Rectangle(1167, 415, 55, 24),
            IsOn = settings.ExtraVisualMods
        };
        conceptVisualToggle.Changed += (_, _) =>
        {
            settings.ExtraVisualMods = conceptVisualToggle.IsOn;
            settings.Save();
        };
        conceptCanvas.Controls.Add(conceptVisualToggle);
        conceptHomeControls.Add(conceptVisualToggle);

        conceptUpdateStatus = new Label
        {
            Bounds = new Rectangle(939, 462, 274, 25),
            Text = "Sprawdź aktualizacje",
            BackColor = Color.FromArgb(21, 20, 35),
            ForeColor = Color.FromArgb(101, 229, 178),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        conceptUpdateStatus.Click += async (_, _) => await CheckLauncherUpdateAsync();
        conceptCanvas.Controls.Add(conceptUpdateStatus);
        conceptHomeControls.Add(conceptUpdateStatus);

        // The large GRAJ artwork is already drawn in the approved scene.
        // This is a real transparent hit target, not a fake "button" in the JPG.
        var playHit = new ConceptClickArea
        {
            Bounds = new Rectangle(398, 532, 603, 91),
            HoverEnabled = true
        };
        playHit.Click += async (_, _) => await StartGame();
        conceptCanvas.Controls.Add(playHit);
        conceptHomeControls.Add(playHit);

        // Existing real progress control replaces the concept's fake "100%".
        progress.Parent = conceptCanvas;
        progress.SetBounds(288, 647, 806, 29);
        progress.Progress = 0;
        progress.Cursor = Cursors.Hand;
        conceptHomeControls.Add(progress);

        // Model-only controls keep saving/logging/launch events from Program.cs.
        status.Visible = false;
        gear.Visible = false;
        premium.Visible = false;
        offline.Visible = false;
        ramSlider.Visible = false;
        play.Visible = false;

        conceptPage = new Panel
        {
            Bounds = new Rectangle(197, 208, 1067, 505),
            BackColor = Color.FromArgb(12, 12, 27),
            Visible = false
        };
        conceptCanvas.Controls.Add(conceptPage);
        conceptPage.BringToFront();

        SetConceptNavigation("home");
        UpdateConceptRadios();

        // Fit the entire approved 1280×720 scene proportionally on smaller
        // displays, instead of allowing Windows to silently clip the right side.
        var work = Screen.FromControl(this).WorkingArea;
        float fit = Math.Min(1f, Math.Min(
            (work.Width - 24f) / 1280f, (work.Height - 24f) / 720f));
        if (fit > 0.35f && fit < 0.999f)
        {
            conceptCanvas.Dock = DockStyle.None;
            conceptCanvas.Bounds = new Rectangle(0, 0, 1280, 720);
            conceptCanvas.Scale(new SizeF(fit, fit));
            var fittedSize = new Size(
                (int)Math.Floor(1280 * fit),
                (int)Math.Floor(720 * fit));
            ClientSize = fittedSize;
            conceptCanvas.Bounds = new Rectangle(Point.Empty, fittedSize);
            conceptCanvas.Dock = DockStyle.Fill;
        }
        ResumeLayout(true);
    }

    private void SelectConceptPremium(bool useMicrosoft)
    {
        // RadioButton controls are detached from the old layout: explicitly
        // keep them mutually exclusive instead of relying on WinForms grouping.
        if (useMicrosoft)
        {
            offline.Checked = false;
            premium.Checked = true;
        }
        else
        {
            premium.Checked = false;
            offline.Checked = true;
        }
        UpdateConceptRadios();
    }

    private void UpdateConceptRadios()
    {
        if (conceptPremiumDot != null) conceptPremiumDot.Selected = premium.Checked;
        if (conceptOfflineDot != null) conceptOfflineDot.Selected = offline.Checked;
        if (nickname.Parent == conceptCanvas)
        {
            nickname.BackColor = Color.FromArgb(26, 25, 43);
            nickname.ForeColor = Color.FromArgb(248, 244, 252);
        }
    }

    private void AddConceptNavigation(string route, string icon, string title, int y)
    {
        var nav = new ConceptNavItem
        {
            Bounds = new Rectangle(10, y, 163, 44),
            Symbol = icon,
            Caption = title,
        };
        nav.Click += (_, _) => ChangeConceptPage(route);
        conceptCanvas!.Controls.Add(nav);
        conceptNavigation[route] = nav;
    }

    private void SetConceptNavigation(string active)
    {
        foreach (var (route, nav) in conceptNavigation)
            nav.Selected = route == active;
    }

    private void ChangeConceptPage(string route)
    {
        if (busy || conceptPage == null) return;
        if (route == "settings")
        {
            OpenSettings();
            if (conceptQuality != null) conceptQuality.Caption = CurrentQualityLabel();
            if (conceptShaderToggle != null) conceptShaderToggle.IsOn = settings.EnableShaders;
            if (conceptVisualToggle != null) conceptVisualToggle.IsOn = settings.ExtraVisualMods;
            return;
        }

        bool home = route == "home";
        SetConceptNavigation(route);
        foreach (Control control in conceptHomeControls) control.Visible = home;
        conceptPage.Visible = !home;
        if (home) return;

        foreach (Control old in conceptPage.Controls.Cast<Control>().ToArray())
        {
            conceptPage.Controls.Remove(old);
            old.Dispose();
        }
        try
        {
            string root = GameRoot();
            Control page = route switch
            {
                "mods" => new TokyoModBrowser(root),
                "screenshots" => new TokyoScreenshotGallery(root),
                "shaders" => new TokyoShaderBrowser(root, settings),
                _ => new Panel { BackColor = Color.FromArgb(12, 12, 27) }
            };
            // Tab content uses a 1067×505 logical viewport. On smaller
            // monitors, retain its full controls and enable scrolling.
            if (conceptPage.Width < 1040 || conceptPage.Height < 500)
            {
                conceptPage.AutoScroll = true;
                page.Dock = DockStyle.None;
                page.Size = new Size(1067, 505);
            }
            else
            {
                conceptPage.AutoScroll = false;
                page.Dock = DockStyle.Fill;
            }
            conceptPage.Controls.Add(page);
            conceptPage.BringToFront();
        }
        catch (Exception e)
        {
            MessageBox.Show(this, e.Message, "Tokyo World", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            ChangeConceptPage("home");
        }
    }

    private string CurrentQualityLabel() =>
        settings.LightweightMode ? "Maksymalny FPS" :
        settings.OptimizeGraphics ? "Zbalansowany" : "Jakość grafiki";

    private void ShowQualityMenu(Control anchor)
    {
        using var menu = new ContextMenuStrip
        {
            BackColor = Color.FromArgb(29, 24, 44),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10)
        };
        void AddChoice(string caption, bool optimized, bool lightweight)
        {
            var item = menu.Items.Add(caption);
            item.Click += (_, _) =>
            {
                if (lightweight && !settings.LightweightMode &&
                    MessageBox.Show(this,
                        "Tryb lekki wyłącza Every Compat. Nie otwieraj istniejących światów " +
                        "z jego przedmiotami bez ponownego włączenia moda.\n\nWłączyć tryb lekki?",
                        "Tokyo World — wydajność", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
                settings.OptimizeGraphics = optimized;
                settings.LightweightMode = lightweight;
                settings.Save();
                if (conceptQuality != null) conceptQuality.Caption = CurrentQualityLabel();
            };
        }
        AddChoice("Zbalansowany", true, false);
        AddChoice("Maksymalny FPS (tryb lekki)", true, true);
        AddChoice("Jakość grafiki", false, false);
        menu.Show(anchor, new Point(0, anchor.Height));
    }
}

internal sealed class ConceptCanvas : Panel
{
    private Image? background;

    public ConceptCanvas()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(11, 11, 25);
        string[] candidates =
        {
            Path.Combine(AppContext.BaseDirectory, "CrimsonTokyoConcept4-UI.jpg"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TokyoWorldLauncher", "CrimsonTokyoConcept4-UI.jpg")
        };
        // An initial single-file EXE may carry the approved artwork as an
        // authenticated JPEG payload. Cache it once in per-user AppData so future
        // auto-updated EXEs preserve exactly the same appearance.
        var artworkBytes = ReadEmbeddedArtwork();
        if (artworkBytes != null)
        {
            try
            {
                using var ms = new MemoryStream(artworkBytes);
                using var original = new Bitmap(ms);
                background = new Bitmap(original);
                string cache = candidates[1];
                Directory.CreateDirectory(Path.GetDirectoryName(cache)!);
                if (!File.Exists(cache)) File.WriteAllBytes(cache, artworkBytes);
            }
            catch { background = null; }
        }
        if (background != null) return;
        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            try
            {
                using var ms = new MemoryStream(File.ReadAllBytes(path));
                using var original = new Bitmap(ms);
                background = new Bitmap(original);
                break;
            }
            catch { /* Local artwork unavailable; use vector fallback. */ }
        }
    }

    private static byte[]? ReadEmbeddedArtwork()
    {
        const int footerSize = 64;
        byte[] magic = Encoding.ASCII.GetBytes("TOKYO_CRIMSON_ART_V171__");
        try
        {
            string path = Environment.ProcessPath ?? "";
            if (!File.Exists(path)) return null;
            using var file = File.OpenRead(path);
            if (file.Length < footerSize) return null;
            file.Seek(-footerSize, SeekOrigin.End);
            byte[] footer = new byte[footerSize];
            file.ReadExactly(footer);
            if (!footer.AsSpan(0, magic.Length).SequenceEqual(magic))
                return null;
            long length = BitConverter.ToInt64(footer, 24);
            if (length < 8_000 || length > 8_000_000 || file.Length < length + footerSize)
                return null;
            file.Seek(-(length + footerSize), SeekOrigin.End);
            byte[] art = new byte[(int)length];
            file.ReadExactly(art);
            byte[] expectedHash = footer.AsSpan(32, 32).ToArray();
            if (!CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(art), expectedHash))
                return null;
            return art;
        }
        catch { return null; }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (background != null)
        {
            g.DrawImage(background, ClientRectangle);
            return;
        }
        // Safe fallback for users who copied only an EXE out of the release ZIP.
        using (var gradient = new LinearGradientBrush(ClientRectangle,
                   Color.FromArgb(12, 12, 28), Color.FromArgb(43, 15, 47), 45f))
            g.FillRectangle(gradient, ClientRectangle);
        using var logo = TokyoBranding.LoadLogo();
        g.DrawImage(logo, 330, 51, 163, 163);
        using var titleFont = new Font("Segoe UI Black", 50, FontStyle.Bold, GraphicsUnit.Pixel);
        using var primary = new SolidBrush(Color.FromArgb(249, 249, 251));
        using var accent = new SolidBrush(Color.FromArgb(248, 70, 147));
        g.DrawString("TOKYO", titleFont, primary, 517, 61);
        g.DrawString("WORLD", titleFont, accent, 523, 119);
        using var side = new SolidBrush(Color.FromArgb(14, 13, 29));
        g.FillRectangle(side, 0, 34, 185, Height);
        using var card = new SolidBrush(Color.FromArgb(23, 21, 40));
        using var pen = new Pen(Color.FromArgb(112, 63, 113), 1.5f);
        foreach (var r in new[] { new Rectangle(207, 211, 447, 305),
                new Rectangle(670, 211, 586, 305) })
        {
            g.FillRectangle(card, r);
            g.DrawRectangle(pen, r);
        }
        using var small = new Font("Segoe UI", 15, FontStyle.Bold);
        g.DrawString("KONTO", small, primary, 245, 225);
        g.DrawString("USTAWIENIA GRY", small, primary, 710, 225);
        g.DrawString("PAMIĘĆ RAM", small, primary, 719, 279);
        g.DrawString("TRYB WYDAJNOŚCI", small, primary, 719, 322);
        g.DrawString("SHADERY", small, primary, 719, 371);
        g.DrawString("DODATKI GRAFICZNE", small, primary, 719, 417);
        g.DrawString("AKTUALIZACJE", small, primary, 719, 466);
        g.DrawString("PROFIL PACZKI", small, primary, 245, 425);
        g.DrawString("Tokyo World • Forge 1.20.1", small, primary, 245, 466);
        using var playBrush = new LinearGradientBrush(new Rectangle(391, 536, 638, 87),
            Color.FromArgb(230, 43, 107), Color.FromArgb(122, 16, 62), 0f);
        g.FillRectangle(playBrush, 391, 536, 638, 87);
        using var huge = new Font("Segoe UI Black", 30, FontStyle.Bold);
        g.DrawString("▶  GRAJ", huge, primary, 564, 553);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) background?.Dispose();
        base.Dispose(disposing);
    }
}

internal sealed class ConceptNavItem : Control
{
    private string caption = "";
    private string symbol = "";
    private bool selected;
    private bool hovering;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Caption { get => caption; set { caption = value; Invalidate(); } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Symbol { get => symbol; set { symbol = value; Invalidate(); } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected { get => selected; set { selected = value; Invalidate(); } }

    public ConceptNavItem()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e) { hovering = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hovering = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (selected || hovering)
        {
            using var brush = new LinearGradientBrush(ClientRectangle,
                selected ? Color.FromArgb(176, 145, 21, 80) : Color.FromArgb(80, 98, 26, 68),
                Color.FromArgb(20, 29, 19, 49), 0f);
            using var p = new Pen(selected ? Color.FromArgb(252, 67, 139) : Color.FromArgb(103, 71, 102), 1.5f);
            using var path = new GraphicsPath();
            path.AddArc(1, 1, 16, 16, 180, 90);
            path.AddArc(Width - 17, 1, 16, 16, 270, 90);
            path.AddArc(Width - 17, Height - 17, 16, 16, 0, 90);
            path.AddArc(1, Height - 17, 16, 16, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
            g.DrawPath(p, path);
        }
        float scale = Math.Min(1f, Width / 163f);
        using var iconFont = new Font("Segoe UI Symbol", 18 * scale, FontStyle.Regular);
        using var textFont = new Font("Segoe UI Semibold", 11 * scale, FontStyle.Bold);
        using var white = new SolidBrush(selected ? Color.White : Color.FromArgb(209, 203, 219));
        g.DrawString(symbol, iconFont, white, 11 * scale, 8 * scale);
        g.DrawString(caption, textFont, white, 47 * scale, 12 * scale);
    }
}

internal sealed class ConceptDot : Control
{
    private bool selected;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected { get => selected; set { selected = value; Invalidate(); } }

    public ConceptDot()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var bg = new SolidBrush(Color.FromArgb(25, 21, 39));
        using var border = new Pen(selected ? Color.FromArgb(254, 66, 142) :
            Color.FromArgb(155, 145, 166), 2f);
        g.FillEllipse(bg, 2, 2, Width - 5, Height - 5);
        g.DrawEllipse(border, 2, 2, Width - 5, Height - 5);
        if (selected)
        {
            using var accent = new SolidBrush(Color.FromArgb(251, 74, 149));
            g.FillEllipse(accent, 8, 8, Width - 17, Height - 17);
        }
    }
}

internal sealed class ConceptClickArea : Control
{
    private bool hovering;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HoverEnabled { get; set; }

    public ConceptClickArea()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
    }
    protected override void OnMouseEnter(EventArgs e) { hovering = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hovering = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        if (!HoverEnabled || !hovering) return;
        using var brush = new SolidBrush(Color.FromArgb(28, 255, 174, 214));
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }
}

internal sealed class ConceptValueButton : Control
{
    private string caption = "";
    private bool hovered;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Caption { get => caption; set { caption = value; Invalidate(); } }
    public ConceptValueButton()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
    }
    protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(hovered ? Color.FromArgb(51, 39, 71) : Color.FromArgb(27, 25, 44));
        using var pen = new Pen(hovered ? Color.FromArgb(236, 69, 141) : Color.FromArgb(99, 88, 121), 1f);
        var rect = new Rectangle(1, 1, Math.Max(2, Width - 3), Math.Max(2, Height - 3));
        using var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, 9, 9, 180, 90);
        path.AddArc(rect.Right - 9, rect.Top, 9, 9, 270, 90);
        path.AddArc(rect.Right - 9, rect.Bottom - 9, 9, 9, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - 9, 9, 9, 90, 90);
        path.CloseFigure();
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);
        using var font = new Font("Segoe UI", 10, FontStyle.Regular);
        using var ink = new SolidBrush(Color.FromArgb(238, 232, 247));
        using var hint = new SolidBrush(Color.FromArgb(196, 171, 214));
        e.Graphics.DrawString(caption, font, ink, 12, 6);
        e.Graphics.DrawString("⌄", font, hint, Width - 27, 5);
    }
}

internal sealed class ConceptSwitch : Control
{
    private bool isOn;
    private bool hovering;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsOn { get => isOn; set { isOn = value; Invalidate(); } }
    public event EventHandler? Changed;

    public ConceptSwitch()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
    }
    protected override void OnMouseEnter(EventArgs e) { hovering = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hovering = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnClick(EventArgs e)
    {
        IsOn = !IsOn;
        Changed?.Invoke(this, EventArgs.Empty);
        base.OnClick(e);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var track = new SolidBrush(IsOn ? Color.FromArgb(244, 52, 125) : Color.FromArgb(79, 73, 101));
        using var knob = new SolidBrush(Color.White);
        e.Graphics.FillEllipse(track, 1, 1, Width - 2, Height - 2);
        e.Graphics.FillRectangle(track, Height / 2, 1, Width - Height, Height - 2);
        int left = IsOn ? Width - Height + 2 : 2;
        e.Graphics.FillEllipse(knob, left, 3, Height - 6, Height - 6);
        if (hovering)
        {
            using var outline = new Pen(Color.FromArgb(186, 244, 156, 206));
            e.Graphics.DrawEllipse(outline, 1, 1, Width - 2, Height - 2);
        }
    }
}

internal sealed class ConceptRamSlider : Control
{
    private int value = 10, minimum = 2, maximum = 16;
    private bool dragging;
    public event EventHandler? ValueChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Minimum { get => minimum; set { minimum = value; Value = this.value; Invalidate(); } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Maximum { get => maximum; set { maximum = value; Value = this.value; Invalidate(); } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => value;
        set
        {
            int next = Math.Clamp(value, minimum, maximum);
            if (this.value == next) return;
            this.value = next;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public ConceptRamSlider()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
    }
    private void SetFromX(int x)
    {
        double t = Math.Clamp((x - 15d) / Math.Max(1, Width - 30), 0, 1);
        Value = (int)Math.Round(minimum + t * (maximum - minimum));
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) { dragging = true; Capture = true; SetFromX(e.X); }
        base.OnMouseDown(e);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (dragging) SetFromX(e.X);
        base.OnMouseMove(e);
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        dragging = false;
        Capture = false;
        base.OnMouseUp(e);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int x0 = 14, x1 = Width - 15, y = Height / 2;
        int fill = (int)(x0 + (x1 - x0) * (value - minimum) / (double)Math.Max(1, maximum - minimum));
        using var track = new Pen(Color.FromArgb(71, 66, 90), 6) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var active = new Pen(Color.FromArgb(249, 71, 138), 6) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        e.Graphics.DrawLine(track, x0, y, x1, y);
        e.Graphics.DrawLine(active, x0, y, fill, y);
        using var glow = new SolidBrush(Color.FromArgb(83, 251, 68, 150));
        using var knob = new SolidBrush(Color.FromArgb(255, 232, 246));
        e.Graphics.FillEllipse(glow, fill - 15, y - 15, 30, 30);
        e.Graphics.FillEllipse(knob, fill - 9, y - 9, 18, 18);
    }
}
