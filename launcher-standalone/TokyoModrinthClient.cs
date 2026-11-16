using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TokyoWorldLauncher;

internal sealed record CatalogItem(string Id, string Title, string Description, string Author)
{
    public override string ToString() => Title + " — " + Author;
}

internal sealed record ManagedMod(string ProjectId, string Name, string FileName, string Type);

/// <summary>
/// Safe, Forge 1.20.1-specific Modrinth browser and installer.
/// Only validated CDN binaries are saved; downloads are atomic and verifiable.
/// The launcher never removes modpack-managed files from its search UI.
/// </summary>
internal sealed class TokyoModrinthClient : IDisposable
{
    private const string Api = "https://api.modrinth.com/v2";
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(6) };
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public TokyoModrinthClient()
    {
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TokyoWorldLauncher/1.7 (Modrinth catalog)");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<List<CatalogItem>> SearchAsync(string term, bool shaders, CancellationToken cancel = default)
    {
        term = term?.Trim() ?? "";
        var filter = shaders
            ? "[[\"project_type:shader\"],[\"versions:1.20.1\"]]"
            : "[[\"project_type:mod\"],[\"versions:1.20.1\"],[\"categories:forge\"]]";
        string url = Api + "/search?query=" + Uri.EscapeDataString(term) +
            "&facets=" + Uri.EscapeDataString(filter) + "&limit=24";
        using var json = await GetAsync(url, cancel);
        var list = new List<CatalogItem>();
        foreach (var hit in json.RootElement.GetProperty("hits").EnumerateArray())
        {
            string id = GetString(hit, "project_id");
            if (id.Length == 0) continue;
            list.Add(new CatalogItem(id, GetString(hit, "title"),
                GetString(hit, "description"), GetString(hit, "author")));
        }
        return list;
    }

    public async Task<IReadOnlyList<string>> InstallAsync(
        string root, CatalogItem selected, bool shader, IProgress<string> log,
        CancellationToken cancel = default)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var installed = new List<string>();
        await InstallProjectAsync(root, selected.Id, shader, visited, installed, log, cancel);
        return installed;
    }

    private async Task InstallProjectAsync(string root, string projectId, bool shader,
        HashSet<string> visited, List<string> installed, IProgress<string> log, CancellationToken cancel)
    {
        if (!Regex.IsMatch(projectId, @"\A[A-Za-z0-9_-]{2,80}\z"))
            throw new InvalidDataException("Nieprawidłowy identyfikator projektu.");
        if (!visited.Add(projectId)) return;
        if (visited.Count > 40) throw new InvalidDataException("Za dużo zależności — przerwano.");

        using var project = await GetAsync(Api + "/project/" + Uri.EscapeDataString(projectId), cancel);
        string type = GetString(project.RootElement, "project_type");
        if (type != (shader ? "shader" : "mod"))
            throw new InvalidDataException("Nieprawidłowy typ dodatku: " + projectId);
        string title = GetString(project.RootElement, "title");
        string slug = GetString(project.RootElement, "slug");
        string dir = Path.Combine(root, shader ? "shaderpacks" : "mods");
        Directory.CreateDirectory(dir);

        string versionsUrl = Api + "/project/" + Uri.EscapeDataString(projectId) +
            "/version?game_versions=" + Uri.EscapeDataString("[\"1.20.1\"]");
        if (!shader) versionsUrl += "&loaders=" + Uri.EscapeDataString("[\"forge\"]");
        using var versions = await GetAsync(versionsUrl, cancel);
        var compatible = versions.RootElement.EnumerateArray()
            .Where(x => x.TryGetProperty("game_versions", out var games) &&
                games.EnumerateArray().Any(v => v.GetString() == "1.20.1") &&
                (shader || (x.TryGetProperty("loaders", out var loaders) &&
                    loaders.EnumerateArray().Any(v => v.GetString() == "forge"))))
            .OrderByDescending(x => GetString(x, "date_published")).ToArray();
        var chosen = compatible.FirstOrDefault(x => GetString(x, "version_type") == "release");
        if (chosen.ValueKind != JsonValueKind.Object) chosen = compatible.FirstOrDefault();
        if (chosen.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Brak zgodnej wersji Forge 1.20.1: " + title);

        if (!shader && chosen.TryGetProperty("dependencies", out var dependencies))
        {
            foreach (var dep in dependencies.EnumerateArray())
            {
                if (GetString(dep, "dependency_type") != "required") continue;
                string requiredProject = GetString(dep, "project_id");
                if (requiredProject.Length == 0)
                    throw new InvalidOperationException("Zależność " + title +
                        " nie ma projektu na Modrinth — sprawdź ją ręcznie.");
                await InstallProjectAsync(root, requiredProject, false, visited, installed, log, cancel);
            }
        }

        // Resolve required dependencies even if the main mod is already installed.
        if (!shader && HasInstalledMod(dir, slug))
        {
            log.Report("Mod już obecny, wymagane biblioteki sprawdzone: " + title);
            return;
        }

        string extension = shader ? ".zip" : ".jar";
        var files = chosen.GetProperty("files").EnumerateArray()
            .Where(f => GetString(f, "filename").EndsWith(extension, StringComparison.OrdinalIgnoreCase)).ToArray();
        var selectedFile = files.FirstOrDefault(f => f.TryGetProperty("primary", out var p) && p.GetBoolean());
        if (selectedFile.ValueKind != JsonValueKind.Object) selectedFile = files.FirstOrDefault();
        if (selectedFile.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Brak pliku " + extension + " dla " + title);

        string fileName = GetString(selectedFile, "filename");
        if (fileName != Path.GetFileName(fileName) || fileName.Length is < 5 or > 175 ||
            fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            !fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Niebezpieczna nazwa pliku: " + fileName);
        string hash = GetString(selectedFile.GetProperty("hashes"), "sha512");
        if (!Regex.IsMatch(hash, @"\A[a-fA-F0-9]{128}\z"))
            throw new InvalidDataException("Brak SHA-512: " + title);
        string raw = GetString(selectedFile, "url");
        var uri = new Uri(raw);
        if (uri.Scheme != Uri.UriSchemeHttps ||
            uri.Host is not ("cdn.modrinth.com" or "cdn-raw.modrinth.com"))
            throw new InvalidDataException("Niezaufane źródło pliku " + title);
        string destination = Path.Combine(dir, fileName);
        if (File.Exists(destination))
        {
            await using var existing = File.OpenRead(destination);
            string digest = Convert.ToHexString(await SHA512.HashDataAsync(existing, cancel));
            if (!digest.Equals(hash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("W mods istnieje inny plik o tej nazwie: " + fileName);
            log.Report("Już zainstalowany: " + fileName);
            return;
        }

        string temp = destination + ".tokyo-pending";
        if (File.Exists(temp)) File.Delete(temp);
        try
        {
            log.Report("Pobieram " + title + "...");
            using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancel);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > 536_870_912)
                throw new InvalidDataException("Plik przekracza 512 MB.");
            await using (var source = await response.Content.ReadAsStreamAsync(cancel))
            await using (var output = File.Create(temp))
                await source.CopyToAsync(output, cancel);
            var info = new FileInfo(temp);
            if (info.Length == 0 || info.Length > 536_870_912)
                throw new InvalidDataException("Pobrany plik ma nieprawidłowy rozmiar.");
            await using (var verify = File.OpenRead(temp))
            {
                string digest = Convert.ToHexString(await SHA512.HashDataAsync(verify, cancel));
                if (!digest.Equals(hash, StringComparison.OrdinalIgnoreCase))
                    throw new CryptographicException("SHA-512 nie zgadza się: " + title);
            }
            if (File.Exists(destination))
                throw new IOException("Plik pojawił się podczas pobierania: " + fileName);
            File.Move(temp, destination);
            var registry = LoadRegistry(root);
            registry.RemoveAll(x => x.Type == (shader ? "shader" : "mod") &&
                x.ProjectId.Equals(projectId, StringComparison.OrdinalIgnoreCase));
            registry.Add(new ManagedMod(projectId, title, fileName, shader ? "shader" : "mod"));
            SaveRegistry(root, registry);
            installed.Add(fileName);
            log.Report("Zainstalowano: " + title);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    private static bool HasInstalledMod(string folder, string slug)
    {
        if (slug.Length == 0) return false;
        string normalized = Regex.Replace(slug, @"[-_ .]", "").ToLowerInvariant();
        return Directory.EnumerateFiles(folder, "*.jar").Any(file =>
            Regex.Replace(Path.GetFileNameWithoutExtension(file), @"[-_ .]", "")
            .StartsWith(normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static List<ManagedMod> LoadRegistry(string root)
    {
        string file = Path.Combine(root, ".tokyo", "launcher-installed.json");
        if (!File.Exists(file)) return new();
        try { return JsonSerializer.Deserialize<List<ManagedMod>>(File.ReadAllText(file)) ?? new(); }
        catch (JsonException) { return new(); }
    }

    private static void SaveRegistry(string root, List<ManagedMod> records)
    {
        string folder = Path.Combine(root, ".tokyo");
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "launcher-installed.json");
        string temp = file + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(records, JsonOpts));
        File.Move(temp, file, overwrite: true);
    }

    public static void RemoveManaged(string root, ManagedMod item)
    {
        string folder = Path.Combine(root, item.Type == "shader" ? "shaderpacks" : "mods");
        if (item.FileName != Path.GetFileName(item.FileName))
            throw new InvalidDataException("Nieprawidłowa nazwa pliku.");
        string path = Path.Combine(folder, item.FileName);
        if (!File.Exists(path))
            throw new FileNotFoundException("Nie znaleziono pliku dodatku.", path);
        var records = LoadRegistry(root);
        if (!records.Any(x => x.Type == item.Type && x.ProjectId == item.ProjectId && x.FileName == item.FileName))
            throw new UnauthorizedAccessException("Ten dodatek nie został dodany przez launcher; nie mogę go automatycznie usunąć.");
        string backup = Path.Combine(root, ".tokyo", "backups", "user-addons",
            DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(backup);
        File.Move(path, Path.Combine(backup, item.FileName));
        records.RemoveAll(x => x.Type == item.Type && x.ProjectId == item.ProjectId && x.FileName == item.FileName);
        SaveRegistry(root, records);
    }

    private async Task<JsonDocument> GetAsync(string url, CancellationToken cancel)
    {
        using var response = await http.GetAsync(url, cancel);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancel));
    }

    private static string GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? "" : "";

    public void Dispose() => http.Dispose();
}
