using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace TokyoWorldLauncher;

/// <summary>
/// Read Forge mod metadata from the actually installed JARs before starting Minecraft.
/// This detects duplicate mod IDs and missing mandatory client-side dependencies,
/// including cases that a Modrinth manifest cannot express accurately.
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

    public static void Verify(string gameRoot)
    {
        string modsDir = Path.Combine(gameRoot, "mods");
        var declared = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var requirements = new List<(string owner, string required)>();
        var errors = new List<string>();
        var files = Directory.EnumerateFiles(modsDir, "*.jar").ToArray();
        foreach (string jar in files)
        {
            try
            {
                using var zip = ZipFile.OpenRead(jar);
                var entry = zip.Entries.FirstOrDefault(x =>
                    x.FullName.Equals("META-INF/mods.toml", StringComparison.OrdinalIgnoreCase));
                if (entry is null) continue; // A library or non-Forge metadata container
                if (entry.Length > 1024 * 1024)
                {
                    errors.Add(Path.GetFileName(jar) + ": mods.toml is unexpectedly large");
                    continue;
                }
                using var reader = new StreamReader(entry.Open(), Encoding.UTF8, true);
                string toml = reader.ReadToEnd();

                foreach (Match block in ModSections.Matches(toml))
                {
                    var id = ModId.Match(block.Groups["body"].Value);
                    if (!id.Success) continue;
                    string key = id.Groups["id"].Value;
                    if (declared.TryGetValue(key, out string? previous) &&
                        !previous.Equals(Path.GetFileName(jar), StringComparison.OrdinalIgnoreCase))
                        errors.Add("Duplikat " + key + ": " + previous + " / " + Path.GetFileName(jar));
                    else
                        declared[key] = Path.GetFileName(jar);
                }
                foreach (Match block in DependencySections.Matches(toml))
                {
                    string body = block.Groups["body"].Value;
                    if (!Mandatory.IsMatch(body) || ServerSide.IsMatch(body)) continue;
                    Match id = ModId.Match(body);
                    if (!id.Success) continue;
                    requirements.Add((Path.GetFileName(jar), id.Groups["id"].Value));
                }
            }
            catch (Exception e) when (e is InvalidDataException or IOException)
            {
                errors.Add("Uszkodzony JAR: " + Path.GetFileName(jar) + " (" + e.Message + ")");
            }
        }
        var providedByForge = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "forge", "minecraft", "java"
        };
        foreach (var (owner, required) in requirements)
        {
            if (providedByForge.Contains(required) || declared.ContainsKey(required)) continue;
            errors.Add("Brak moda " + required + " (wymagany przez " + owner + ")");
        }
        string report = Path.Combine(gameRoot, ".tokyo", "mod-integrity.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(report)!);
        File.WriteAllLines(report,
            errors.Count == 0 ? new[] { $"OK: sprawdzono {files.Length} JAR" } : errors.Distinct());
        if (errors.Count > 0)
            throw new InvalidOperationException(
                "Niezgodna paczka Tokyo World. Wykryto błędy modów:\n" +
                string.Join("\n", errors.Distinct().Take(8)) +
                "\nPełna lista: .tokyo/mod-integrity.txt");
    }
}
