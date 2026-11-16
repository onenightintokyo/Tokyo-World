using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace TokyoWorldLauncher;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length == 2 && args[0] == "--render-preview")
        {
            // Show an actual native Windows window: WinForms does not draw child
            // controls in DrawToBitmap until they have live window handles.
            Environment.SetEnvironmentVariable("TOKYO_UI_PREVIEW", "1");
            using var window = new LauncherWindow();
            window.Show();
            Application.DoEvents();
            System.Threading.Thread.Sleep(250);
            Application.DoEvents();
            using var bitmap = new Bitmap(window.ClientSize.Width, window.ClientSize.Height);
            window.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            bitmap.Save(args[1], System.Drawing.Imaging.ImageFormat.Png);
            window.Close();
            return;
        }
        Application.Run(new LauncherWindow());
    }
}

internal sealed partial class LauncherWindow : Form
{
    private readonly LauncherSettings settings = LauncherSettings.Load();
    private readonly MinecraftEngine engine = new();

    private readonly TextBox nickname = new();
    private readonly RadioButton premium = new();
    private readonly RadioButton offline = new();
    private readonly TrackBar ramSlider = new();
    private readonly Label ramValue = new();
    private readonly Label status = new();
    private readonly TokyoProgressBar progress = new();
    private readonly CrimsonGradientButton play = new();
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
        Icon = TokyoBranding.LoadIcon();

        var hero = new TokyoArtPanel { Location = new Point(0, 0), Size = new Size(900, 310) };
        Controls.Add(hero);
        // Display the same TW crest used in the EXE and Minecraft taskbar.
        var brand = new PictureBox
        {
            Image = TokyoBranding.LoadLogo(),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Location = new Point(29, 66),
            Size = new Size(166, 166)
        };
        hero.Controls.Add(brand);
        brand.BringToFront();
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
        ramSlider.Maximum = 16;
        ramSlider.TickFrequency = 2;
        ramSlider.LargeChange = 2;
        ramSlider.SmallChange = 1;
        ramSlider.AutoSize = false;
        ramSlider.Height = 45;
        ramSlider.Value = Math.Clamp((int)Math.Round(settings.RamMb / 1024.0), 2, 16);
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

        progress.SetBounds(46, 542, 808, 24);
        progress.Click += (_, _) => ShowLogs();
        Controls.Add(progress);
        status.SetBounds(46, 559, 807, 28);
        status.ForeColor = Gray;
        status.Text = "Gotowy do gry";
        status.Visible = false; // The modern launcher shows only the visual progress bar.
        status.Font = new Font("Segoe UI", 9);
        status.TextAlign = ContentAlignment.MiddleCenter;
        status.Cursor = Cursors.Hand;
        status.Click += (_, _) => ShowLogs();
        Controls.Add(status);

        premium.Checked = settings.UseMicrosoft;
        offline.Checked = !settings.UseMicrosoft;
        UpdateMode();

        BuildConcept4Ui();

