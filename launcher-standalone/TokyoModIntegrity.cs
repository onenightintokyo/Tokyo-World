using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace TokyoWorldLauncher;

/// <summary>
/// Checks actual Forge mod metadata, including Forge JarJar embedded libraries.
/// A raw scan of only top-level mods/*.jar reports false missing dependencies
/// (Create's Flywheel/Ponder, PuzzlesLib's PuzzlesAccessApi and bundled libraries).
/// </summary>
internal static class TokyoModIntegrity
{
    private static readonly Regex ModSections = new(
        @"(?ims)^\s*\[\[mods\]\]\s*(?<body>.*?)(?=^\s*\[\[|\z)");
    private static readonly Regex DependencySections = new(
        @"(?ims)^\s*\[\[dependencies\.[^\]]+\]\]\s*(?<body>.*?)(?=^\s*\[\[|\z)");
    private static readonly Regex ModId = new(
        @"(?im)^\s*modId\s*=\s*[""'](?<id>[a-zA-Z0-9_.-]+)[""']");
    private static readonly Regex Mandatory = new(
        @"(?im)^\s*mandatory\s*=\s*true\b");
    private static readonly Regex ServerSide = new(
        @"(?im)^\s*side\s*=\s*[""']SERVER[""']");
    private static readonly Regex RetiredEveryCompat = new(
        @"(?i)^every[-_.]?(?:compat|comp)[-_.]?.*\.jar$");

    private const long MaximumMetadataLength = 1024 * 1024;
    private const long MaximumEmbeddedJarLength = 70L * 1024 * 1024;
    private const int MaximumEmbeddedCount = 400;
    private const int MaximumDepth = 3;

    public static void Verify(string gameRoot)
    {
        string modsDir = Path.Combine(gameRoot, "mods");
        var declared = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var requirements = new List<(string owner, string required)>();
        var errors = new List<string>();
        var visitedEmbedded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int embeddedCount = 0;
        var files = Directory.EnumerateFiles(modsDir, "*.jar").ToArray();

        foreach (string file in files)
        {
            string filename = Path.GetFileName(file);
            if (RetiredEveryCompat.IsMatch(filename))
                errors.Add("Wycofany Every Compat w aktywnych modach: " + filename);
            try
            {
                using var zip = ZipFile.OpenRead(file);
                ReadJar(zip, filename, 0);
            }
            catch (Exception e) when (e is InvalidDataException or IOException)
            {
                errors.Add("Uszkodzony JAR: " + filename + " (" + e.Message + ")");
            }
        }

        // Built-in ids are supplied by the game and mod loader, not mods/*.jar.
        var builtin = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "forge", "minecraft", "java"
        };
        foreach (var (owner, required) in requirements.Distinct())
        {
            if (builtin.Contains(required) || declared.ContainsKey(required)) continue;
            errors.Add("Brak moda " + required + " (wymagany przez " + owner + ")");
        }

        string report = Path.Combine(gameRoot, ".tokyo", "mod-integrity.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(report)!);
        var reportLines = new List<string>
        {
            $"Sprawdzono {files.Length} modów JAR i {embeddedCount} bibliotek Forge JarJar.",
            $"Znaleziono {declared.Count} identyfikatorów modów.",
            $"Błędy: {errors.Distinct().Count()}"
        };
        reportLines.AddRange(errors.Distinct());
        File.WriteAllLines(report, reportLines);
        if (errors.Count > 0)
            throw new InvalidOperationException(
                "Niezgodna paczka Tokyo World. Wykryto błędy modów:\n" +
                string.Join("\n", errors.Distinct().Take(8)) +
                "\nPełna lista: .tokyo/mod-integrity.txt");

        void ReadJar(ZipArchive archive, string origin, int depth)
        {
            var metadata = archive.Entries.FirstOrDefault(x =>
                x.FullName.Equals("META-INF/mods.toml", StringComparison.OrdinalIgnoreCase));
            if (metadata != null)
            {
                if (metadata.Length < 0 || metadata.Length > MaximumMetadataLength)
                {
                    errors.Add("Za duże mods.toml: " + origin);
                }
                else
                {
                    using var reader = new StreamReader(metadata.Open(), Encoding.UTF8, true);
                    string toml = reader.ReadToEnd();
                    foreach (Match section in ModSections.Matches(toml))
                    {
                        Match id = ModId.Match(section.Groups["body"].Value);
                        if (!id.Success) continue;
                        string key = id.Groups["id"].Value;
                        if (declared.TryGetValue(key, out string? previous) &&
                            !previous.Equals(origin, StringComparison.OrdinalIgnoreCase))
                            errors.Add("Duplikat " + key + ": " + previous + " / " + origin);
                        else
                            declared[key] = origin;
                    }
                    foreach (Match section in DependencySections.Matches(toml))
                    {
                        string body = section.Groups["body"].Value;
                        if (!Mandatory.IsMatch(body) || ServerSide.IsMatch(body)) continue;
                        Match id = ModId.Match(body);
                        if (id.Success) requirements.Add((origin, id.Groups["id"].Value));
                    }
                }
            }

            if (depth >= MaximumDepth) return;
            // Forge's JarJar loader loads these as mods even though they do not
            // appear as separate files in the top-level mods folder.
            foreach (var entry in archive.Entries)
            {
                // Forge plugins may bundle dependencies under either path:
                // Create uses META-INF/jarjar (Flywheel/Ponder), while Create:
                // New Age bundles ESL under META-INF/jars.
                bool forgeJarJar = entry.FullName.StartsWith("META-INF/jarjar/", StringComparison.OrdinalIgnoreCase);
                bool forgeNested = entry.FullName.StartsWith("META-INF/jars/", StringComparison.OrdinalIgnoreCase);
                if ((!forgeJarJar && !forgeNested) ||
                    !entry.FullName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) continue;
                if (entry.Length <= 0 || entry.Length > MaximumEmbeddedJarLength)
                {
                    errors.Add("Nieprawidłowa biblioteka JarJar: " + origin + "!/" + entry.FullName);
                    continue;
                }
                string nestedOrigin = origin + "!/" + entry.FullName;
                if (!visitedEmbedded.Add(nestedOrigin)) continue;
                if (++embeddedCount > MaximumEmbeddedCount)
                {
                    errors.Add("Zbyt dużo dołączonych bibliotek Forge JarJar.");
                    return;
                }
                try
                {
                    using var input = entry.Open();
                    using var buffer = new MemoryStream();
                    input.CopyTo(buffer);
                    buffer.Position = 0;
                    using var nested = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: false);
                    ReadJar(nested, nestedOrigin, depth + 1);
                }
                catch (Exception e) when (e is InvalidDataException or IOException)
                {
                    errors.Add("Nie można odczytać JarJar: " + nestedOrigin + ": " + e.Message);
                }
            }
        }
    }
}
