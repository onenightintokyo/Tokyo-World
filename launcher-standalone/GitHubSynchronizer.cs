using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TokyoWorldLauncher;

internal sealed class GitHubSynchronizer
{
    private readonly string repo;
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(3) };

    private static readonly string[] Managed = {
        "config/", "defaultconfigs/", "kubejs/", "resourcepacks/", "shaderpacks/"
    };
    private static readonly Dictionary<string, string> Extras = new() {
        ["modpacks/tokyo_modules.json"] = ".tokyo/modules.json",
        ["modpacks/base_mod_hashes.txt"] = ".tokyo/base_mod_hashes.txt"
    };

    public GitHubSynchronizer(string token, string repository)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(repository ?? "", @"\A[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+\z"))
            throw new ArgumentException("Podaj repozytorium w formacie uzytkownik/nazwa.", nameof(repository));
        repo = repository;
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TokyoWorldIndependentLauncher/1.1");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        if (!string.IsNullOrWhiteSpace(token))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
    }

    private static string? RelativeTarget(string repoPath)
    {
        if (Extras.TryGetValue(repoPath, out var mapping)) return mapping;
        return Managed.Any(prefix => repoPath.StartsWith(prefix, StringComparison.Ordinal)) ? repoPath : null;
    }

    public async Task<int> SynchronizeAsync(string gameRoot, IProgress<string> progress, CancellationToken cancellation = default)
    {
        progress.Report("GitHub: sprawdzam najnowszy stan paczki...");
        using var response = await http.GetAsync(
            $"https://api.github.com/repos/{repo}/git/trees/main?recursive=1", cancellation);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new InvalidOperationException("Nie można odczytać aktualizacji z GitHuba. Ustaw publiczne repozytorium dystrybucyjne lub podaj własny token z uprawnieniem Contents: Read w ustawieniach (⚙). Nie umieszczaj tokenu w EXE.");
        response.EnsureSuccessStatusCode();
        using var tree = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellation));
        if (tree.RootElement.TryGetProperty("truncated", out var trunc) && trunc.GetBoolean())
            throw new InvalidOperationException("GitHub zwrócił niepełną listę plików. Aktualizację wstrzymano.");

        Directory.CreateDirectory(gameRoot);
        var root = Path.GetFullPath(gameRoot);
        var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        int count = 0;
        foreach (var entry in tree.RootElement.GetProperty("tree").EnumerateArray())
        {
            cancellation.ThrowIfCancellationRequested();
            if (!string.Equals(entry.GetProperty("type").GetString(), "blob", StringComparison.Ordinal))
                continue;
            string source = entry.GetProperty("path").GetString() ?? "";
            string? relative = RelativeTarget(source);
            if (relative == null) continue;
            string sha = entry.GetProperty("sha").GetString() ?? "";
            if (sha.Length != 40) throw new InvalidDataException($"Błędny SHA dla {source}");

            // Never allow remote paths to reach outside the selected game directory.
            string destination = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!destination.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Niebezpieczna ścieżka w repo: {source}");
            if (File.Exists(destination) && CalculateGitBlobSha(destination).Equals(sha, StringComparison.OrdinalIgnoreCase))
                continue;

            progress.Report($"Aktualizacja: {source}");
            byte[] data = await DownloadBlobAsync(sha, cancellation);
            if (!CalculateGitBlobSha(data).Equals(sha, StringComparison.OrdinalIgnoreCase))
                throw new CryptographicException($"Niezgodny SHA-1 GitHub dla {source}");

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (File.Exists(destination))
            {
                string backup = Path.Combine(root, ".tokyo", "backups",
                    DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"), relative.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                File.Copy(destination, backup, overwrite: true);
            }
            string temporary = destination + ".tw-download";
            await File.WriteAllBytesAsync(temporary, data, cancellation);
            File.Move(temporary, destination, overwrite: true);
            count++;
        }
        progress.Report($"GitHub: aktualizacja gotowa ({count} nowych/zmienionych plików).");
        return count;
    }

    private async Task<byte[]> DownloadBlobAsync(string sha, CancellationToken cancellation)
    {
        using var resp = await http.GetAsync(
            $"https://api.github.com/repos/{repo}/git/blobs/{sha}", cancellation);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStreamAsync(cancellation));
        string encoding = doc.RootElement.GetProperty("encoding").GetString() ?? "";
        if (encoding != "base64") throw new InvalidDataException("GitHub zwrócił nieobsługiwany sposób kodowania pliku.");
        return Convert.FromBase64String(doc.RootElement.GetProperty("content").GetString() ?? "");
    }

    private static string CalculateGitBlobSha(string path)
    {
        using var stream = File.OpenRead(path);
        return CalculateGitBlobSha(stream);
    }

    private static string CalculateGitBlobSha(byte[] data) => CalculateGitBlobSha(new MemoryStream(data));

    private static string CalculateGitBlobSha(Stream stream)
    {
        using var sha = SHA1.Create();
        byte[] header = Encoding.ASCII.GetBytes($"blob {stream.Length}\0");
        sha.TransformBlock(header, 0, header.Length, header, 0);
        byte[] buffer = new byte[128 * 1024];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
            sha.TransformBlock(buffer, 0, read, buffer, 0);
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash!).ToLowerInvariant();
    }
}