        Shown += async (_, _) =>
        {
            if (Environment.GetEnvironmentVariable("TOKYO_UI_PREVIEW") == "1")
                return;
            if (!settings.FirstRunCompleted)
                OpenSettings(firstRun: true);
            if (settings.FirstRunCompleted)
                await CheckLauncherUpdateAsync();
        };
        FormClosing += (_, _) => SaveState();
    }

    private async Task CheckLauncherUpdateAsync()
    {
        if (busy) return;
        busy = true;
        play.Enabled = false;
        gear.Enabled = false;
        try
        {
            progress.Progress = 5;
            bool restart = await LauncherAutoUpdater.TryUpdateAsync(settings, Reporter());
            if (restart)
            {
                Application.Exit();
                return;
            }
        }
        catch (Exception ex)
        {
            SetStatus("Sprawdzenie EXE: " + ex.Message);
        }
        finally
        {
            busy = false;
            play.Enabled = true;
            gear.Enabled = true;
            progress.Progress = 0;
        }
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
        nickname.BackColor = Color.FromArgb(26, 25, 43);
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
        // Quiet, percentage-only UI. The full diagnostic log is still available by
        // clicking the progress bar.
        if (value.StartsWith("Aktualizacja:", StringComparison.Ordinal))
            progress.Progress = Math.Min(28, progress.Progress + 1);
        else if (value.StartsWith("Dodatek:", StringComparison.Ordinal) ||
                 value.StartsWith("Baza modów:", StringComparison.Ordinal))
            progress.Progress = Math.Min(69, Math.Max(32, progress.Progress + 1));
        else if (value.StartsWith("Forge:", StringComparison.Ordinal) ||
                 value.StartsWith("Minecraft:", StringComparison.Ordinal))
            progress.Progress = Math.Min(95, Math.Max(72, progress.Progress + 1));
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
        progress.Progress = 2;
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
            progress.Progress = Math.Max(progress.Progress, 25);
            PostInstallTweaks.Apply(root, reporter);
            TokyoBranding.CopyLogoToGame(root, reporter);
            TokyoQuestBanners.Install(root, reporter);

            // Recover already-owned original mod files before querying Modrinth.
            // This is a one-time IMPORT, not an ongoing Prism dependency.
            int localRecovered = await Task.Run(() => BaseModRecovery.ImportFromLocalCopies(root, reporter));
            if (localRecovered > 0)
                SetStatus($"Przywrócono {localRecovered} oryginalnych modów z lokalnej kopii.");

            // If the owner still has the original exported pack ZIP, detect it
            // automatically. Every JAR is checked against the exact SHA-1 lockfile.
            int needed = await Task.Run(() => BaseModRecovery.CountMissing(root));
            string? originalZip = needed > 0 ? BaseModRecovery.FindOriginalZip() : null;
            if (originalZip != null)
            {
                SetStatus("Znaleziono oryginalny ZIP — uzupełniam pliki modów...");
                await Task.Run(() => BaseModRecovery.ImportFromZip(root, originalZip, reporter));
            }

            // Apply the lightweight policy before and after sync to prevent JAR reinstallation.
            TokyoOptimization.ApplyModPolicy(root, settings.LightweightMode, reporter);
            var modpack = new ModpackInstaller();
            var report = await modpack.InstallAsync(root, settings.InstallAllModules, settings.LightweightMode,
                settings.ExtraVisualMods, settings.EnableShaders, reporter);
            // v1.7: installed shaderpacks are user-selected in the SHADERY tab.
            // Oculus is installed only when enabled; never overwrite the chosen pack.
            progress.Progress = Math.Max(progress.Progress, 70);

            TokyoOptimization.ApplyModPolicy(root, settings.LightweightMode, reporter);
            TokyoOptimization.ApplyGraphics(root, settings.OptimizeGraphics, reporter);
            needed = await Task.Run(() => BaseModRecovery.CountMissing(root));
            SetStatus($"Mody: +{report.Installed}, pominięto {report.SkippedOptional}, brakuje {needed} bazowych.");

            if (needed > 0)
            {
                var decision = MessageBox.Show(this,
                    $"Brakuje {needed} oryginalnych modów, których nie można pobrać w dokładnej wersji z Modrinth.\n\n" +
                    "Launcher może automatycznie zaimportować je z oryginalnego ZIP Tokyo World i sprawdzić sumy plików.\n\n" +
                    "Czy chcesz wskazać archiwum Tokyo_World_Forge_1.20.1.zip?",
                    "Tokyo World — uzupełnij brakujące mody",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (decision == DialogResult.Yes)
                {
                    using var dialog = new OpenFileDialog
                    {
                        Filter = "Paczka modów ZIP (*.zip)|*.zip",
                        Title = "Wybierz oryginalną paczkę Tokyo World",
                        CheckFileExists = true
                    };
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        await Task.Run(() => BaseModRecovery.ImportFromZip(root, dialog.FileName, reporter));
                        needed = await Task.Run(() => BaseModRecovery.CountMissing(root));
                    }
                }
            }
            if (needed > 0)
                throw new InvalidOperationException(
                    $"Brakuje {needed} oryginalnych modów Tokyo World. Nie uruchamiam niekompletnej gry. " +
                    "Brakujące pliki: " + string.Join(", ", BaseModRecovery.MissingNames(root)) + ". " +
                    "Sprawdź folder .tokyo/missing-mods.txt lub przywróć oryginalne pliki.");

            SetStatus("Wszystkie 66 oryginalnych modów są zweryfikowane — przygotowuję Forge...");
            await engine.PrepareGameAsync(root, reporter);
            progress.Progress = 95;
            var process = await engine.PlayAsync(root, settings.RamMb,
                premium.Checked, settings.OfflineNickname.Trim(), reporter);
            progress.Progress = 100;
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
            if (progress.Progress != 100) progress.Progress = 0;
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
    private readonly CheckBox optimize = new();
    private readonly CheckBox lightweight = new();
    private readonly CheckBox extraVisuals = new();
    private readonly CheckBox shaders = new();
    private readonly Button save = new();

    public SettingsWindow(LauncherSettings data, bool firstRun)
    {
        settings = data;
        Text = firstRun ? "Tokyo World — pierwsza konfiguracja" : "Tokyo World — ustawienia";
        ClientSize = new Size(650, 638);
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
        Label("Dla znajomych: Tokyo-World-Client (PUBLICZNE) — bez tokenu.", 25, 175, 610, color: Color.FromArgb(155, 165, 182));

        Label("Token GitHub — tylko gdy repo jest prywatne", 25, 208, 595);
        Field(token, 25, 236, 593, data.GetToken());
        token.UseSystemPasswordChar = true;
        Label("Nie umieszczaj własnego tokenu w EXE ani nie przesyłaj go znajomym.", 25, 272, 610, color: Color.FromArgb(155, 165, 182));

        allMods.Text = "Pełna paczka (MEGA + WAVE)";
        allMods.SetBounds(25, 303, 590, 26);
        allMods.ForeColor = Color.White;
        allMods.Checked = data.InstallAllModules;
        Controls.Add(allMods);

        optimize.Text = "Optymalizacja grafiki (render 8 / symulacja 6 / 90 FPS)";
        optimize.SetBounds(25, 332, 590, 27);
        optimize.ForeColor = Color.White;
        optimize.Checked = data.OptimizeGraphics;
        Controls.Add(optimize);

        lightweight.Text = "Tryb lekki: priorytet dla płynności gry";
        lightweight.SetBounds(25, 365, 590, 27);
        lightweight.ForeColor = Color.White;
        lightweight.Checked = data.LightweightMode;
        Controls.Add(lightweight);
        Label("Every Compat usunięty z całej paczki (stary JAR trafia do kopii).",
            25, 394, 615, color: Color.FromArgb(237, 182, 109));

        extraVisuals.Text = "Dodatkowe modele i tekstury (opcjonalne)";
        extraVisuals.SetBounds(25, 425, 590, 27);
        extraVisuals.ForeColor = Color.White;
        extraVisuals.Checked = data.ExtraVisualMods;
        Controls.Add(extraVisuals);

        shaders.Text = "Oculus + Complementary Reimagined (domyślnie wyłączone)";
        shaders.SetBounds(25, 460, 590, 27);
        shaders.ForeColor = Color.White;
        shaders.Checked = data.EnableShaders;
        Controls.Add(shaders);

        var restore = ActionButton("PRZYWRÓĆ PRZED OPTYMALIZACJĄ", 25, 505, 593);
        restore.Click += (_, _) =>
        {
            if (MessageBox.Show(this,
                "Przywrócić poprzednie ustawienia grafiki options.txt? " +
                "Every Compat pozostanie wyłączony, aby nie spowalniać ładowania gry.",
                "Tokyo World — przywracanie",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                TokyoOptimization.RestoreAll(
                    Path.GetFullPath(Environment.ExpandEnvironmentVariables(folder.Text.Trim())),
                    new Progress<string>(message => MessageBox.Show(this, message, "Tokyo World")));
                optimize.Checked = false;
                lightweight.Checked = false;
                data.OptimizeGraphics = false;
                data.LightweightMode = false;
                data.Save();
                MessageBox.Show(this, "Przywrócono ustawienia grafiki. Every Compat pozostaje wyłączony.", "Tokyo World");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Nie udało się przywrócić", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        save = ActionButton("ZAPISZ", 25, 556, 593);
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
            data.OptimizeGraphics = optimize.Checked;
            data.LightweightMode = lightweight.Checked;
            data.ExtraVisualMods = extraVisuals.Checked;
            data.EnableShaders = shaders.Checked;
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
