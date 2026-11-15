using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace TokyoWorldLauncher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new LauncherWindow());
    }
}

internal sealed class LauncherWindow : Form
{
    private readonly LauncherSettings settings = LauncherSettings.Load();
    private readonly MinecraftEngine engine = new();

    private readonly TextBox nickname = new();
    private readonly RadioButton premium = new();
    private readonly RadioButton offline = new();
    private readonly TrackBar ramSlider = new();
    private readonly Label ramValue = new();
    private readonly Label status = new();
    private readonly ProgressBar progress = new();
    private readonly Button play = new();
    private readonly Label gear = new();
    private readonly List<string> logs = new();
    private bool busy;
    private bool changingMode;

    private static readonly Color Bg = Color.FromArgb(11, 13, 22);
    private static readonly Color Surface = Color.FromArgb(28, 30, 43);
    private static readonly Color Ink = Color.FromArgb(247, 246, 250);
    private static readonly Color Gray = Color.FromArgb(159, 164, 180);
    private static readonly Color Red = Color.FromArgb(231, 49, 74);
    private static readonly Color Cyan = Color.FromArgb(72, 227, 222);

    public LauncherWindow()
    {
        Text = "TOKYO WORLD";
        ClientSize = new Size(900, 602);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = Bg;
        ForeColor = Ink;
        Font = new Font("Segoe UI", 10);
        Icon = SystemIcons.Application;

        var hero = new TokyoArtPanel { Location = new Point(0, 0), Size = new Size(900, 310) };
        Controls.Add(hero);
        gear.Text = "⚙";
        gear.Font = new Font("Segoe UI Symbol", 22, FontStyle.Regular);
        gear.TextAlign = ContentAlignment.MiddleCenter;
        gear.ForeColor = Ink;
        gear.BackColor = Color.FromArgb(36, 38, 51);
        gear.SetBounds(834, 17, 48, 44);
        gear.Cursor = Cursors.Hand;
        gear.Click += (_, _) => OpenSettings();
        Controls.Add(gear);
        gear.BringToFront();

        AddLabel("NICK MINECRAFT", 46, 329, 320, 23, 10, true, Gray);
        nickname.SetBounds(46, 359, 353, 42);
        nickname.BackColor = Surface;
        nickname.ForeColor = Ink;
        nickname.Font = new Font("Segoe UI Semibold", 15);
        nickname.BorderStyle = BorderStyle.FixedSingle;
        nickname.MaxLength = 16;
        nickname.TextChanged += (_, _) =>
        {
            if (!changingMode && offline.Checked)
                settings.OfflineNickname = nickname.Text;
        };
        Controls.Add(nickname);

        AddLabel("RAM", 452, 329, 200, 23, 10, true, Gray);
        ramSlider.SetBounds(444, 360, 314, 42);
        ramSlider.Minimum = 2;
        ramSlider.Maximum = 24;
        ramSlider.TickFrequency = 2;
        ramSlider.LargeChange = 2;
        ramSlider.SmallChange = 1;
        ramSlider.AutoSize = false;
        ramSlider.Height = 45;
        ramSlider.Value = Math.Clamp((int)Math.Round(settings.RamMb / 1024.0), 2, 24);
        ramSlider.BackColor = Bg;
        ramSlider.TickStyle = TickStyle.BottomRight;
        ramValue.SetBounds(762, 356, 105, 39);
        ramValue.TextAlign = ContentAlignment.MiddleRight;
        ramValue.ForeColor = Cyan;
        ramValue.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        ramValue.Text = $"{ramSlider.Value} GB";
        ramSlider.ValueChanged += (_, _) =>
        {
            ramValue.Text = $"{ramSlider.Value} GB";
            settings.RamMb = ramSlider.Value * 1024;
        };
        Controls.Add(ramSlider);
        Controls.Add(ramValue);

        premium.Text = "PREMIUM / MICROSOFT";
        premium.SetBounds(46, 409, 230, 28);
        premium.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        premium.ForeColor = Gray;
        premium.BackColor = Bg;
        offline.Text = "OFFLINE / NICK";
        offline.SetBounds(288, 409, 184, 28);
        offline.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        offline.ForeColor = Gray;
        offline.BackColor = Bg;
        premium.CheckedChanged += (_, _) => UpdateMode();
        offline.CheckedChanged += (_, _) => UpdateMode();
        Controls.Add(premium);
        Controls.Add(offline);

        play.Text = "GRAJ";
        play.SetBounds(46, 453, 808, 66);
        play.FlatStyle = FlatStyle.Flat;
        play.FlatAppearance.BorderSize = 0;
        play.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 76, 104);
        play.BackColor = Red;
        play.ForeColor = Color.White;
        play.Font = new Font("Segoe UI Black", 23, FontStyle.Bold);
        play.Cursor = Cursors.Hand;
        play.Click += async (_, _) => await StartGame();
        Controls.Add(play);

