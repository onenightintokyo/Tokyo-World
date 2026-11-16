using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace TokyoWorldLauncher;

internal sealed class TokyoModBrowser : UserControl
{
    private readonly string root;
    private readonly TokyoModrinthClient catalog = new();
    private readonly TextBox query = CrimsonTheme.SearchBox("Szukaj modów na Modrinth...");
    private readonly ListBox results = new();
    private readonly ListBox installed = new();
    private readonly Label notice = CrimsonTheme.Label("Wyszukiwane są tylko mody Forge 1.20.1.", 9);
    private readonly Button install = CrimsonTheme.Button("Zainstaluj mod", 160, 38, true);
    private readonly Button remove = CrimsonTheme.Button("Usuń dodany mod", 176);
    private readonly Button search = CrimsonTheme.Button("Szukaj", 110);
    private bool busy;

    private sealed record InstalledEntry(string FullPath, ManagedMod? Managed)
    {
        public override string ToString() => Path.GetFileName(FullPath) +
            (Managed == null ? "  •  paczka / ręcznie" : "  •  dodany w launcherze");
    }

    public TokyoModBrowser(string gameRoot)
    {
        root = gameRoot;
        Dock = DockStyle.Fill;
        BackColor = CrimsonTheme.Background;
        var title = CrimsonTheme.Label("MODY  /  FORGE 1.20.1", 19, true);
        title.SetBounds(22, 12, 520, 40);
        title.AutoSize = false;
        Controls.Add(title);

        query.SetBounds(22, 60, 478, 34);
        query.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SearchAsync(); } };
        Controls.Add(query);
        search.SetBounds(507, 59, 112, 36);
        search.Click += async (_, _) => await SearchAsync();
        Controls.Add(search);

        var leftHeading = CrimsonTheme.Label("WYNIKI WYSZUKIWANIA", 10, true);
        leftHeading.SetBounds(22, 105, 330, 25);
        leftHeading.AutoSize = false;
        Controls.Add(leftHeading);
        var rightHeading = CrimsonTheme.Label("ZAINSTALOWANE W GRZE", 10, true);
        rightHeading.SetBounds(535, 105, 340, 25);
        rightHeading.AutoSize = false;
        Controls.Add(rightHeading);

        StyleList(results);
        results.SetBounds(22, 130, 480, 270);
        results.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
        results.SelectedIndexChanged += (_, _) => install.Enabled = !busy && results.SelectedItem is CatalogItem;
        Controls.Add(results);
        StyleList(installed);
        installed.SetBounds(535, 130, 480, 270);
        installed.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;
        installed.SelectedIndexChanged += (_, _) => remove.Enabled = !busy && installed.SelectedItem is InstalledEntry { Managed: not null };
        Controls.Add(installed);

        install.SetBounds(22, 411, 166, 40);
        install.Enabled = false;
        install.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        install.Click += async (_, _) => await InstallSelectedAsync();
        Controls.Add(install);
        remove.SetBounds(535, 411, 185, 40);
        remove.Enabled = false;
        remove.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        remove.Click += (_, _) => RemoveSelected();
        Controls.Add(remove);

        var folder = CrimsonTheme.Button("Otwórz mods", 138);
        folder.SetBounds(736, 411, 140, 40);
        folder.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        folder.Click += (_, _) =>
        {
            Directory.CreateDirectory(Path.Combine(root, "mods"));
            Process.Start(new ProcessStartInfo(Path.Combine(root, "mods")) { UseShellExecute = true });
        };
        Controls.Add(folder);
        notice.SetBounds(22, 456, 952, 25);
        notice.AutoSize = false;
        notice.ForeColor = CrimsonTheme.Muted;
        notice.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        Controls.Add(notice);
        RefreshInstalled();
        _ = SearchAsync();
    }

    private static void StyleList(ListBox list)
    {
        list.BackColor = CrimsonTheme.Surface2;
        list.ForeColor = CrimsonTheme.Foreground;
        list.Font = new Font("Segoe UI", 10);
        list.IntegralHeight = false;
        list.ItemHeight = 29;
        list.BorderStyle = BorderStyle.FixedSingle;
        list.HorizontalScrollbar = true;
    }

    private void RefreshInstalled()
    {
        string mods = Path.Combine(root, "mods");
        Directory.CreateDirectory(mods);
        var registered = TokyoModrinthClient.LoadRegistry(root)
            .Where(x => x.Type == "mod").ToList();
        installed.BeginUpdate();
        installed.Items.Clear();
        foreach (string path in Directory.EnumerateFiles(mods, "*.jar")
            .OrderBy(x => Path.GetFileName(x), StringComparer.OrdinalIgnoreCase))
        {
            string filename = Path.GetFileName(path);
            var entry = registered.FirstOrDefault(x => x.FileName.Equals(filename, StringComparison.OrdinalIgnoreCase));
            installed.Items.Add(new InstalledEntry(path, entry));
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
        notice.Text = "Wyszukuję zgodne mody...";
        try
        {
            var found = await catalog.SearchAsync(query.Text, false);
            if (IsDisposed) return;
            results.BeginUpdate();
            results.Items.Clear();
            foreach (var item in found) results.Items.Add(item);
            results.EndUpdate();
            notice.Text = $"Znaleziono {found.Count} modów. Pobierane są tylko wydania Forge 1.20.1.";
        }
        catch (Exception ex) { if (!IsDisposed) notice.Text = "Modrinth: " + ex.Message; }
        finally
        {
            busy = false;
            if (!IsDisposed) search.Enabled = true;
        }
    }

    private async Task InstallSelectedAsync()
    {
        if (busy || results.SelectedItem is not CatalogItem selected) return;
        if (MessageBox.Show(this,
            "Dodać \"" + selected.Title + "\" do Tokyo World?\n\n" +
            "Mody mogą wpływać na zapis świata, wydajność i zgodność serwera. " +
            "Launcher pobierze także wymagane zależności, jeśli są dostępne.",
            "Instalacja moda", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        busy = true;
        search.Enabled = false;
        install.Enabled = false;
        notice.Text = "Instalacja: " + selected.Title + "...";
        try
        {
            var files = await catalog.InstallAsync(root, selected, false,
                new Progress<string>(s => { if (!IsDisposed) notice.Text = s; }));
            RefreshInstalled();
            notice.Text = files.Count == 0 ? "Mod był już zainstalowany." :
                "Pobrano " + files.Count + " plików. Uruchom ponownie Minecrafta.";
        }
        catch (Exception e)
        {
            notice.Text = "Błąd instalacji: " + e.Message;
            MessageBox.Show(this, e.Message, "Tokyo World — instalacja moda",
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
        if (installed.SelectedItem is not InstalledEntry { Managed: not null } entry || busy) return;
        if (MessageBox.Show(this,
            "Przenieść \"" + entry.Managed.Name + "\" do kopii zapasowej?\n" +
            "Upewnij się, że nie jest zależnością innych modów, i nie otwieraj światów " +
            "zawierających przedmioty tego dodatku.",
            "Usuwanie dodatku", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            TokyoModrinthClient.RemoveManaged(root, entry.Managed);
            RefreshInstalled();
            notice.Text = "Mod przeniesiono do .tokyo/backups/user-addons.";
        }
        catch (Exception e) { MessageBox.Show(this, e.Message, "Usuwanie dodatku"); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) catalog.Dispose();
        base.Dispose(disposing);
    }
}
