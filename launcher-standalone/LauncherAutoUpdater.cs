using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TokyoWorldLauncher;

internal static class LauncherAutoUpdater
{
    // Fetch a pinned, SHA256-verified release through a static, public update feed.
    // No user token, GitHub REST API calls, or hourly API limit are needed.
    public static async Task<bool> TryUpdateAsync(LauncherSettings settings, IProgress<string> report)
    {
        // Public immutable release feed, served as a static GitHub raw file.
        // This DOES NOT call api.github.com and cannot consume the unauthenticated
        // GitHub REST API quota (60 requests/hour per public IP).
        // Game file updates use a companion public client-files.json feed.
        if (!System.Text.RegularExpressions.Regex.IsMatch(settings.Repository ?? "",
                @"\A[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+\z"))
            throw new InvalidDataException("Nieprawidłowa nazwa repozytorium aktualizacji.");

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TokyoWorldLauncher/StaticUpdateFeed");
        string feedUrl = "https://raw.githubusercontent.com/" + settings.Repository +
            "/main/.github/launcher-updates/latest.json";
        using var feedResponse = await http.GetAsync(feedUrl, HttpCompletionOption.ResponseHeadersRead);
        if (feedResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new InvalidOperationException(
                "Brak publicznego pliku aktualizacji: .github/launcher-updates/latest.json.");
        if (!feedResponse.IsSuccessStatusCode)
            throw new HttpRequestException(
                "Źródło aktualizacji nie odpowiada (" + (int)feedResponse.StatusCode +
                "). Spróbuj ponownie później.");
        if (feedResponse.Content.Headers.ContentLength is > 64_000)
            throw new InvalidDataException("Zbyt duży manifest aktualizacji.");
        await using var feedStream = await feedResponse.Content.ReadAsStreamAsync();
        using var feed = await JsonDocument.ParseAsync(feedStream);
        var metadata = feed.RootElement;
        if (!metadata.TryGetProperty("schema", out var schema) || schema.GetInt32() != 1)
            throw new InvalidDataException("Nieobsługiwana wersja manifestu aktualizacji.");
        string versionText = metadata.GetProperty("version").GetString() ?? "";
        if (!System.Text.RegularExpressions.Regex.IsMatch(versionText,
                @"\A[0-9]+\.[0-9]+\.[0-9]+\z") ||
            !Version.TryParse(versionText + ".0", out Version? publishedVersion))
            throw new InvalidDataException("Błędny numer najnowszej wersji.");
        string selectedTag = "tokyo-world-launcher-v" + versionText;
        string checksum = "sha256:" + (metadata.GetProperty("sha256").GetString() ?? "");
        long size = metadata.GetProperty("size").GetInt64();
        string downloadUrl = metadata.GetProperty("download_url").GetString() ?? "";
        if (!System.Text.RegularExpressions.Regex.IsMatch(checksum,
                @"\Asha256:[a-f0-9]{64}\z",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
            size < 10_000_000 || size > 350_000_000 ||
            !Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? parsed) ||
            parsed.Scheme != Uri.UriSchemeHttps || parsed.Host != "github.com" ||
            !parsed.AbsolutePath.Equals(
                "/" + settings.Repository + "/releases/download/" + selectedTag +
                "/TokyoWorldLauncher.exe", StringComparison.OrdinalIgnoreCase))
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
