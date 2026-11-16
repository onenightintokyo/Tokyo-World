using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TokyoWorldLauncher;

internal static class LauncherAutoUpdater
{
    // Fetch signed-in user's latest successful Windows build from the configured repo.
    // Never hardcode a private token into the EXE.
    public static async Task<bool> TryUpdateAsync(LauncherSettings settings, IProgress<string> report)
    {
        // GitHub Releases work for everybody once the repository is public.
        // A personal token remains optional for the owner of a private repo.
        if (!System.Text.RegularExpressions.Regex.IsMatch(settings.Repository ?? "",
                @"\A[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+\z"))
            throw new InvalidDataException("Nieprawidłowa nazwa repozytorium aktualizacji.");

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TokyoWorldLauncher/PublicReleaseUpdater");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        // The official Tokyo World source is public. Ignore any previously
        // saved token so a revoked credential cannot break anonymous updates.
        string token = settings.Repository.Equals("onenightintokyo/Tokyo-World",
            StringComparison.OrdinalIgnoreCase) ? "" : settings.GetToken();
        if (!string.IsNullOrWhiteSpace(token))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        string api = "https://api.github.com/repos/" + settings.Repository;
        using var releasesResponse = await http.GetAsync(api + "/releases?per_page=50");
        if (releasesResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            report.Report("Źródło aktualizacji jest prywatne. Po ustawieniu publicznego repo znajomi będą aktualizować EXE bez tokenu.");
            return false;
        }
        releasesResponse.EnsureSuccessStatusCode();
        using var releases = JsonDocument.Parse(await releasesResponse.Content.ReadAsStreamAsync());
        JsonElement selectedAsset = default;
        Version? publishedVersion = null;
        string selectedTag = "";
        foreach (var release in releases.RootElement.EnumerateArray())
        {
            if (release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean())
                continue;
            string tag = release.GetProperty("tag_name").GetString() ?? "";
            var match = System.Text.RegularExpressions.Regex.Match(tag,
                @"\Atokyo-world-launcher-v(?<version>\d+\.\d+\.\d+)\z",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!match.Success || !Version.TryParse(match.Groups["version"].Value + ".0", out Version? version))
                continue;
            if (publishedVersion is not null && version <= publishedVersion) continue;
            var assets = release.GetProperty("assets").EnumerateArray().Where(a =>
                a.GetProperty("name").GetString() == "TokyoWorldLauncher.exe").ToArray();
            if (assets.Length != 1) continue;
            selectedAsset = assets[0].Clone();
            publishedVersion = version;
            selectedTag = tag;
        }
        if (publishedVersion is null) return false;
        string checksum = selectedAsset.TryGetProperty("digest", out var digest)
            ? digest.GetString() ?? "" : "";
        long size = selectedAsset.GetProperty("size").GetInt64();
        string downloadUrl = selectedAsset.GetProperty("browser_download_url").GetString() ?? "";
        if (!System.Text.RegularExpressions.Regex.IsMatch(checksum,
                @"\Asha256:[a-f0-9]{64}\z",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
            size < 10_000_000 || size > 350_000_000 ||
            !Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? parsed) ||
            parsed.Scheme != Uri.UriSchemeHttps || parsed.Host != "github.com" ||
            !parsed.AbsolutePath.StartsWith(
                "/" + settings.Repository + "/releases/download/" + selectedTag + "/",
                StringComparison.Ordinal) ||
            !parsed.AbsolutePath.EndsWith("/TokyoWorldLauncher.exe", StringComparison.Ordinal))
            throw new InvalidDataException("Nieprawidłowe źródło lub SHA-256 aktualizacji.");

        string current = Environment.ProcessPath ?? throw new InvalidOperationException("Nie znam ścieżki EXE.");
        if (Version.TryParse(FileVersionInfo.GetVersionInfo(current).FileVersion, out var currentVersion) &&
            publishedVersion < currentVersion)
            return false;
        await using (var running = File.OpenRead(current))
        {
            string currentHash = Convert.ToHexString(await SHA256.HashDataAsync(running));
            if (currentHash.Equals(checksum[7..], StringComparison.OrdinalIgnoreCase))
            {
                settings.PendingUpdateHash = "";
                settings.Save();
                report.Report("Launcher jest aktualny.");
                return false;
            }
        }

        report.Report("Pobieram aktualizację Tokyo World " + publishedVersion + "...");
        string stageFolder = Path.Combine(Path.GetTempPath(), "TokyoWorldUpdater",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stageFolder);
        string next = Path.Combine(stageFolder, "TokyoWorldLauncher.exe");
        try
        {
            using var download = await http.GetAsync(downloadUrl,
                HttpCompletionOption.ResponseHeadersRead);
            download.EnsureSuccessStatusCode();
            if (download.Content.Headers.ContentLength is long contentLength &&
                contentLength != size)
                throw new InvalidDataException("Rozmiar aktualizacji nie zgadza się z wydaniem.");
            await using (var input = await download.Content.ReadAsStreamAsync())
            await using (var output = File.Create(next))
                await input.CopyToAsync(output);
            if (new FileInfo(next).Length != size)
                throw new InvalidDataException("Niekompletna aktualizacja EXE.");
            await using (var check = File.OpenRead(next))
            {
                string actual = Convert.ToHexString(await SHA256.HashDataAsync(check));
                if (!actual.Equals(checksum[7..], StringComparison.OrdinalIgnoreCase))
                    throw new CryptographicException("Suma SHA-256 aktualizacji jest niezgodna.");
            }
            using (var file = File.OpenRead(next))
            {
                if (file.ReadByte() != 'M' || file.ReadByte() != 'Z')
                    throw new InvalidDataException("Aktualizacja nie jest programem Windows.");
            }
            if (Version.TryParse(FileVersionInfo.GetVersionInfo(next).FileVersion, out var downloadedVersion) &&
                Version.TryParse(FileVersionInfo.GetVersionInfo(current).FileVersion, out var installedVersion) &&
                downloadedVersion < installedVersion)
                throw new InvalidDataException("Pobrana starsza wersja launchera została odrzucona.");
            settings.PendingUpdateHash = checksum[7..];
            settings.Save();

            // The running EXE cannot replace itself on Windows. A short-lived local
            // PowerShell helper waits until it exits, replaces it, and restarts it.
            string ps = Path.Combine(stageFolder, "apply-update.ps1");
            static string Quoted(string value) => "'" + value.Replace("'", "''") + "'";
            string script = "$ErrorActionPreference = 'Stop'\n" +
                "try { Wait-Process -Id " + Environment.ProcessId + " -ErrorAction SilentlyContinue } catch {}\n" +
                "$source = " + Quoted(next) + "\n" +
                "$target = " + Quoted(current) + "\n" +
                "for ($i = 0; $i -lt 35; $i++) {\n" +
                "  try { Copy-Item -LiteralPath $source -Destination $target -Force; Start-Process -FilePath $target; exit 0 }\n" +
                "  catch { Start-Sleep -Seconds 2 }\n" +
                "}\nexit 1\n";
            File.WriteAllText(ps, script, new UTF8Encoding(false));
            var psi = new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-NoLogo");
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-ExecutionPolicy");
            psi.ArgumentList.Add("Bypass");
            psi.ArgumentList.Add("-File");
            psi.ArgumentList.Add(ps);
            Process.Start(psi);
            report.Report("Uruchamiam nową wersję launchera...");
            return true;
        }
        catch
        {
            // Do not claim installation success or crash the launcher.
            throw;
        }
    }

    private static async Task<JsonDocument> GetJson(HttpClient http, string uri)
    {
        using var resp = await http.GetAsync(uri);
        resp.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await resp.Content.ReadAsStreamAsync());
    }
}
