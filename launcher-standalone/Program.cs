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

    private readonly TextBox folderBox = new();
    private readonly TextBox githubTokenBox = new();
    private readonly TextBox nicknameBox = new();
    private readonly RadioButton microsoftRadio = new();
    private readonly RadioButton offlineRadio = new();
    private readonly CheckBox allModsBox = new();
    private readonly NumericUpDown ram = new();
    private readonly TextBox log = new();
    private readonly ProgressBar progressBar = new();
    private readonly Label state = new();
    private readonly Button installButton = new();
    private readonly Button playButton = new();
    private bool busy;

    private static readonly Color Background = Color.FromArgb(12, 13, 21);
    private static readonly Color PanelColor = Color.FromArgb(25, 27, 39);
    private static readonly Color TextColor = Color.FromArgb(243, 242, 249);
    private static readonly Color Muted = Color.FromArgb(162, 163, 180);
    private static readonly Color Accent = Color.FromArgb(230, 57, 80);

    public LauncherWindow()
    {
        Text = "TOKYO WORLD | Niezależny Launcher";
        ClientSize = new Size(990, 720);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = Background;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 10);

        var side = new Panel { Left = 0, Top = 0, Width = 285, Height = 720, BackColor = PanelColor };
        Controls.Add(side);
        side.Controls.Add(new Panel { Left = 0, Top = 0, Width = 285, Height = 5, BackColor = Accent });
        AddText(side, "TOKYO", 22, 82, 250, 60, 33, true, TextColor);
        AddText(side, "WORLD", 22, 135, 250, 65, 33, true, Accent);
        AddText(side, "TECH // EXPERT // ADVENTURE", 23, 207, 250, 24, 9, false, Muted);
        AddText(side, "Minecraft 1.20.1", 23, 319, 255, 28, 14, true, TextColor);
        AddText(side, "Forge 47.4.28", 23, 352, 240, 28, 11, false, Muted);
        AddText(side, "Bez Prism i zewnętrznego launchera", 23, 633, 250, 44, 9, false, Muted);
        AddText(side, "TOKYO WORLD 1.0", 23, 592, 252, 28, 12, true, TextColor);

        AddText(this, "URUCHOM GRĘ", 314, 20, 625, 42, 22, true, TextColor);
        AddText(this, "Instalacja, aktualizacja i logowanie w jednym programie.", 315, 65, 630, 24, 10, false, Muted);

        AddText(this, "Folder instalacji gry", 315, 102, 430, 23, 10, false, Muted);
        SetupInput(folderBox, 315, 128, 518);
        folderBox.Text = settings.InstallDirectory;
        AddButton(this, "WYBIERZ", 842, 127, 116, 34, PanelColor, (_, _) => PickFolder());

        AddText(this, "Repozytorium GitHub (prywatne — token z dostępem Contents: Read)", 315, 176, 635, 24, 9, false, Muted);
        SetupInput(githubTokenBox, 315, 201, 643);
        githubTokenBox.UseSystemPasswordChar = true;
        githubTokenBox.Text = settings.GetToken();
        AddText(this, "Token szyfrowany przez Windows DPAPI. Nigdy nie umieszczaj go w pliku EXE.", 315, 241, 639, 21, 8, false, Muted);

        AddText(this, "LOGOWANIE", 315, 273, 290, 24, 10, true, TextColor);
        microsoftRadio.SetBounds(315, 306, 250, 28);
        microsoftRadio.Text = "Premium / Microsoft";
        microsoftRadio.Checked = settings.UseMicrosoft;
        microsoftRadio.ForeColor = TextColor;
        microsoftRadio.BackColor = Background;
        Controls.Add(microsoftRadio);
        offlineRadio.SetBounds(585, 306, 260, 28);
        offlineRadio.Text = "Offline / własny nick";
        offlineRadio.Checked = !settings.UseMicrosoft;
        offlineRadio.ForeColor = TextColor;
        offlineRadio.BackColor = Background;
        Controls.Add(offlineRadio);

        SetupInput(nicknameBox, 315, 341, 330);
        nicknameBox.Text = settings.OfflineNickname;
        nicknameBox.PlaceholderText = "Nick 3–16 znaków";
        AddButton(this, "ZALOGUJ MICROSOFT", 663, 340, 295, 35, PanelColor, async (_, _) =>
        {
            if (busy) return;
            await RunBusy(async () =>
            {
                WriteLog("Uruchamiam oficjalne logowanie Minecraft/Microsoft...");
                var user = await engine.ChangeMicrosoftAccountAsync();
                WriteLog("Zalogowano konto premium: " + user);
            });
        });

        allModsBox.SetBounds(315, 400, 435, 26);
        allModsBox.Text = "Pełny modpack: MEGA + WAVE + aktualizacje";
        allModsBox.ForeColor = TextColor;
        allModsBox.Checked = settings.InstallAllModules;
        Controls.Add(allModsBox);

        AddText(this, "RAM (MB)", 779, 399, 90, 25, 10, false, Muted);
        ram.SetBounds(866, 398, 91, 30);
        ram.Minimum = 2048;
        ram.Maximum = 32768;
        ram.Increment = 512;
        ram.Value = Math.Clamp(settings.RamMb, (int)ram.Minimum, (int)ram.Maximum);
        ram.BackColor = PanelColor;
        ram.ForeColor = TextColor;
        Controls.Add(ram);

        installButton.Text = "POBIERZ / AKTUALIZUJ";
        StyleButton(installButton, 315, 452, 265, 47, PanelColor);
        installButton.Click += async (_, _) =>
        {
            if (busy) return;
            await RunBusy(async () => await InstallEverything());
        };

        playButton.Text = "GRAJ W TOKYO WORLD";
        StyleButton(playButton, 595, 452, 363, 47, Accent);
        playButton.Click += async (_, _) =>
        {
            if (busy) return;
            await RunBusy(async () =>
            {
                var root = await InstallEverything();
                var session = await engine.PlayAsync(root, (int)ram.Value,
                    microsoftRadio.Checked, nicknameBox.Text.Trim(), CreateProgress());
                WriteLog("Wystartowała gra (PID " + session.Id + ").");
            });
        };

        AddButton(this, "IMPORTUJ STARĄ PACZKĘ ZIP", 315, 508, 272, 32, PanelColor, async (_, _) =>
        {
            if (busy) return;
            using var chooser = new OpenFileDialog { Filter = "Paczka ZIP (*.zip)|*.zip", Title = "Wybierz oryginalne Tokyo_World_Forge_1.20.1.zip" };
            if (chooser.ShowDialog(this) != DialogResult.OK) return;
            await RunBusy(async () =>
            {
                string root = SelectedRoot();
                await Task.Run(() => ModpackInstaller.ImportOriginalZip(chooser.FileName, root, CreateProgress()));
                WriteLog("Oryginalne pliki modów zaimportowano. Teraz kliknij GRAJ.");
            });
        });
        AddButton(this, "OTWÓRZ FOLDER", 602, 508, 174, 32, PanelColor, (_, _) =>
        {
            Directory.CreateDirectory(SelectedRoot());
            Process.Start(new ProcessStartInfo { FileName = SelectedRoot(), UseShellExecute = true });
        });
        AddButton(this, "DZIENNIKI", 790, 508, 168, 32, PanelColor, (_, _) =>
        {
            string path = Path.Combine(SelectedRoot(), ".tokyo");
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        });

        progressBar.SetBounds(315, 566, 643, 14);
        progressBar.Style = ProgressBarStyle.Blocks;
        Controls.Add(progressBar);
        state.SetBounds(316, 588, 630, 24);
        state.Text = "Gotowy do instalacji";
        state.ForeColor = Muted;
        Controls.Add(state);
        log.SetBounds(315, 621, 643, 78);
        log.Multiline = true;
        log.ReadOnly = true;
        log.ScrollBars = ScrollBars.Vertical;
        log.BackColor = PanelColor;
        log.ForeColor = TextColor;
        log.BorderStyle = BorderStyle.None;
        log.Font = new Font("Consolas", 8);
        Controls.Add(log);

        Shown += (_, _) =>
        {
            WriteLog("Witaj w niezależnym Tokyo World Launcher.");
            WriteLog("Wybierz katalog. Repozytorium prywatne wymaga tokenu GitHub (Contents: Read).");
        };
        FormClosing += (_, _) => SavePreferences();
    }

    private static void AddText(Control parent, string text, int x, int y, int w, int h, float size, bool bold, Color color)
    {
        parent.Controls.Add(new Label {
            Text = text, Left = x, Top = y, Width = w, Height = h,
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = color, BackColor = Color.Transparent
        });
    }

    private void SetupInput(TextBox input, int x, int y, int w)
    {
        input.SetBounds(x, y, w, 32);
        input.Font = new Font("Segoe UI", 10);
        input.BackColor = PanelColor;
        input.ForeColor = TextColor;
        input.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(input);
    }

    private void StyleButton(Button button, int x, int y, int w, int h, Color bg)
    {
        button.SetBounds(x, y, w, h);
        button.BackColor = bg;
        button.ForeColor = TextColor;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        Controls.Add(button);
    }

    private Button AddButton(Control parent, string text, int x, int y, int w, int h, Color bg, EventHandler click)
    {
        var button = new Button { Text = text };
        StyleButton(button, x, y, w, h, bg);
        button.Click += click;
        return button;
    }

    private void PickFolder()
    {
        using var choose = new FolderBrowserDialog
        {
            Description = "Wybierz folder instalacji Tokyo World (nie wybieraj C:\\ ani Windows).",
            ShowNewFolderButton = true,
            InitialDirectory = Directory.Exists(folderBox.Text) ? folderBox.Text : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };
        if (choose.ShowDialog(this) == DialogResult.OK) folderBox.Text = choose.SelectedPath;
    }

    private string SelectedRoot()
    {
        var root = Path.GetFullPath(Environment.ExpandEnvironmentVariables(folderBox.Text.Trim()));
        if (root == Path.GetPathRoot(root) ||
            root.Equals(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Wybierz osobny folder gry, a nie katalog główny dysku lub Windows.");
        return root;
    }

    private void SavePreferences()
    {
        settings.InstallDirectory = folderBox.Text;
        settings.OfflineNickname = nicknameBox.Text;
        settings.UseMicrosoft = microsoftRadio.Checked;
        settings.InstallAllModules = allModsBox.Checked;
        settings.RamMb = (int)ram.Value;
        settings.SetToken(githubTokenBox.Text);
        settings.Save();
    }

    private IProgress<string> CreateProgress() => new Progress<string>(WriteLog);

    private void WriteLog(string message)
    {
        if (InvokeRequired) { BeginInvoke(() => WriteLog(message)); return; }
        var line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message;
        log.AppendText(line + Environment.NewLine);
        if (log.TextLength > 30000) log.Text = log.Text[^18000..];
        state.Text = message.Length > 88 ? message[..88] + "..." : message;
    }

    private async Task<string> InstallEverything()
    {
        SavePreferences();
        string root = SelectedRoot();
        Directory.CreateDirectory(root);
        var progress = CreateProgress();
        var synchronizer = new GitHubSynchronizer(settings.GetToken());
        await synchronizer.SynchronizeAsync(root, progress);
        PostInstallTweaks.Apply(root, progress);

        var modInstaller = new ModpackInstaller();
        var report = await modInstaller.InstallAsync(root, allModsBox.Checked, progress);
        WriteLog($"Mody: dodano {report.Installed}; brakujące oryginalne: {report.MissingBase}; pominięte dodatki: {report.SkippedOptional}");
        if (report.MissingBase > 0)
            throw new InvalidOperationException($"{report.MissingBase} bazowych modów nie ma na Modrinth. Importuj oryginalny ZIP przyciskiem w launcherze, a następnie ponów instalację.");
        await engine.PrepareGameAsync(root, progress);
        return root;
    }

    private async Task RunBusy(Func<Task> operation)
    {
        busy = true;
        installButton.Enabled = playButton.Enabled = false;
        progressBar.Style = ProgressBarStyle.Marquee;
        try
        {
            await operation();
            WriteLog("Operacja zakończona.");
        }
        catch (Exception ex)
        {
            WriteLog("BŁĄD: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Tokyo World — błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            progressBar.Style = ProgressBarStyle.Blocks;
            installButton.Enabled = playButton.Enabled = true;
            busy = false;
        }
    }
}
