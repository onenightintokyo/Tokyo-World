using System.IO.Compression;
using System.Text.RegularExpressions;

namespace TokyoWorldLauncher;

internal static class PostInstallTweaks
{
    public static void Apply(string gameDir, IProgress<string> status)
    {
        // Reproduce the Tokyo World visual fixes that previously depended on
        // the Prism pre-launch PowerShell script. This launcher never invokes Prism.
        var zipFile = Path.Combine(gameDir, "resourcepacks", "TokyoWorld_Visuals.zip");
        if (File.Exists(zipFile))
        {
            using var zip = ZipFile.OpenRead(zipFile);
            const string prefix = "assets/tokyoworld/textures/gui/";
            foreach (var entry in zip.Entries)
            {
                string name = entry.FullName.Replace('\\', '/');
                if (!name.StartsWith(prefix, StringComparison.Ordinal) ||
                    !name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    continue;
                string target = Path.GetFullPath(Path.Combine(gameDir, "kubejs", name.Replace('/', Path.DirectorySeparatorChar)));
                string safeRoot = Path.GetFullPath(Path.Combine(gameDir, "kubejs")).TrimEnd(Path.DirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                if (!target.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Niebezpieczna ścieżka w paczce grafik: " + name);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                if (!File.Exists(target))
                {
                    entry.ExtractToFile(target);
                    status.Report("Grafiki FTB: " + name);
                }
            }
        }

        var options = Path.Combine(gameDir, "config", "fancymenu", "options.txt");
        if (File.Exists(options))
        {
            var before = File.ReadAllText(options);
            var after = Regex.Replace(before, @"(?m)^B:modpack_mode\s*=\s*'(true|false)';",
                "B:modpack_mode = 'true';");
            after = Regex.Replace(after, @"(?m)^B:show_customization_overlay\s*=\s*'(true|false)';",
                "B:show_customization_overlay = 'false';");
            if (before != after)
            {
                File.WriteAllText(options, after);
                status.Report("FancyMenu: pasek edycji ukryty (tryb modpacka).");
            }
        }

        // Minecraft options should not be overwritten or replaced on every update.
        Directory.CreateDirectory(Path.Combine(gameDir, "saves"));
        Directory.CreateDirectory(Path.Combine(gameDir, "screenshots"));
        Directory.CreateDirectory(Path.Combine(gameDir, "logs"));
    }
}
