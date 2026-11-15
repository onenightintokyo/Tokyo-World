using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace TokyoWorldLauncher;

/// <summary>Downloads opt-in shader ZIP through verified Modrinth metadata.</summary>
internal static class TokyoShaderInstaller
{
    public static async Task InstallAsync(string gameDir, IProgress<string> report)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(4) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TokyoWorldLauncher/1.6");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var listUri = "https://api.modrinth.com/v2/project/complementary-reimagined/version?game_versions=%5B%221.20.1%22%5D";
        using var res = await http.GetAsync(listUri);
        res.EnsureSuccessStatusCode();
        using var versions = JsonDocument.Parse(await res.Content.ReadAsStreamAsync());
        var selected = versions.RootElement.EnumerateArray()
            .Where(v => v.GetProperty("game_versions").EnumerateArray().Any(g => g.GetString() == "1.20.1"))
            .OrderByDescending(v => v.GetProperty("date_published").GetString()).FirstOrDefault();
        if (selected.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Nie znaleziono shaderów Complementary Reimagined dla Minecraft 1.20.1.");
        var file = selected.GetProperty("files").EnumerateArray().FirstOrDefault(f =>
            f.GetProperty("filename").GetString()?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true);
        if (file.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Wydanie shaderów nie zawiera ZIP.");
        string name = file.GetProperty("filename").GetString() ?? "";
        if (name != Path.GetFileName(name) || name.Length > 160 || !name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Nieprawidłowa nazwa shaderpacka.");
        string hash = file.GetProperty("hashes").GetProperty("sha512").GetString() ?? "";
        if (!System.Text.RegularExpressions.Regex.IsMatch(hash, @"\A[a-fA-F0-9]{128}\z"))
            throw new InvalidDataException("Brak sumy SHA-512 shaderpacka.");
        string rawUrl = file.GetProperty("url").GetString() ?? "";
        var uri = new Uri(rawUrl);
        if (uri.Scheme != Uri.UriSchemeHttps || uri.Host is not ("cdn.modrinth.com" or "cdn-raw.modrinth.com"))
            throw new InvalidDataException("Nieznany serwer shaderów.");
        string folder = Path.Combine(gameDir, "shaderpacks");
        Directory.CreateDirectory(folder);
        string final = Path.Combine(folder, name);
        if (File.Exists(final))
        {
            await using var existing = File.OpenRead(final);
            string digest = Convert.ToHexString(await SHA512.HashDataAsync(existing));
            if (digest.Equals(hash, StringComparison.OrdinalIgnoreCase)) return;
            throw new InvalidDataException("Istniejące shadery mają inną sumę. Nie nadpisuję pliku użytkownika.");
        }
        report.Report("Pobieram shadery Complementary Reimagined...");
        string temp = final + ".tw-download";
        try
        {
            using var content = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            content.EnsureSuccessStatusCode();
            await using (var from = await content.Content.ReadAsStreamAsync())
            await using (var to = File.Create(temp))
                await from.CopyToAsync(to);
            await using (var fileStream = File.OpenRead(temp))
            {
                string digest = Convert.ToHexString(await SHA512.HashDataAsync(fileStream));
                if (!digest.Equals(hash, StringComparison.OrdinalIgnoreCase))
                    throw new CryptographicException("Nieprawidłowa suma pobranych shaderów.");
            }
            File.Move(temp, final);
            report.Report("Shadery pobrane — aktywuj je w Opcjach grafiki > Shadery.");
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
