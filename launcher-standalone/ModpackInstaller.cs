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
    private bool suppressEveryCompat;
    private IProgress<string>? status;
    private CancellationToken cancel;

    public ModpackInstaller()
    {
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TokyoWorldStandaloneLauncher/1.0");
    }

    public async Task<ModInstallResult> InstallAsync(string gameDir, bool includeAllModules,
        bool lightweightMode, bool extraVisualMods, bool enableShaders,
        IProgress<string> progress, CancellationToken cancellation = default)
    {
        modsDir = Path.Combine(gameDir, "mods");
        suppressEveryCompat = lightweightMode;
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
                bool required = profile.Name is "tech-core" or "tech-expanded" or "tech-lab" or "repair-dependencies" or "adventure" or "performance-fps" or "community-core" or "client-hud";
                if (profile.Name == "visual-extras" && !extraVisualMods) continue;
                if (profile.Name == "visual-shaders" && !enableShaders) continue;
                if (!includeAllModules && !required && profile.Name is not ("visual-extras" or "visual-shaders")) continue;
                foreach (var module in profile.Value.EnumerateArray())
                {
                    var project = module.GetProperty("project").GetString() ?? "";
                    if (project.Length > 0 &&
                        !project.Equals("every-compat", StringComparison.OrdinalIgnoreCase) &&
                        !project.Equals("everycomp", StringComparison.OrdinalIgnoreCase) &&
                        !modules.ContainsKey(project))
                        modules[project] = module.Clone();
                }
            }
            foreach (var entry in modules)
            {
                cancellation.ThrowIfCancellationRequested();
                try
                {
                    if (GetString(entry.Value, "source").Equals("curseforge", StringComparison.OrdinalIgnoreCase))
                        await InstallOfficialCurseForgeAsync(gameDir, entry.Key,
                            GetString(entry.Value, "fileName"), GetString(entry.Value, "url"),
                            GetString(entry.Value, "sha1"));
                    else
                        await InstallProjectAsync(entry.Key, GetString(entry.Value, "versionId"),
                            GetString(entry.Value, "version"), GetString(entry.Value, "match"));
                }
                catch (Exception ex)
                {
                    if (entry.Key.Equals("packet-fixer", StringComparison.OrdinalIgnoreCase) ||
                        (entry.Value.TryGetProperty("requiredForMultiplayer", out var mandatory) &&
                         mandatory.ValueKind == JsonValueKind.True))
                        throw new InvalidOperationException(
                            "Brak obowiązkowego moda sieciowego Tokyo World: " + entry.Key + ". " +
                            "Nie uruchamiam gry z niezgodną paczką. Szczegóły: " + ex.Message, ex);
                    string message = $"{entry.Key}: {ex.Message}";
                    skipped.Add(message);
                    status.Report("Niedostępny dodatek: " + message);
                }
            }
        }
        // Verify the actual Forge mod ID and SHA512; a matching filename alone
        // does not prove expatternprovider is installed or compatible.
        await EnsureExtendedAeDependencyAsync(gameDir);

        // Reconcile pinned mod compatibility after optional modules have been resolved.
        // This fixes Delightful 3.8.1 vs End\u0027s Delight 2.4 and duplicate IntegratedTerminals.
        await ModCompatibilityRepair.ApplyAsync(gameDir, progress, cancellation);
        // Validate the installed binaries, not just the intended manifest.
        // Show all missing mandatory Forge mod IDs before starting the game.
        TokyoModIntegrity.Verify(gameDir);
        // Forge channel parity is stricter than generic missing-mod checks:
        // each multiplayer JAR must be byte-for-byte the verified server version.
        VerifyMultiplayerParity(gameDir);

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
            // FTB Library/Teams are distributed officially via CurseForge rather
            // than Modrinth. They will be fetched by the signed SHA1 CurseForge
            // manifest below, even when missing from a fresh installation.
            if (Regex.IsMatch(parts[0], @"\Aftb-(library|teams|quests)-forge-", RegexOptions.IgnoreCase))
                continue;
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


    private async Task InstallOfficialCurseForgeAsync(
        string gameDir, string projectRef, string fileName, string url, string sha1)
    {
        // Only pinned, publisher SHA1-verified CurseForge releases are accepted.
        // The exact Twilight Forest JAR must match the verified dedicated server
        // or joining fails with missing twilight:magic_paintings/restrictions.
        bool isTwilight = projectRef.Equals("twilightforest", StringComparison.OrdinalIgnoreCase);
        // Only these exact file hashes, verified against the server ZIP, may
        // supply Forge's mandatory multiplayer network channels. The manifest
        // itself cannot silently change the approved binary or pick Fabric.
        var networkPins = new Dictionary<string, (string Name, string Sha1)>(StringComparer.OrdinalIgnoreCase)
        {
            ["compact-machines"] = ("compactmachines-forge-6.0.3.jar", "41debe12bc7b013c39998788ed02ee971a11518f"),
            ["building-gadgets"] = ("buildinggadgets2-1.0.8.jar", "d06a806e8ca84a9e96a69c32da4b43226a51e0c9"),
            ["mining-gadgets"] = ("mininggadgets-1.15.6.jar", "79cfc6e0afc3ed3fc86bbda274ef3fccdc8260dd"),
            ["flux-networks"] = ("FluxNetworks-1.20.1-7.2.1.15.jar", "65cea2d3da2a6d6fd274ef1dad37a516466be22c"),
            ["laserio"] = ("laserio-1.6.8.jar", "f9e8dc07506eaa058f64e875e1f46dcd44ab6003"),
            ["iron-chests-restocked"] = ("ironchests-5.0.2-forge.jar", "610aa09a1d83253db6bf32f6b12c84d446fa99b6"),
            ["born-in-chaos"] = ("born_in_chaos_[Forge]1.20.1_1.7.5.jar", "4d616e1ccef4c55e14b04ca63228d826f4fdda5e"),
            ["the-graveyard"] = ("The_Graveyard_3.1_(FORGE)_for_1.20.1.jar", "11ae4c94aa94b38b78cf9b901dad4612b679f8ac"),
        };
        bool isNetwork = networkPins.TryGetValue(projectRef, out var networkPin);
        if (!isTwilight && !isNetwork &&
            projectRef is not ("ftb-library" or "ftb-teams" or "ftb-chunks" or "ftb-quests"))
            throw new InvalidDataException("Niezatwierdzony projekt CurseForge: " + projectRef);
        if (!SafeName(fileName) || !Regex.IsMatch(sha1, @"\A[a-fA-F0-9]{40}\z"))
            throw new InvalidDataException("Niepoprawna nazwa lub SHA1 CurseForge: " + fileName);
        if (isNetwork &&
            (fileName != networkPin.Name ||
             !sha1.Equals(networkPin.Sha1, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Mod sieciowy nie odpowiada wersji serwera: " + projectRef);
        if (isTwilight)
        {
            if (fileName != "twilightforest-1.20.1-4.3.2508-universal.jar" ||
                !sha1.Equals("fa3b506a45d3e9c465551cd533fd50c899496e9f", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Twilight Forest wymaga dokładnego JAR-a 4.3.2508 używanego na serwerze.");
        }
        else if (!isNetwork && !fileName.StartsWith(projectRef + "-forge-", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Niepoprawny plik FTB: " + fileName);

        // URL-encoded square brackets/parentheses are expected in original
        // CurseForge filenames (Born in Chaos, Graveyard).
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            uri.Host != "mediafilez.forgecdn.net" ||
            !Regex.IsMatch(Uri.UnescapeDataString(uri.AbsolutePath),
                @"\A/files/[0-9]+/[0-9]+/[^/]+\.jar\z") ||
            !Uri.UnescapeDataString(uri.AbsolutePath).EndsWith("/" + fileName, StringComparison.Ordinal))
            throw new InvalidDataException("Nieautoryzowany adres pobierania CurseForge: " + url);

        string file = Path.Combine(modsDir, fileName);
        if (File.Exists(file))
        {
            if (CheckSha1(file, sha1))
            {
                if (isTwilight || isNetwork) RetireOlderSharedJars(isTwilight ? "twilightforest" : projectRef, file);
                status?.Report("Zweryfikowano SHA1: " + fileName);
                return;
            }
            // Incorrect/stale FTB JAR? Verify a replacement before moving
            // the original into .tokyo/backups/official-ftb, never delete it.
            status?.Report("FTB: naprawiam JAR o niezgodnej sumie SHA1: " + fileName);
        }
        string temporary = file + ".tokyo-download";
        try
        {
            status?.Report("Pobieram oficjalny FTB: " + fileName);
            using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancel);
            response.EnsureSuccessStatusCode();
            var finalUri = response.RequestMessage?.RequestUri;
            if (finalUri?.Scheme != Uri.UriSchemeHttps ||
                finalUri.Host is not ("mediafilez.forgecdn.net" or "edge.forgecdn.net" or "media.forgecdn.net"))
                throw new InvalidDataException("Nieautoryzowane przekierowanie CurseForge.");
            if (response.Content.Headers.ContentLength is > 25_000_000)
                throw new InvalidDataException("FTB: plik jest zbyt duży.");
            await using (var src = await response.Content.ReadAsStreamAsync(cancel))
            await using (var dst = File.Create(temporary))
                await src.CopyToAsync(dst, cancel);
            if (new FileInfo(temporary).Length is <= 0 or > 25_000_000 ||
                !CheckSha1(temporary, sha1))
                throw new CryptographicException("FTB: SHA1 nie zgadza się dla " + fileName);

            // Avoid duplicate versions of one Forge mod ID. The old copy is
            // moved only AFTER the new one has been downloaded and checked.
            string backup = Path.Combine(gameDir, ".tokyo", "backups",
                "replaced-official-mods", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            string? previous = null;
            if (File.Exists(file))
            {
                // A corrupt/stale JAR at the expected path must not block its
                // verified replacement. Preserve a copy for recovery.
                Directory.CreateDirectory(backup);
                previous = Path.Combine(backup, fileName);
                File.Move(file, previous);
            }
            try
            {
                File.Move(temporary, file);
            }
            catch
            {
                if (previous is not null && !File.Exists(file))
                    File.Move(previous, file);
                throw;
            }
            if (isTwilight || isNetwork)
            {
                // Retire OTHER versions, e.g. fluxnetworks-*.jar or
                // born_in_chaos_*.jar; never retire the verified destination.
                RetireOlderSharedJars(isTwilight ? "twilightforest" : projectRef, file);
            }
            else
            {
                string pattern = @"\A" + Regex.Escape(projectRef) + @"-forge-[^/\\]+\.jar\z";
                foreach (string older in Directory.EnumerateFiles(modsDir, "*.jar")
                    .Where(x => !Path.GetFullPath(x).Equals(Path.GetFullPath(file), StringComparison.OrdinalIgnoreCase))
                    .Where(x => Regex.IsMatch(Path.GetFileName(x), pattern, RegexOptions.IgnoreCase)).ToArray())
                {
                    Directory.CreateDirectory(backup);
                    File.Move(older, Path.Combine(backup, Path.GetFileName(older)));
                }
            }
            status?.Report("Zainstalowano i sprawdzono SHA1: " + fileName);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private async Task InstallProjectAsync(string projectRef, string versionId, string versionNeed, string matchPattern)
    {
        // Never reinstall an intentionally disabled optional mod on update.
        if (projectRef.Equals("every-compat", StringComparison.OrdinalIgnoreCase)
            || projectRef.Equals("everycomp", StringComparison.OrdinalIgnoreCase)) return;
        if (processed.Contains(projectRef) || resolving.Contains(projectRef)) return;
        resolving.Add(projectRef);
        try
        {
            using var project = await GetJsonAsync($"project/{Uri.EscapeDataString(projectRef)}");
            if (project is null) throw new InvalidOperationException("Nie znaleziono projektu Modrinth");
            string slug = GetString(project.RootElement, "slug");
            string id = GetString(project.RootElement, "id");
            bool enforceSharedVersion = slug is "the-bumblezone" or "sophisticated-core" or "extended-ae" or "guideme" or "glodium" or "kotlin-for-forge" or "thermal-foundation" or "packet-fixer";
            if (processed.Contains(id) && !enforceSharedVersion) return;
            if (!enforceSharedVersion && IsInstalled(slug, matchPattern)) { processed.Add(id); return; }

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
                    if (enforceSharedVersion) RetireOlderSharedJars(slug, destination);
                    processed.Add(id);
                    return;
                }
                status!.Report("Dodatek: " + slug + " → " + filename);
                await DownloadCheckedAsync(GetString(primary, "url"), destination, sha512, "sha512");
                if (enforceSharedVersion) RetireOlderSharedJars(slug, destination);
                processed.Add(id);
            }
            finally { version?.Dispose(); }
        }
        finally { resolving.Remove(projectRef); }
    }

    private void VerifyMultiplayerParity(string gameDir)
    {
        string manifestPath = Path.Combine(gameDir, ".tokyo", "modules.json");
        string reportPath = Path.Combine(gameDir, ".tokyo", "network-parity.txt");
        var report = new List<string>
        {
            "Tokyo World — kontrola modów wymaganych przez kanały Forge",
            "Minecraft 1.20.1 / Forge 47.4.28"
        };
        var errors = new List<string>();
        if (!File.Exists(manifestPath))
        {
            errors.Add("Brakuje aktualnego manifestu modułów: " + manifestPath);
        }
        else
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
            foreach (var group in doc.RootElement.GetProperty("profiles").EnumerateObject())
            foreach (var entry in group.Value.EnumerateArray())
            {
                if (!entry.TryGetProperty("requiredForMultiplayer", out var required) ||
                    required.ValueKind != JsonValueKind.True) continue;
                string project = GetString(entry, "project");
                string filename = GetString(entry, "fileName");
                string expectedSha1 = GetString(entry, "sha1");
                string expectedSha256 = GetString(entry, "sha256");
                if (!SafeName(filename) ||
                    !Regex.IsMatch(expectedSha1, @"\A[0-9a-f]{40}\z", RegexOptions.IgnoreCase) ||
                    !Regex.IsMatch(expectedSha256, @"\A[0-9a-f]{64}\z", RegexOptions.IgnoreCase))
                {
                    errors.Add(project + ": niepoprawna konfiguracja obowiązkowego moda");
                    continue;
                }
                string file = Path.Combine(modsDir, filename);
                if (!File.Exists(file))
                {
                    errors.Add(project + ": BRAK " + filename);
                    continue;
                }
                if (!CheckSha1(file, expectedSha1))
                {
                    errors.Add(project + ": niezgodna suma SHA1 " + filename);
                    continue;
                }
                using var stream = File.OpenRead(file);
                string actualSha256 = Convert.ToHexString(SHA256.HashData(stream));
                if (!actualSha256.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(project + ": niezgodna suma SHA256 " + filename);
                    continue;
                }
                report.Add("OK " + project + ": " + filename + " (" + actualSha256.ToLowerInvariant() + ")");
            }
        }
        report.AddRange(errors.Select(x => "ERROR " + x));
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllLines(reportPath, report);
        if (errors.Count > 0)
            throw new InvalidOperationException(
                "Niezgodne mody sieciowe Tokyo World:\n" +
                string.Join("\n", errors.Take(8)) +
                "\nSzczegóły: .tokyo/network-parity.txt");
        status?.Report("Kanały multiplayer: dokładne pliki JAR zgodne z serwerem.");
    }

    private static bool ContainsForgeModId(string jarPath, string modId)
    {
        try
        {
            using var archive = ZipFile.OpenRead(jarPath);
            var entry = archive.Entries.FirstOrDefault(e =>
                e.FullName.Equals("META-INF/mods.toml", StringComparison.OrdinalIgnoreCase));
            if (entry is null || entry.Length > 1_000_000) return false;
            using var reader = new StreamReader(entry.Open());
            string toml = reader.ReadToEnd();
            return Regex.IsMatch(toml,
                @"(?im)^\s*modId\s*=\s*[""']" + Regex.Escape(modId) + @"[""']\s*(?:#.*)?$");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException)
        {
            return false;
        }
    }

    private async Task EnsureExtendedAeDependencyAsync(string gameDir)
    {
        const string expectedFile = "ExtendedAE-1.20-1.4.18-forge.jar";
        const string expectedProject = "JiOqfoFM";
        string installed = Path.Combine(modsDir, expectedFile);

        // Fetch the pinned release itself, not the project slug. This bypasses
        // stale processed/IsInstalled state and ExtendedAEPlus prefix collisions.
        using var version = await GetJsonAsync("version/uq3lO4ER");
        if (version is null)
            throw new InvalidOperationException("Nie można pobrać metadanych Extended AE.");
        var v = version.RootElement;
        if (!GetString(v, "project_id").Equals(expectedProject, StringComparison.Ordinal) ||
            !v.GetProperty("game_versions").EnumerateArray().Any(x => x.GetString() == "1.20.1") ||
            !v.GetProperty("loaders").EnumerateArray().Any(x => x.GetString() == "forge"))
            throw new InvalidDataException("Extended AE ma niezgodny identyfikator projektu, Minecraft lub loader.");

        var candidates = v.GetProperty("files").EnumerateArray()
            .Where(x => GetString(x, "filename").Equals(expectedFile, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (candidates.Length != 1)
            throw new InvalidDataException("Wersja Extended AE nie zawiera oczekiwanego JAR: " + expectedFile);
        var fileInfo = candidates[0];
        string sha512 = GetString(fileInfo.GetProperty("hashes"), "sha512");
        string url = GetString(fileInfo, "url");
        if (!Regex.IsMatch(sha512, @"\A[a-fA-F0-9]{128}\z"))
            throw new InvalidDataException("Extended AE nie posiada poprawnej sumy SHA-512.");

        if (File.Exists(installed) && CheckSha512(installed, sha512) &&
            ContainsForgeModId(installed, "expatternprovider"))
        {
            RetireOlderSharedJars("extended-ae", installed);
            status?.Report("Extended AE / expatternprovider: sprawdzono SHA-512 i mods.toml.");
            return;
        }

        // Download and verify in a staging folder first. Never delete a working
        // JAR before the replacement is available and its Forge metadata verified.
        string staging = Path.Combine(gameDir, ".tokyo", "repair-staging");
        Directory.CreateDirectory(staging);
        string tempJar = Path.Combine(staging, Guid.NewGuid().ToString("N") + "-" + expectedFile);
        try
        {
            status?.Report("Naprawiam brakujący Extended AE (expatternprovider)...");
            await DownloadCheckedAsync(url, tempJar, sha512, "sha512");
            if (!ContainsForgeModId(tempJar, "expatternprovider"))
                throw new InvalidDataException("Pobrany Extended AE nie zawiera modId expatternprovider.");

            string? prior = null;
            if (File.Exists(installed))
            {
                string archive = Path.Combine(gameDir, ".tokyo", "backups", "invalid-extended-ae");
                Directory.CreateDirectory(archive);
                prior = Path.Combine(archive, Guid.NewGuid().ToString("N") + "-" + expectedFile);
                File.Move(installed, prior);
            }
            try
            {
                File.Move(tempJar, installed);
            }
            catch
            {
                if (prior != null && !File.Exists(installed)) File.Move(prior, installed);
                throw;
            }
            RetireOlderSharedJars("extended-ae", installed);
            status?.Report("Zainstalowano Extended AE 1.4.18 — expatternprovider potwierdzony.");
        }
        finally
        {
            if (File.Exists(tempJar)) File.Delete(tempJar);
        }
    }

    private void RetireOlderSharedJars(string slug, string destination)
    {
        string pattern = slug switch
        {
            "the-bumblezone" => @"\A(?:the_bumblezone|bumblezone)[-_.].*\.jar\z",
            "twilightforest" => @"\A(?:twilightforest|twilight[_-]forest)[-_.].*\.jar\z",
            "extended-ae" => @"\AExtendedAE-1\.20-.*\.jar\z",
            "guideme" => @"\Aguideme[-_.].*\.jar\z",
            "glodium" => @"\AGlodium[-_.].*\.jar\z",
            "kotlin-for-forge" => @"\Akotlinforforge[-_.].*\.jar\z",
            "thermal-foundation" => @"\Athermal_foundation[-_.].*\.jar\z",
            "packet-fixer" => @"\Apacketfixer(?:forge)?[-_.].*\.jar\z",
            "compact-machines" => @"\Acompactmachines[-_.].*\.jar\z",
            "building-gadgets" => @"\A(?:buildinggadgets2|buildinggadgets)[-_.].*\.jar\z",
            "mining-gadgets" => @"\Amininggadgets[-_.].*\.jar\z",
            "flux-networks" => @"\AFluxNetworks[-_.].*\.jar\z",
            "laserio" => @"\Alaserio[-_.].*\.jar\z",
            "iron-chests-restocked" => @"\Aironchests[-_.].*\.jar\z",
            "born-in-chaos" => @"\Aborn_in_chaos[-_.].*\.jar\z",
            "the-graveyard" => @"\A(?:The_Graveyard|the-graveyard)[-_.].*\.jar\z",
            _ => @"\Asophisticatedcore[-_.].*\.jar\z"
        };
        string backup = Path.Combine(Directory.GetParent(modsDir)!.FullName, ".tokyo",
            "backups", "old-shared-mod-versions");
        foreach (string candidate in Directory.EnumerateFiles(modsDir, "*.jar")
            .Where(x => Regex.IsMatch(Path.GetFileName(x), pattern, RegexOptions.IgnoreCase))
            .Where(x => !Path.GetFullPath(x).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            .ToArray())
        {
            Directory.CreateDirectory(backup);
            string target = Path.Combine(backup, Guid.NewGuid().ToString("N") + "-" + Path.GetFileName(candidate));
            File.Move(candidate, target);
            status?.Report("Zarchiwizowano niezgodną wersję moda: " + Path.GetFileName(candidate));
        }
    }

    private bool IsInstalled(string slug, string match)
    {
        string[] names = Directory.EnumerateFiles(modsDir, "*.jar").Select(Path.GetFileName).ToArray()!;
        if (match.Length > 0)
        {
            try { if (names.Any(x => Regex.IsMatch(x, match, RegexOptions.IgnoreCase))) return true; }
            catch (ArgumentException) { /* Ignore faulty manifest matcher. */ }
        }
        // Never treat ExtendedAEPlus as ExtendedAE: a valid slug must end at a
        // version/file-name boundary, not in the middle of another mod name.
        string simple = Regex.Replace(slug, @"[-_ ]", "");
        return names.Any(x =>
        {
            string normalized = Regex.Replace(x!, @"[-_ ]", "");
            return normalized.StartsWith(simple, StringComparison.OrdinalIgnoreCase)
                && (normalized.Length == simple.Length || !char.IsLetter(normalized[simple.Length]));
        });
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
