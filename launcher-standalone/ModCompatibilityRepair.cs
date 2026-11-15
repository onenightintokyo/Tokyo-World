using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TokyoWorldLauncher;

/// <summary>
/// Repairs confirmed Forge mod conflicts without touching worlds.
/// Stale JARs are moved to a dated backup (not deleted).
/// Downloads are validated before old files are moved. Failed swaps roll back.
/// </summary>
internal static class ModCompatibilityRepair
{
    private const string EndDelightProjectId = "yHN0njMr";
    private const string EndDelightForgeVersionId = "txuQ2GPU"; // End's Delight 2.6+forge.1.20.1
    private static readonly Regex EndDelightFile = new(
        @"\Aends_delight[-_.].*\.jar\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex IntegratedTerminalsFile = new(
        @"\AIntegratedTerminals[-_.].*\.jar\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static Version? EndDelightVersion(string filename)
    {
        var match = Regex.Match(filename, @"\Aends_delight-(?:1\.20\.1-)?(?<version>\d+\.\d+(?:\.\d+)?)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success && Version.TryParse(match.Groups["version"].Value, out var result) ? result : null;
    }

    private static Version? IntegratedTerminalsVersion(string filename)
    {
        var match = Regex.Match(filename,
            @"\AIntegratedTerminals-1\.20\.1-(?<version>\d+\.\d+(?:\.\d+)?)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success && Version.TryParse(match.Groups["version"].Value, out var result) ? result : null;
    }

    public static async Task ApplyAsync(string gameDir, IProgress<string> report,
        CancellationToken cancellation = default)
    {
        string modsDir = Path.Combine(gameDir, "mods");
        if (!Directory.Exists(modsDir)) return;

        var endFiles = Directory.EnumerateFiles(modsDir, "*.jar")
            .Where(x => EndDelightFile.IsMatch(Path.GetFileName(x))).ToArray();

        // Keep the highest compatible version already present. No automatic downgrade
        // from 2.6.1 to the pinned 2.6.
        var goodEnd = endFiles
            .Select(f => new { File = f, Version = EndDelightVersion(Path.GetFileName(f)) })
            .Where(x => x.Version is not null && x.Version >= new Version(2, 5))
            .OrderByDescending(x => x.Version).FirstOrDefault();
        if (endFiles.Length > 0)
        {
            string? replacement = null;
            if (goodEnd == null)
            {
                report.Report("Naprawiam konflikt Delightful: End's Delight wymaga wersji 2.5 lub nowszej...");
                replacement = await DownloadEndDelightAsync(gameDir, report, cancellation);
            }

            var obsolete = endFiles.Where(f =>
                goodEnd is null || !Path.GetFullPath(f).Equals(
                    Path.GetFullPath(goodEnd.File), StringComparison.OrdinalIgnoreCase)).ToArray();

            // A transaction: download/verify first, then stash, then activate;
            // restore original JAR files if activating the replacement fails.
            string? target = replacement == null ? null :
                Path.Combine(modsDir, Path.GetFileName(replacement)[..^".verified".Length]);
            var moved = new List<(string Source, string Backup)>();
            try
            {
                foreach (string file in obsolete)
                    Stash(gameDir, file, moved, report);
                if (replacement != null)
                {
                    if (target == null || File.Exists(target))
                        throw new IOException("Nie można bezpiecznie aktywować poprawionej wersji End's Delight.");
                    File.Move(replacement, target);
                    report.Report("End's Delight 2.6 zainstalowany; poprzednia wersja jest w kopii zapasowej.");
                }
            }
            catch
            {
                if (replacement != null && target != null && File.Exists(target))
                    File.Move(target, replacement, overwrite: true);
                foreach (var entry in moved.AsEnumerable().Reverse())
                    if (!File.Exists(entry.Source) && File.Exists(entry.Backup))
                        File.Move(entry.Backup, entry.Source);
                throw;
            }
        }

        // Found in latest.log: IntegratedTerminals 1.7.0 and 1.7.1 simultaneously.
        // Forge must not receive two copies of the same mod.
        var terminals = Directory.EnumerateFiles(modsDir, "*.jar")
            .Where(f => IntegratedTerminalsFile.IsMatch(Path.GetFileName(f)))
            .Select(f => new { File = f, Version = IntegratedTerminalsVersion(Path.GetFileName(f)) })
            .ToArray();
        if (terminals.Length > 1)
        {
            if (terminals.Any(f => f.Version == null))
                throw new InvalidOperationException("Wykryto różne pliki IntegratedTerminals, ale ich wersji nie można wiarygodnie rozpoznać. Wymagana kontrola ręczna.");
            var keep = terminals.OrderByDescending(f => f.Version).First();
            var moved = new List<(string Source, string Backup)>();
            try
            {
                foreach (var file in terminals)
                    if (!file.File.Equals(keep.File, StringComparison.OrdinalIgnoreCase))
                        Stash(gameDir, file.File, moved, report);
                report.Report("Integrated Terminals: pozostawiono tylko " + Path.GetFileName(keep.File) + ".");
            }
            catch
            {
                foreach (var entry in moved.AsEnumerable().Reverse())
                    if (!File.Exists(entry.Source) && File.Exists(entry.Backup))
                        File.Move(entry.Backup, entry.Source);
                throw;
            }
        }
    }

    private static void Stash(string gameDir, string file,
        List<(string Source, string Backup)> moved, IProgress<string> report)
    {
        string dir = Path.Combine(gameDir, ".tokyo", "backups", "mod-conflicts",
            DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(dir);
        string backup = Path.Combine(dir, Path.GetFileName(file));
        if (File.Exists(backup))
            backup = Path.Combine(dir, Guid.NewGuid().ToString("N") + "-" + Path.GetFileName(file));
        File.Move(file, backup);
        moved.Add((file, backup));
        report.Report("Wycofano starszy JAR (zachowano kopię): " + Path.GetFileName(file));
    }

    private static async Task<string> DownloadEndDelightAsync(string gameDir,
        IProgress<string> report, CancellationToken cancellation)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TokyoWorldLauncher/1.3");
        http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await http.GetAsync(
            "https://api.modrinth.com/v2/version/" + EndDelightForgeVersionId, cancellation);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellation));
        var root = json.RootElement;
        if (root.GetProperty("project_id").GetString() != EndDelightProjectId ||
            !root.GetProperty("game_versions").EnumerateArray().Any(v => v.GetString() == "1.20.1") ||
            !root.GetProperty("loaders").EnumerateArray().Any(v => v.GetString() == "forge"))
            throw new InvalidDataException("Modrinth zwrócił End's Delight niezgodny z Forge 1.20.1.");

        var file = root.GetProperty("files").EnumerateArray()
            .FirstOrDefault(v => v.GetProperty("filename").GetString()?.EndsWith(
                ".jar", StringComparison.OrdinalIgnoreCase) == true &&
                v.TryGetProperty("primary", out var primary) && primary.GetBoolean());
        if (file.ValueKind != JsonValueKind.Object)
            file = root.GetProperty("files").EnumerateArray()
                .FirstOrDefault(v => v.GetProperty("filename").GetString()?.EndsWith(
                    ".jar", StringComparison.OrdinalIgnoreCase) == true);
        if (file.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("End's Delight nie ma pliku Forge JAR.");

        var filename = file.GetProperty("filename").GetString() ?? "";
        if (!Regex.IsMatch(filename, @"\Aends_delight[-_.][^/\\:]{1,160}\.jar\z",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            throw new InvalidDataException("Modrinth zwrócił nieprawidłową nazwę JAR.");
        string sha512 = file.GetProperty("hashes").GetProperty("sha512").GetString() ?? "";
        if (!Regex.IsMatch(sha512, @"\A[a-fA-F0-9]{128}\z"))
            throw new InvalidDataException("Brakuje podpisanej sumy SHA-512 moda.");
        var url = new Uri(file.GetProperty("url").GetString() ?? "");
        if (url.Scheme != Uri.UriSchemeHttps ||
            url.Host is not ("cdn.modrinth.com" or "cdn-raw.modrinth.com"))
            throw new InvalidDataException("Nieznane źródło pobierania moda.");

        string staging = Path.Combine(gameDir, ".tokyo", "staging");
        Directory.CreateDirectory(staging);
        string destination = Path.Combine(staging, filename + ".verified");
        try
        {
            using var download = await http.GetAsync(url,
                HttpCompletionOption.ResponseHeadersRead, cancellation);
            download.EnsureSuccessStatusCode();
            await using (var from = await download.Content.ReadAsStreamAsync(cancellation))
            await using (var to = File.Create(destination))
                await from.CopyToAsync(to, cancellation);
            await using var stream = File.OpenRead(destination);
            string digest = Convert.ToHexString(await SHA512.HashDataAsync(stream, cancellation));
            if (!digest.Equals(sha512, StringComparison.OrdinalIgnoreCase))
                throw new CryptographicException("Pobrany End's Delight ma nieprawidłową sumę SHA-512.");
            report.Report("Pobrano i sprawdzono SHA-512: " + filename);
            return destination;
        }
        catch
        {
            if (File.Exists(destination)) File.Delete(destination);
            throw;
        }
    }
}
