using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace TokyoWorldLauncher;

/// <summary>Find/install individual shaderpacks; Oculus is enabled only by user consent.</summary>
internal sealed class TokyoShaderBrowser : UserControl
{
    private readonly string root;
    private readonly LauncherSettings settings;
    private readonly TokyoModrinthClient catalog = new();
    private readonly TextBox query = CrimsonTheme.SearchBox("Wpisz nazwę shaderów, np. Complementary, BSL, Bliss...");
    private readonly ListBox results = new();
    private readonly ListBox installed = new();
    private readonly Label notice = CrimsonTheme.Label("Wybierz shader, aby zainstalować konkretną wersję.", 9);
    private readonly Button install = CrimsonTheme.Button("Zainstaluj wybrane", 190, 37, true);
    private readonly Button remove = CrimsonTheme.Button("Usuń wybrane", 155);
    private readonly Button search = CrimsonTheme.Button("Szukaj", 110);
    private readonly CheckBox oculus = new();
    private bool busy;

    private sealed record PackEntry(string Filename, ManagedMod? Managed)
    {
        public override string ToString() => Filename +
            (Managed == null ? "  •  własny" : "  •  z Modrinth");
    }

    public TokyoShaderBrowser(string gameRoot, LauncherSettings launcherSettings)
    {
        root = gameRoot;
        settings = launcherSettings;
        // WinForms applies Anchor offsets relative to the size at creation.
        // Use the actual page size BEFORE positioning anchored controls.
        Size = new Size(1067, 505);
        Dock = DockStyle.Fill;
        BackColor = CrimsonTheme.Background;

        var title = CrimsonTheme.Label("SHADERY  /  WYBIERZ I ZAINSTALUJ", 19, true);
        title.SetBounds(22, 11, 710, 38);
        title.AutoSize = false;
        Controls.Add(title);
        query.SetBounds(22, 55, 478, 33);
        query.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SearchAsync(); } };
        Controls.Add(query);
        search.SetBounds(507, 54, 112, 36);
        search.Click += async (_, _) => await SearchAsync();
        Controls.Add(search);

        oculus.Text = "Włącz obsługę shaderów (Oculus + Embeddium)";
        oculus.SetBounds(643, 55, 373, 33);
        oculus.Checked = settings.EnableShaders;
        oculus.ForeColor = CrimsonTheme.Foreground;
        oculus.Font = new Font("Segoe UI", 10);
        oculus.CheckedChanged += (_, _) =>
        {
            settings.EnableShaders = oculus.Checked;
            settings.Save();
            notice.Text = oculus.Checked
                ? "Oculus zostanie doinstalowany przez launcher przy kliknięciu GRAJ."
                : "Shadery wyłączone w profilu; już pobrane pliki pozostają w folderze.";
        };
        Controls.Add(oculus);

        var leftLabel = CrimsonTheme.Label("KATALOG MODRINTH", 10, true);
        leftLabel.SetBounds(22, 105, 290, 25);
        leftLabel.AutoSize = false;
        Controls.Add(leftLabel);
        var rightLabel = CrimsonTheme.Label("POBRANE SHADERY", 10, true);
        rightLabel.SetBounds(533, 105, 290, 25);
        rightLabel.AutoSize = false;
        Controls.Add(rightLabel);

        StyleList(results);
        results.SetBounds(22, 130, 479, 266);
        results.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
        results.SelectedIndexChanged += (_, _) => install.Enabled = !busy && results.SelectedItem is CatalogItem;
        Controls.Add(results);
        StyleList(installed);
        installed.SetBounds(535, 130, 480, 266);
        installed.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        installed.SelectedIndexChanged += (_, _) => remove.Enabled = installed.SelectedItem is PackEntry { Managed: not null };
        Controls.Add(installed);

        install.SetBounds(22, 407, 192, 40);
        install.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        install.Click += async (_, _) => await InstallSelectedAsync();
        install.Enabled = false;
        Controls.Add(install);
        remove.SetBounds(533, 407, 156, 40);
        remove.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        remove.Click += (_, _) => RemoveSelected();
        remove.Enabled = false;
        Controls.Add(remove);
        var folder = CrimsonTheme.Button("Folder shaderów", 162);
        folder.SetBounds(697, 407, 166, 40);
        folder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        folder.Click += (_, _) =>
        {
            var dir = Path.Combine(root, "shaderpacks");
            Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
        };
        Controls.Add(folder);
        notice.AutoSize = false;
        notice.SetBounds(22, 459, 986, 39);
        notice.Anchor = AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;
        Controls.Add(notice);

        RefreshInstalled();
        _ = SearchAsync();
    }

    private static void StyleList(ListBox control)
    {
        control.Font = new Font("Segoe UI", 10);
        control.BackColor = CrimsonTheme.Surface2;
        control.ForeColor = CrimsonTheme.Foreground;
        control.BorderStyle = BorderStyle.FixedSingle;
        control.IntegralHeight = false;
        control.HorizontalScrollbar = true;
        control.ItemHeight = 29;
    }

    private void RefreshInstalled()
    {
        string dir = Path.Combine(root, "shaderpacks");
        Directory.CreateDirectory(dir);
        var managed = TokyoModrinthClient.LoadRegistry(root)
            .Where(x => x.Type == "shader").ToList();
        installed.BeginUpdate();
        installed.Items.Clear();
        foreach (string file in Directory.EnumerateFiles(dir, "*.zip")
            .OrderBy(x => Path.GetFileName(x), StringComparer.OrdinalIgnoreCase))
        {
            string filename = Path.GetFileName(file);
            installed.Items.Add(new PackEntry(filename,
                managed.FirstOrDefault(x => x.FileName.Equals(filename, StringComparison.OrdinalIgnoreCase))));
        }
        installed.EndUpdate();
        remove.Enabled = false;
    }

    private async Task SearchAsync()
    {
        if (busy || IsDisposed) return;
        busy = true;
        search.Enabled = false;
        install.Enabled = false;
        try
        {
            notice.Text = "Wyszukuję shaderpacki dla Minecraft 1.20.1...";
            var found = await catalog.SearchAsync(query.Text, true);
            if (IsDisposed) return;
            results.BeginUpdate();
            results.Items.Clear();
            foreach (var item in found) results.Items.Add(item);
            results.EndUpdate();
            notice.Text = $"Znaleziono {found.Count} shaderpacków. Po instalacji wybierz pack w ustawieniach Oculus w grze.";
        }
        catch (Exception e) { if (!IsDisposed) notice.Text = "Modrinth: " + e.Message; }
        finally
        {
            busy = false;
            if (!IsDisposed) search.Enabled = true;
        }
    }

    private async Task InstallSelectedAsync()
    {
        if (busy || results.SelectedItem is not CatalogItem chosen) return;
        busy = true;
        install.Enabled = false;
        search.Enabled = false;
        try
        {
            var files = await catalog.InstallAsync(root, chosen, true,
                new Progress<string>(s => { if (!IsDisposed) notice.Text = s; }));
            RefreshInstalled();
            if (!settings.EnableShaders)
            {
                var response = MessageBox.Show(this,
                    "Shader został pobrany. Włączyć również Oculus, aby można było wybrać go w Minecraft?\n" +
                    "Oculus instaluje się przy kolejnym uruchomieniu gry.",
                    "Tokyo World — shader", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (response == DialogResult.Yes)
                {
                    oculus.Checked = true;
                    settings.Save();
                }
            }
            notice.Text = files.Count > 0
                ? "Zainstalowano " + chosen.Title + ". Aktywacja: Minecraft → Opcje grafiki → Shader Packs."
                : "Wybrany shaderpack jest już obecny w grze.";
        }
        catch (Exception e)
        {
            notice.Text = "Błąd pobierania: " + e.Message;
            MessageBox.Show(this, e.Message, "Tokyo World — shadery",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            busy = false;
            if (!IsDisposed)
            {
                search.Enabled = true;
                install.Enabled = results.SelectedItem is CatalogItem;
            }
        }
    }

    private void RemoveSelected()
    {
        if (installed.SelectedItem is not PackEntry { Managed: not null } selected || busy) return;
        if (MessageBox.Show(this, "Przenieść \"" + selected.Filename +
            "\" do kopii zapasowej? Możesz ponownie go pobrać w każdej chwili.",
            "Tokyo World — shadery", MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            TokyoModrinthClient.RemoveManaged(root, selected.Managed);
            RefreshInstalled();
            notice.Text = "Shader przeniesiono do .tokyo/backups/user-addons.";
        }
        catch (Exception e) { MessageBox.Show(this, e.Message, "Usuwanie shaderów"); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) catalog.Dispose();
        base.Dispose(disposing);
    }
}
