using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace TokyoWorldLauncher;

/// <summary>
/// Recover the exact, SHA1-locked 66 base JARs from locally owned game copies
/// or the original pack ZIP. The new launcher runs independently of Prism;
/// Prism is only ONE optional import source when already present.
/// </summary>
internal static class BaseModRecovery
{
    private sealed record Entry(string Name, string Sha1);

    private static Dictionary<string, Entry> ReadLock(string gameRoot)
    {
        var file = Path.Combine(gameRoot, ".tokyo", "base_mod_hashes.txt");
        if (!File.Exists(file))
            throw new InvalidOperationException("Brakuje indeksu bazowych modów. Najpierw pobierz aktualizację z GitHuba.");

        var result = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in File.ReadLines(file))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var fields = line.Split('|', 2);
            if (fields.Length != 2 ||
                !Regex.IsMatch(fields[0], @"\A[^\\/:*?""<>|]{1,200}\.jar\z", RegexOptions.IgnoreCase) ||
                !Regex.IsMatch(fields[1], @"\A[a-fA-F0-9]{40}\z"))
                throw new InvalidDataException("Nieprawidłowy plik indeksu modów: " + line);
            result.Add(fields[0], new Entry(fields[0], fields[1]));
        }
        return result;
    }

    private static bool Verified(string path, string sha1)
    {
        if (!File.Exists(path)) return false;
        using var f = File.OpenRead(path);
        return Convert.ToHexString(SHA1.HashData(f)).Equals(sha1, StringComparison.OrdinalIgnoreCase);
    }

    public static string[] MissingNames(string gameRoot)
    {
        var items = ReadLock(gameRoot);
        string mods = Path.Combine(gameRoot, "mods");
        return items.Values.Where(e => !Verified(Path.Combine(mods, e.Name), e.Sha1))
            .Select(e => e.Name).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static int CountMissing(string gameRoot) => MissingNames(gameRoot).Length;

    private static IEnumerable<string> ExistingModFolders(string gameRoot)
    {
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string[] candidates = {
            Path.Combine(roaming, "PrismLauncher", "instances", "Tokyo_World_Forge_1.20.1", "minecraft", "mods"),
            Path.Combine(roaming, "PrismLauncher", "instances", "Tokyo_World_Forge_1.20.1", ".minecraft", "mods"),
            Path.Combine(roaming, "MultiMC", "instances", "Tokyo_World_Forge_1.20.1", "minecraft", "mods"),
            Path.Combine(roaming, ".minecraft", "mods"),
        };
        string ours = Path.GetFullPath(Path.Combine(gameRoot, "mods"));
        foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            if (Directory.Exists(path) && !Path.GetFullPath(path).Equals(ours, StringComparison.OrdinalIgnoreCase))
                yield return path;
    }

    public static string? FindOriginalZip()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] directories = {
            Path.Combine(home, "Downloads"), Path.Combine(home, "Desktop"),
            Path.Combine(home, "Documents")
        };
        string[] names = { "Tokyo_World_Forge_1.20.1.zip", "Tokyo_World_Forge_1.20.1 (1).zip" };
        foreach (var dir in directories)
            foreach (var name in names)
            {
                var file = Path.Combine(dir, name);
                if (File.Exists(file)) return file;
            }
        return null;
    }

    private static void CopyVerified(Stream source, string destination, string expectedSha1)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (File.Exists(destination))
        {
            if (!Verified(destination, expectedSha1))
                throw new InvalidDataException("Plik już istnieje z inną sumą SHA-1: " + Path.GetFileName(destination));
            return;
        }
        string temp = destination + ".tokyo-base-import";
        try
        {
            if (File.Exists(temp)) File.Delete(temp);
            using (var target = new FileStream(temp, FileMode.CreateNew, FileAccess.Write))
                source.CopyTo(target);
            if (!Verified(temp, expectedSha1))
                throw new InvalidDataException("Plik ma inną sumę SHA-1: " + Path.GetFileName(destination));
            File.Move(temp, destination);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    public static int ImportFromLocalCopies(string gameRoot, IProgress<string> report)
    {
        var entries = ReadLock(gameRoot);
        string destination = Path.Combine(gameRoot, "mods");
        Directory.CreateDirectory(destination);
        int recovered = 0;

        foreach (string sourceDir in ExistingModFolders(gameRoot))
        {
            report.Report("Szukam bazowych modów w: " + sourceDir);
            foreach (var entry in entries.Values)
            {
                string dest = Path.Combine(destination, entry.Name);
                if (File.Exists(dest)) continue;
                string source = Path.Combine(sourceDir, entry.Name);
                if (!Verified(source, entry.Sha1)) continue;
                using var input = File.OpenRead(source);
                CopyVerified(input, dest, entry.Sha1);
                report.Report("Odzyskano mod: " + entry.Name);
                recovered++;
            }
        }
        return recovered;
    }

    public static int ImportFromZip(string gameRoot, string zipPath, IProgress<string> report)
    {
        var entries = ReadLock(gameRoot);
        string destination = Path.Combine(gameRoot, "mods");
        Directory.CreateDirectory(destination);
        int recovered = 0;
        using var zip = ZipFile.OpenRead(zipPath);
        foreach (var item in zip.Entries)
        {
            string path = item.FullName.Replace('\\', '/');
            if (!(path.StartsWith("mods/", StringComparison.OrdinalIgnoreCase) ||
                  path.Contains("/mods/", StringComparison.OrdinalIgnoreCase)) ||
                !path.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                continue;

            // Never use ZIP entry paths for extraction, only whitelisted filenames.
            string name = Path.GetFileName(path);
            if (!entries.TryGetValue(name, out var expected)) continue;
            string dest = Path.Combine(destination, expected.Name);
            if (File.Exists(dest)) continue;

            report.Report("Importuję oryginalny mod: " + name);
            try
            {
                using var input = item.Open();
                CopyVerified(input, dest, expected.Sha1);
                recovered++;
            }
            catch (InvalidDataException ex)
            {
                report.Report("Pominięto niezgodny plik: " + name + " — " + ex.Message);
            }
        }
        report.Report($"Odzyskano {recovered} modów z ZIP.");
        return recovered;
    }
}