        progress.SetBounds(46, 542, 808, 8);
        progress.Style = ProgressBarStyle.Blocks;
        Controls.Add(progress);
        status.SetBounds(46, 559, 807, 28);
        status.ForeColor = Gray;
        status.Text = "Gotowy do gry";
        status.Font = new Font("Segoe UI", 9);
        status.TextAlign = ContentAlignment.MiddleCenter;
        status.Cursor = Cursors.Hand;
        status.Click += (_, _) => ShowLogs();
        Controls.Add(status);

        premium.Checked = settings.UseMicrosoft;
        offline.Checked = !settings.UseMicrosoft;
        UpdateMode();

        Shown += (_, _) =>
        {
            if (!settings.FirstRunCompleted)
                OpenSettings(firstRun: true);
        };
        FormClosing += (_, _) => SaveState();
    }

    private void AddLabel(string value, int x, int y, int w, int h, int size, bool bold, Color color)
    {
        Controls.Add(new Label
        {
            Text = value, Left = x, Top = y, Width = w, Height = h,
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = color, BackColor = Bg
        });
    }

    private void UpdateMode()
    {
        changingMode = true;
        bool isPremium = premium.Checked;
        nickname.ReadOnly = isPremium;
        nickname.Text = isPremium
            ? (string.IsNullOrWhiteSpace(settings.PremiumNickname) ? "Konto Microsoft" : settings.PremiumNickname)
            : settings.OfflineNickname;
        nickname.BackColor = isPremium ? Color.FromArgb(21, 24, 36) : Surface;
        nickname.ForeColor = isPremium && string.IsNullOrWhiteSpace(settings.PremiumNickname) ? Gray : Ink;
        premium.ForeColor = isPremium ? Cyan : Gray;
        offline.ForeColor = isPremium ? Gray : Cyan;
        settings.UseMicrosoft = isPremium;
        changingMode = false;
    }

    private void SaveState()
    {
        settings.RamMb = ramSlider.Value * 1024;
        settings.UseMicrosoft = premium.Checked;
        if (offline.Checked) settings.OfflineNickname = nickname.Text.Trim();
        settings.Save();
    }

    private string GameRoot()
    {
        string value = settings.InstallDirectory;
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Wybierz folder instalacji w ustawieniach (⚙).");
        string root = Path.GetFullPath(Environment.ExpandEnvironmentVariables(value));
        if (root == Path.GetPathRoot(root) ||
            root.Equals(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Nie można instalować gry bezpośrednio na dysku lub w folderze Windows.");
        return root;
    }

    private IProgress<string> Reporter() => new Progress<string>(SetStatus);

    private void SetStatus(string value)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(() => SetStatus(value)); return; }
        string line = $"[{DateTime.Now:HH:mm:ss}] {value}";
        logs.Add(line);
        if (logs.Count > 1200) logs.RemoveRange(0, 500);
        status.Text = value.Length > 116 ? value[..113] + "..." : value;
    }

    private void ShowLogs()
    {
        using var dialog = new Form
        {
            Text = "Tokyo World — dziennik",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(800, 460),
            BackColor = Bg
        };
        var contents = new TextBox
        {
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both,
            WordWrap = false, Dock = DockStyle.Fill,
            BackColor = Surface, ForeColor = Ink,
            Font = new Font("Consolas", 9),
            Text = string.Join(Environment.NewLine, logs)
        };
        dialog.Controls.Add(contents);
        dialog.ShowDialog(this);
    }

    private void OpenSettings(bool firstRun = false)
    {
        if (busy) return;
        using var dialog = new SettingsWindow(settings, firstRun);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            settings.FirstRunCompleted = true;
            settings.Save();
            SetStatus("Ustawienia zapisane.");
        }
    }

    private async Task StartGame()
    {
        if (busy) return;
        if (!settings.FirstRunCompleted)
        {
            OpenSettings(firstRun: true);
            if (!settings.FirstRunCompleted) return;
        }
        if (offline.Checked && !System.Text.RegularExpressions.Regex.IsMatch(
                nickname.Text.Trim(), @"\A[A-Za-z0-9_]{3,16}\z"))
        {
            MessageBox.Show(this, "Nick może mieć od 3 do 16 znaków: A-Z, cyfry lub _.",
                "Tokyo World", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        busy = true;
        play.Enabled = false;
        gear.Enabled = false;
        progress.Style = ProgressBarStyle.Marquee;
        try
        {
            SaveState();
            var reporter = Reporter();

            // Log in FIRST so the selected premium nickname is visible on the main screen.
            if (premium.Checked)
            {
                SetStatus("Otwieram bezpieczne logowanie Microsoft...");
                string user = await engine.LoginMicrosoftAsync();
                settings.PremiumNickname = user;
                nickname.Text = user;
                nickname.ForeColor = Ink;
                settings.Save();
                SetStatus("Zalogowano Microsoft: " + user);
            }

            string root = GameRoot();
            Directory.CreateDirectory(root);
            SetStatus("Sprawdzam aktualizacje Tokyo World...");
            var sync = new GitHubSynchronizer(settings.GetToken(), settings.Repository);
            await sync.SynchronizeAsync(root, reporter);
            PostInstallTweaks.Apply(root, reporter);

            var modpack = new ModpackInstaller();
            var report = await modpack.InstallAsync(root, settings.InstallAllModules, reporter);
            SetStatus($"Mody: +{report.Installed}, pominięto {report.SkippedOptional}, brakujących bazowych {report.MissingBase}.");
            if (report.MissingBase > 0)
                throw new InvalidOperationException(
                    "Brakuje części oryginalnych modów Tokyo World. Aby uniknąć crasha, wymagany jest import bazowej paczki ZIP. Szczegóły w dzienniku i dokumentacji.");

            await engine.PrepareGameAsync(root, reporter);
            var process = await engine.PlayAsync(root, settings.RamMb,
                premium.Checked, settings.OfflineNickname.Trim(), reporter);
            SetStatus($"Uruchomiono Tokyo World! PID {process.Id}.");
        }
        catch (Exception ex)
        {
            SetStatus("BŁĄD: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Tokyo World — uruchomienie nieudane",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            progress.Style = ProgressBarStyle.Blocks;
            play.Enabled = true;
            gear.Enabled = true;
            busy = false;
        }
    }
}

internal sealed class SettingsWindow : Form
{
    private readonly LauncherSettings settings;
    private readonly TextBox folder = new();
    private readonly TextBox repository = new();
    private readonly TextBox token = new();
    private readonly CheckBox allMods = new();
    private readonly Button save = new();

    public SettingsWindow(LauncherSettings data, bool firstRun)
    {
        settings = data;
        Text = firstRun ? "Tokyo World — pierwsza konfiguracja" : "Tokyo World — ustawienia";
        ClientSize = new Size(650, 406);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(15, 17, 28);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);

        Label("Folder instalacji", 25, 24, 420);
        Field(folder, 25, 51, 472, data.InstallDirectory);
        var browse = ActionButton("...", 510, 51, 108);
        browse.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Wybierz folder Tokyo World",
                ShowNewFolderButton = true
            };
            if (dlg.ShowDialog(this) == DialogResult.OK) folder.Text = dlg.SelectedPath;
        };

        Label("Repozytorium plików paczki (właściciel/nazwa)", 25, 110, 590);
        Field(repository, 25, 138, 593, data.Repository);
        Label("Publiczne repo dystrybucyjne nie wymaga tokenu u znajomych.", 25, 175, 610, color: Color.FromArgb(155, 165, 182));

        Label("Token GitHub — tylko gdy repo jest prywatne", 25, 208, 595);
        Field(token, 25, 236, 593, data.GetToken());
        token.UseSystemPasswordChar = true;
        Label("Nie umieszczaj własnego tokenu w EXE ani nie przesyłaj go znajomym.", 25, 272, 610, color: Color.FromArgb(155, 165, 182));

        allMods.Text = "Pełna paczka (MEGA + WAVE)";
        allMods.SetBounds(25, 303, 590, 26);
        allMods.ForeColor = Color.White;
        allMods.Checked = data.InstallAllModules;
        Controls.Add(allMods);

        save = ActionButton("ZAPISZ", 25, 348, 593);
        save.BackColor = Color.FromArgb(224, 48, 74);
        save.Click += (_, _) =>
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(repository.Text.Trim(),
                @"\A[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+\z"))
            {
                MessageBox.Show("Podaj repozytorium w formacie użytkownik/nazwa.");
                return;
            }
            if (folder.Text.Trim().Length == 0)
            {
                MessageBox.Show("Wybierz folder instalacji.");
                return;
            }
            data.InstallDirectory = folder.Text.Trim();
            data.Repository = repository.Text.Trim();
            data.SetToken(token.Text);
            data.InstallAllModules = allMods.Checked;
            DialogResult = DialogResult.OK;
            Close();
        };
    }

    private void Label(string text, int x, int y, int width, Color? color = null)
    {
        Controls.Add(new Label
        {
            Text = text, Left = x, Top = y, Width = width, Height = 26,
            ForeColor = color ?? Color.FromArgb(238, 239, 244)
        });
    }
    private void Field(TextBox input, int x, int y, int width, string value)
    {
        input.SetBounds(x, y, width, 33);
        input.Text = value;
        input.ForeColor = Color.White;
        input.BackColor = Color.FromArgb(33, 36, 52);
        input.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(input);
    }
    private Button ActionButton(string text, int x, int y, int width)
    {
        var b = new Button
        {
            Text = text, Left = x, Top = y, Width = width, Height = 35,
            ForeColor = Color.White, BackColor = Color.FromArgb(41, 44, 60),
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        Controls.Add(b);
        return b;
    }
}
