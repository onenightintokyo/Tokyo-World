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
        string token = settings.GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            report.Report("Launcher działa bez tokenu. Aktualizacje EXE z prywatnego repo są niedostępne — do automatycznych aktualizacji znajomych potrzebne jest publiczne źródło wydań.");
            return false;
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(settings.Repository ?? "", @"\A[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+\z"))
            return false;

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(6) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TokyoWorldLauncher/1.6");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

        string api = "https://api.github.com/repos/" + settings.Repository;
        using var latest = await GetJson(http,
            api + "/actions/workflows/tokyo-launcher-standalone.yml/runs?branch=main&status=success&per_page=5");
        long runId = 0;
        foreach (var run in latest.RootElement.GetProperty("workflow_runs").EnumerateArray())
        {
            if (run.GetProperty("conclusion").GetString() != "success") continue;
            runId = run.GetProperty("id").GetInt64();
            break;
        }
        if (runId == 0) return false;
        string runningExe = Environment.ProcessPath ?? "";
        if (runId <= settings.LastLauncherRunId)
        {
            if (string.IsNullOrEmpty(settings.PendingUpdateHash) || !File.Exists(runningExe))
                return false;
            await using var running = File.OpenRead(runningExe);
            string runningHash = Convert.ToHexString(await SHA256.HashDataAsync(running));
            if (runningHash.Equals(settings.PendingUpdateHash, StringComparison.OrdinalIgnoreCase))
            {
                settings.PendingUpdateHash = "";
                settings.Save();
                return false;
            }
            report.Report("Ponawiam aktualizację, której wcześniej nie udało się zastosować.");
        }
        using var artifacts = await GetJson(http, api + "/actions/runs/" + runId + "/artifacts?per_page=30");
        JsonElement selected = default;
        foreach (var artifact in artifacts.RootElement.GetProperty("artifacts").EnumerateArray())
        {
            if (artifact.GetProperty("name").GetString() != "TokyoWorld-Standalone-Windows-x64") continue;
            if (artifact.GetProperty("expired").GetBoolean()) continue;
            selected = artifact.Clone();
            break;
        }
        if (selected.ValueKind != JsonValueKind.Object) return false;
        long id = selected.GetProperty("id").GetInt64();
        string checksum = selected.TryGetProperty("digest", out var digest)
            ? digest.GetString() ?? "" : "";
        if (!checksum.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ||
            checksum.Length != "sha256:".Length + 64)
        {
            report.Report("Aktualizacja EXE bez wiarygodnej sumy SHA-256 — pominięto.");
            return false;
        }

        report.Report("Znaleziono nowy launcher. Pobieram bezpieczną aktualizację...");
        string stageFolder = Path.Combine(Path.GetTempPath(), "TokyoWorldUpdater", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stageFolder);
        string zipPath = Path.Combine(stageFolder, "update.zip");
        try
        {
            using var download = await http.GetAsync(api + "/actions/artifacts/" + id + "/zip",
                HttpCompletionOption.ResponseHeadersRead);
            download.EnsureSuccessStatusCode();
            await using (var input = await download.Content.ReadAsStreamAsync())
            await using (var output = File.Create(zipPath))
                await input.CopyToAsync(output);
            await using (var verifyStream = File.OpenRead(zipPath))
            {
                string actual = Convert.ToHexString(await SHA256.HashDataAsync(verifyStream));
                if (!actual.Equals(checksum["sha256:".Length..], StringComparison.OrdinalIgnoreCase))
                    throw new CryptographicException("Suma SHA-256 archiwum aktualizacji jest nieprawidłowa.");
            }
            string next = Path.Combine(stageFolder, "TokyoWorldLauncher.exe");
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                var entries = archive.Entries.Where(e =>
                    e.FullName.Equals("TokyoWorldLauncher.exe", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (entries.Length != 1 || entries[0].Length < 10_000_000 || entries[0].Length > 350_000_000)
                    throw new InvalidDataException("Archiwum GitHub nie zawiera oczekiwanego programu EXE.");
                entries[0].ExtractToFile(next);
            }
            using (var file = File.OpenRead(next))
            {
                if (file.ReadByte() != 'M' || file.ReadByte() != 'Z')
                    throw new InvalidDataException("Pobrany plik nie jest wykonywalnym programem Windows.");
            }

            string current = Environment.ProcessPath ?? throw new InvalidOperationException("Nie znam ścieżki EXE.");
            var runningVersion = FileVersionInfo.GetVersionInfo(current).FileVersion;
            var downloadedVersion = FileVersionInfo.GetVersionInfo(next).FileVersion;
            if (Version.TryParse(runningVersion, out var currentNumber) &&
                Version.TryParse(downloadedVersion, out var downloadedNumber) &&
                downloadedNumber < currentNumber)
            {
                // Never downgrade to an older successful build from main.
                report.Report("Nowszy launcher jest już zainstalowany — pomijam starszy build.");
                settings.LastLauncherRunId = runId;
                settings.PendingUpdateHash = "";
                settings.Save();
                return false;
            }
            using var currentStream = File.OpenRead(current);
            using var newStream = File.OpenRead(next);
            string currentHash = Convert.ToHexString(await SHA256.HashDataAsync(currentStream));
            string newHash = Convert.ToHexString(await SHA256.HashDataAsync(newStream));
            settings.LastLauncherRunId = runId;
            settings.PendingUpdateHash = currentHash.Equals(newHash, StringComparison.OrdinalIgnoreCase)
                ? "" : newHash;
            settings.Save();
            if (currentHash.Equals(newHash, StringComparison.OrdinalIgnoreCase))
            {
                report.Report("Launcher jest już aktualny.");
                return false;
            }

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
