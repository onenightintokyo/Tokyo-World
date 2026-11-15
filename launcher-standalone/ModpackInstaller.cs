using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO.Compression;

namespace TokyoWorldLauncher;

internal sealed class ModpackInstaller
{
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(10) };
    private readonly HashSet<string> processed = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> resolving = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> skipped = new();
    private string modsDir = "";
    private IProgress<string>? status;
    private CancellationToken cancel;

    public ModpackInstaller()
    {
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TokyoWorldStandaloneLauncher/1.0");
    }

    public async Task<ModInstallResult> InstallAsync(string gameDir, bool includeAllModules,
        IProgress<string> progress, CancellationToken cancellation = default)
    {
        modsDir = Path.Combine(gameDir, "mods");
        Directory.CreateDirectory(modsDir);
        status = progress;
        cancel = cancellation;
        processed.Clear();
        resolving.Clear();
        skipped.Clear();

        int before = Directory.EnumerateFiles(modsDir, "*.jar").Count();
        int missingBase = await InstallBaseModsAsync(gameDir);
        string manifest = Path.Combine(gameDir, ".tokyo", "modules.json");
        if (File.Exists(manifest))
        {
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(manifest, cancellation));
            var profiles = json.RootElement.GetProperty("profiles");
            var modules = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var profile in profiles.EnumerateObject())
            {
                // For initial installs the core and repair packages are mandatory.
                bool required = profile.Name is "tech-core" or "tech-expanded" or "tech-lab" or "repair-dependencies" or "adventure";
                if (!includeAllModules && !required) continue;
                foreach (var module in profile.Value.EnumerateArray())
                {
                    var project = module.GetProperty("project").GetString() ?? "";
                    if (project.Length > 0 && !modules.ContainsKey(project))
                        modules[project] = module.Clone();
                }
            }
            foreach (var entry in modules)
            {
                cancellation.ThrowIfCancellationRequested();
                try
                {
                    await InstallProjectAsync(entry.Key, GetString(entry.Value, "versionId"),
                        GetString(entry.Value, "version"), GetString(entry.Value, "match"));
                }
                catch (Exception ex)
                {
                    string message = $"{entry.Key}: {ex.Message}";
                    skipped.Add(message);
                    status.Report("Niedostępny dodatek: " + message);
                }
            }
        }
        string reportDir = Path.Combine(gameDir, ".tokyo");
        Directory.CreateDirectory(reportDir);
        await File.WriteAllLinesAsync(Path.Combine(reportDir, "missing-mods.txt"), skipped, cancellation);
        int after = Directory.EnumerateFiles(modsDir, "*.jar").Count();
        return new ModInstallResult(after - before, missingBase, skipped.Count);
    }

    private static string GetString(JsonElement elem, string name) =>
        elem.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private async Task<int> InstallBaseModsAsync(string gameDir)
    {
        string index = Path.Combine(gameDir, ".tokyo", "base_mod_hashes.txt");
        if (!File.Exists(index))
            throw new InvalidOperationException("Brakuje indeksu bazowych modów w repozytorium (.tokyo/base_mod_hashes.txt).");

        int missing = 0;
        foreach (var raw in await File.ReadAllLinesAsync(index, cancel))
        {
            cancel.ThrowIfCancellationRequested();
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var parts = line.Split('|', 2);
            if (parts.Length != 2 || !SafeName(parts[0]) || !Regex.IsMatch(parts[1], @"\A[a-fA-F0-9]{40}\z"))
                throw new InvalidDataException("Nieprawidłowy wpis indeksu modów: " + line);
            string path = Path.Combine(modsDir, parts[0]);
            if (File.Exists(path) && CheckSha1(path, parts[1])) continue;
            if (File.Exists(path))
                throw new InvalidDataException("Inny plik już istnieje w mods: " + parts[0] + ". Nie nadpisuję go.");
            status!.Report("Baza modów: " + parts[0]);
            try
            {
                string hash = parts[1];
                using var ver = await GetJsonAsync($"version_file/{hash}?algorithm=sha1", optional: true);
                if (ver is null)
                {
                    missing++;
                    skipped.Add("BAZA: " + parts[0] + " — brak dokładnego pliku na Modrinth");
                    continue;
                }
                var match = ver.RootElement.GetProperty("files").EnumerateArray().FirstOrDefault(f =>
                    string.Equals(GetString(f.GetProperty("hashes"), "sha1"), hash, StringComparison.OrdinalIgnoreCase));
                if (match.ValueKind != JsonValueKind.Object)
                {
                    missing++;
                    skipped.Add("BAZA: " + parts[0] + " — brak zgodnego SHA1");
                    continue;
                }
                string url = GetString(match, "url");
                await DownloadCheckedAsync(url, path, hash, algorithm: "sha1");
            }
            catch (Exception ex)
            {
                missing++;
                skipped.Add("BAZA: " + parts[0] + " — " + ex.Message);
            }
        }
        return missing;
    }

    private async Task InstallProjectAsync(string projectRef, string versionId, string versionNeed, string matchPattern)
    {
        if (processed.Contains(projectRef) || resolving.Contains(projectRef)) return;
        resolving.Add(projectRef);
        try
        {
            using var project = await GetJsonAsync($"project/{Uri.EscapeDataString(projectRef)}");
            if (project is null) throw new InvalidOperationException("Nie znaleziono projektu Modrinth");
            string slug = GetString(project.RootElement, "slug");
            string id = GetString(project.RootElement, "id");
            if (processed.Contains(id)) return;
            if (IsInstalled(slug, matchPattern)) { processed.Add(id); return; }

            JsonDocument? version = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(versionId))
                    version = await GetJsonAsync($"version/{Uri.EscapeDataString(versionId)}");
                else
                {
                    string query = $"project/{Uri.EscapeDataString(id)}/version?game_versions=%5B%221.20.1%22%5D&loaders=%5B%22forge%22%5D";
                    using var variants = await GetJsonAsync(query);
                    if (variants is null) throw new InvalidOperationException("Brak wydań");
                    var choices = variants.RootElement.EnumerateArray().Where(v =>
                        v.GetProperty("game_versions").EnumerateArray().Any(x => x.GetString() == "1.20.1") &&
                        v.GetProperty("loaders").EnumerateArray().Any(x => x.GetString() == "forge") &&
                        (versionNeed.Length == 0 || GetString(v, "version_number").Contains(versionNeed, StringComparison.OrdinalIgnoreCase)))
                        .OrderByDescending(v => GetString(v, "date_published")).ToArray();
                    var selected = choices.FirstOrDefault(x => GetString(x, "version_type") == "release");
                    if (selected.ValueKind != JsonValueKind.Object) selected = choices.FirstOrDefault();
                    if (selected.ValueKind != JsonValueKind.Object)
                        throw new InvalidOperationException("Brak wydania Forge 1.20.1");
                    version = JsonDocument.Parse(selected.GetRawText());
                }
                if (version is null) throw new InvalidOperationException("Nie znaleziono wydania");
                var ver = version.RootElement;
                if (!ver.GetProperty("game_versions").EnumerateArray().Any(x => x.GetString() == "1.20.1")
                    || !ver.GetProperty("loaders").EnumerateArray().Any(x => x.GetString() == "forge"))
                    throw new InvalidOperationException("Wydanie ma zły loader lub wersję Minecraft");
                if (GetString(ver, "project_id") != id)
                    throw new InvalidOperationException("Przypięte wydanie nie należy do projektu");

                // Resolve all required dependencies before activating this mod.
                if (ver.TryGetProperty("dependencies", out var deps) && deps.ValueKind == JsonValueKind.Array)
                {
                    foreach (var dep in deps.EnumerateArray())
                    {
                        if (GetString(dep, "dependency_type") != "required") continue;
                        string depId = GetString(dep, "project_id");
                        if (depId.Length == 0) continue;
                        await InstallProjectAsync(depId, "", "", "");
                    }
                }
                var files = ver.GetProperty("files").EnumerateArray()
                    .Where(x => GetString(x, "filename").EndsWith(".jar", StringComparison.OrdinalIgnoreCase)).ToArray();
                var primary = files.FirstOrDefault(x => x.TryGetProperty("primary", out var p) && p.GetBoolean());
                if (primary.ValueKind != JsonValueKind.Object) primary = files.FirstOrDefault();
                if (primary.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException("Nie znaleziono pliku JAR");
                string filename = GetString(primary, "filename");
                if (!SafeName(filename)) throw new InvalidDataException("Niebezpieczna nazwa moda: " + filename);
                string destination = Path.Combine(modsDir, filename);
                string sha512 = GetString(primary.GetProperty("hashes"), "sha512");
                if (!Regex.IsMatch(sha512, @"\A[a-fA-F0-9]{128}\z"))
                    throw new InvalidDataException("Brak SHA512 pliku " + filename);
                if (File.Exists(destination))
                {
                    if (!CheckSha512(destination, sha512))
                        throw new InvalidDataException("Istniejący JAR ma inną sumę: " + filename);
                    processed.Add(id);
                    return;
                }
                status!.Report("Dodatek: " + slug + " → " + filename);
                await DownloadCheckedAsync(GetString(primary, "url"), destination, sha512, "sha512");
                processed.Add(id);
            }
            finally { version?.Dispose(); }
        }
        finally { resolving.Remove(projectRef); }
    }

    private bool IsInstalled(string slug, string match)
    {
        string[] names = Directory.EnumerateFiles(modsDir, "*.jar").Select(Path.GetFileName).ToArray()!;
        if (match.Length > 0)
        {
            try { if (names.Any(x => Regex.IsMatch(x, match, RegexOptions.IgnoreCase))) return true; }
            catch (ArgumentException) { /* Ignore faulty manifest matcher. */ }
        }
        string simple = Regex.Replace(slug, @"[-_ ]", "");
        return names.Any(x => Regex.Replace(x!, @"[-_ ]", "").StartsWith(simple, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<JsonDocument?> GetJsonAsync(string api, bool optional = false)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            using var resp = await http.GetAsync("https://api.modrinth.com/v2/" + api, cancel);
            if (optional && resp.StatusCode == HttpStatusCode.NotFound) return null;
            if ((int)resp.StatusCode == 429 || (int)resp.StatusCode >= 500)
            {
                await Task.Delay(1500 * (attempt + 1), cancel);
                continue;
            }
            resp.EnsureSuccessStatusCode();
            return JsonDocument.Parse(await resp.Content.ReadAsStreamAsync(cancel));
        }
        throw new HttpRequestException("Modrinth nie odpowiada po ponowieniach: " + api);
    }

    private static bool SafeName(string s) =>
        s.Length is > 0 and <= 200 &&
        s.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) &&
        s == Path.GetFileName(s) &&
        !s.EndsWith('.') && !s.EndsWith(' ') &&
        s.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    private async Task DownloadCheckedAsync(string url, string destination, string expectedHash, string algorithm)
    {
        var uri = new Uri(url);
        if (uri.Scheme != "https" || uri.Host is not ("cdn.modrinth.com" or "cdn-raw.modrinth.com"))
            throw new InvalidDataException("Pobieranie z niezatwierdzonego serwera: " + uri.Host);
        string temporary = destination + ".tokyo-download";
        try
        {
            using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancel);
            response.EnsureSuccessStatusCode();
            await using (var input = await response.Content.ReadAsStreamAsync(cancel))
            await using (var output = File.Create(temporary))
                await input.CopyToAsync(output, cancel);
            bool correct = algorithm == "sha1" ? CheckSha1(temporary, expectedHash) : CheckSha512(temporary, expectedHash);
            if (!correct) throw new CryptographicException("Suma kontrolna JAR nie zgadza się: " + Path.GetFileName(destination));
            if (File.Exists(destination)) throw new IOException("Plik pojawił się w czasie pobierania. Nie nadpisuję.");
            File.Move(temporary, destination);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static void ImportOriginalZip(string zipPath, string gameRoot, IProgress<string> status)
    {
        string mods = Path.Combine(gameRoot, "mods");
        Directory.CreateDirectory(mods);
        using var zip = ZipFile.OpenRead(zipPath);
        foreach (var entry in zip.Entries.Where(x => x.FullName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)
              && x.FullName.Replace('\\', '/').Contains("/mods/", StringComparison.OrdinalIgnoreCase)))
        {
            string file = Path.GetFileName(entry.Name);
            if (!SafeName(file)) continue;
            string path = Path.Combine(mods, file);
            if (File.Exists(path)) continue;
            status.Report("Import: " + file);
            entry.ExtractToFile(path);
        }
    }

    private static bool CheckSha1(string path, string expected) =>
        Convert.ToHexString(SHA1.HashData(File.ReadAllBytes(path))).Equals(expected, StringComparison.OrdinalIgnoreCase);
    private static bool CheckSha512(string path, string expected) =>
        Convert.ToHexString(SHA512.HashData(File.ReadAllBytes(path))).Equals(expected, StringComparison.OrdinalIgnoreCase);
}

internal record ModInstallResult(int Installed, int MissingBase, int SkippedOptional);
