using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TokyoWorldLauncher;

/// <summary>
/// Synchronizes public game content from a SHA256 index served on GitHub's raw
/// CDN, pinned to a concrete git commit. No unauthenticated GitHub REST calls,
/// no token, and no hourly REST API quota even for many game files.
/// </summary>
internal sealed class GitHubSynchronizer
{
    private readonly string repo;
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(10) };

    private static readonly string[] Managed =
    {
        "config/", "defaultconfigs/", "kubejs/", "resourcepacks/", "shaderpacks/"
    };

    private static readonly Dictionary<string, string> Extras = new()
    {
        ["modpacks/tokyo_modules.json"] = ".tokyo/modules.json",
        ["modpacks/base_mod_hashes.txt"] = ".tokyo/base_mod_hashes.txt"
    };

    public GitHubSynchronizer(string token, string repository)
    {
        if (!Regex.IsMatch(repository ?? "", @"\A[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+\z"))
            throw new ArgumentException("Niepoprawna nazwa repozytorium.", nameof(repository));
        repo = repository;
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TokyoWorldIndependentLauncher/PublicIndex");
        // The auth argument is kept for binary compatibility with existing
        // call sites, but intentionally never transmitted for public files.
    }

    private static string? RelativeTarget(string repoPath)
    {
        if (Extras.TryGetValue(repoPath, out var mapped)) return mapped;
        return Managed.Any(prefix => repoPath.StartsWith(prefix, StringComparison.Ordinal))
            ? repoPath : null;
    }

    public async Task<int> SynchronizeAsync(string gameRoot, IProgress<string> progress,
        CancellationToken cancellation = default)
    {
        progress.Report("GitHub: sprawdzam publiczną listę aktualizacji paczki...");
        string indexUrl = $"https://raw.githubusercontent.com/{repo}/main/.github/launcher-updates/client-files.json";
        using var response = await http.GetAsync(indexUrl, HttpCompletionOption.ResponseHeadersRead, cancellation);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden
            or HttpStatusCode.Unauthorized)
            throw new InvalidOperationException(
                "Nie można odczytać aktualizacji z GitHuba: brak publicznego client-files.json. " +
                "Użyj aktualnego repozytorium Tokyo World lub sprawdź połączenie.");
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > 8_000_000)
            throw new InvalidDataException("Publiczny indeks paczki jest zbyt duży.");
        await using var indexStream = await response.Content.ReadAsStreamAsync(cancellation);
        using var index = await JsonDocument.ParseAsync(indexStream, cancellationToken: cancellation);
        var doc = index.RootElement;
        if (doc.GetProperty("schema").GetInt32() != 1)
            throw new InvalidDataException("Nieobsługiwana wersja indeksu paczki.");
        string commit = doc.GetProperty("commit").GetString() ?? "";
        if (!Regex.IsMatch(commit, @"\A[a-f0-9]{40}\z", RegexOptions.IgnoreCase))
            throw new InvalidDataException("Indeks paczki nie zawiera pełnego SHA commita.");

        string root = Path.GetFullPath(gameRoot);
        string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(root);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        int count = 0;
        int total = 0;
        foreach (var entry in doc.GetProperty("files").EnumerateArray())
        {
            cancellation.ThrowIfCancellationRequested();
            total++;
            if (total > 30_000)
                throw new InvalidDataException("Zbyt dużo plików w publicznym indeksie.");
            string name = entry.GetProperty("path").GetString() ?? "";
            string? relative = RelativeTarget(name);
            if (relative is null || name.Contains('\\') || name.Contains('\0') ||
                name.StartsWith("/", StringComparison.Ordinal) ||
                name.Split('/').Any(s => s is "." or ".." or "") ||
                !seen.Add(name))
                throw new InvalidDataException("Niedozwolona ścieżka w indeksie: " + name);

            string sha256 = entry.GetProperty("sha256").GetString() ?? "";
            long expectedSize = entry.GetProperty("size").GetInt64();
            if (!Regex.IsMatch(sha256, @"\A[a-f0-9]{64}\z", RegexOptions.IgnoreCase) ||
                expectedSize < 0 || expectedSize > 500_000_000)
                throw new InvalidDataException("Błędne SHA256 lub rozmiar: " + name);

            string destination = Path.GetFullPath(Path.Combine(root,
                relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!destination.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Ścieżka poza katalogiem gry: " + name);

            if (File.Exists(destination) &&
                new FileInfo(destination).Length == expectedSize &&
                CheckSha256(destination, sha256))
                continue;

            // Fetch from the manifest's exact immutable git commit, not from
            // main where files might change while an update is in progress.
            string encodedPath = string.Join('/',
                name.Split('/').Select(Uri.EscapeDataString));
            string url = $"https://raw.githubusercontent.com/{repo}/{commit}/{encodedPath}";
            progress.Report("Aktualizacja: " + name);
            using var fileResponse = await http.GetAsync(url,
                HttpCompletionOption.ResponseHeadersRead, cancellation);
            fileResponse.EnsureSuccessStatusCode();
            if (fileResponse.Content.Headers.ContentLength is long length &&
                length != expectedSize)
                throw new InvalidDataException("Zły rozmiar pliku paczki: " + name);

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            string temporary = destination + ".tw-download";
            try
            {
                await using (var input = await fileResponse.Content.ReadAsStreamAsync(cancellation))
                await using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                    await input.CopyToAsync(output, cancellation);

                if (new FileInfo(temporary).Length != expectedSize ||
                    !CheckSha256(temporary, sha256))
                    throw new CryptographicException("Pobrany plik nie zgadza się z SHA-256: " + name);

                if (File.Exists(destination))
                {
                    string backup = Path.Combine(root, ".tokyo", "backups",
                        DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"),
                        relative.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                    File.Copy(destination, backup, overwrite: true);
                }
                File.Move(temporary, destination, overwrite: true);
                count++;
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
        if (!seen.Contains("modpacks/tokyo_modules.json") ||
            !seen.Contains("modpacks/base_mod_hashes.txt"))
            throw new InvalidDataException("Publiczny indeks nie zawiera manifestu modów.");
        progress.Report($"GitHub: aktualizacja gotowa ({count}/{total} plików zmienionych).");
        return count;
    }

    private static bool CheckSha256(string path, string sha)
    {
        using var file = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(file)).Equals(sha,
            StringComparison.OrdinalIgnoreCase);
    }
}
