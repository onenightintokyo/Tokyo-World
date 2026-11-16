using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TokyoWorldLauncher;

/// <summary>
/// Reversible 1.20.1 client-side performance profile. Does not touch saves or quests.
/// </summary>
internal static class TokyoOptimization
{
    private static readonly Regex EveryCompatFile = new(
        @"\A(?:everycomp(?:at)?|every[-_.]?compat)[-_.]?.*\.jar\z",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static string StateFolder(string root) => Path.Combine(root, ".tokyo", "optimizer-v15");
    private static string DisabledFolder(string root) => Path.Combine(StateFolder(root), "disabled-every-compat");

    public static bool HasEveryCompat(string root)
    {
        string mods = Path.Combine(root, "mods");
        return Directory.Exists(mods) && Directory.EnumerateFiles(mods, "*.jar")
            .Any(path => EveryCompatFile.IsMatch(Path.GetFileName(path)));
    }

    public static void ApplyModPolicy(string root, bool lightweightMode, IProgress<string> log)
    {
        // Every Compat is retired from Tokyo World for ALL clients and modes.
        // Never auto-restore it when users disable lightweight mode.
        // Keep a rollback copy: old saves may contain blocks from this mod.
        string mods = Path.Combine(root, "mods");
        if (!Directory.Exists(mods)) return;
        foreach (string file in Directory.EnumerateFiles(mods, "*.jar")
            .Where(path => EveryCompatFile.IsMatch(Path.GetFileName(path))).ToArray())
        {
            string storage = Path.Combine(root, ".tokyo", "backups", "retired-every-compat");
            Directory.CreateDirectory(storage);
            string destination = Path.Combine(storage, Path.GetFileName(file));
            if (File.Exists(destination))
                destination = Path.Combine(storage, Guid.NewGuid().ToString("N") + "-" + Path.GetFileName(file));
            File.Move(file, destination);
            log.Report("Every Compat wyłączony dla całej paczki; kopia: " + destination);
        }
    }

    public static void ApplyGraphics(string root, bool enabled, IProgress<string> log)
    {
        if (!enabled) return;
        string options = Path.Combine(root, "options.txt");
        if (!File.Exists(options))
        {
            log.Report("Profil wydajności: ustawienia grafiki będą dostępne po pierwszym uruchomieniu Minecrafta.");
            return;
        }

        // Restrict only expensive settings; do not lower already lighter user preferences.
        var caps = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["renderDistance"] = 8,
            ["simulationDistance"] = 6,
            ["mipmapLevels"] = 2,
            ["entityDistanceScaling"] = 0.75,
            ["maxFps"] = 90
        };
        var lines = File.ReadAllLines(options).ToList();
        bool changed = false;
        foreach (var key in caps.Keys)
        {
            int index = lines.FindIndex(line => line.StartsWith(key + ":", StringComparison.Ordinal));
            if (index < 0) continue; // Keep Minecraft-generated defaults for absent keys.
            string value = lines[index][(key.Length + 1)..].Trim();
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double current))
                continue;
            double limit = caps[key];
            bool tooHigh = current > limit || (key == "maxFps" && current == 0);
            if (!tooHigh) continue;
            string replacement = key == "entityDistanceScaling"
                ? limit.ToString("0.##", CultureInfo.InvariantCulture)
                : ((int)limit).ToString(CultureInfo.InvariantCulture);
            lines[index] = key + ":" + replacement;
            changed = true;
        }
        if (!changed) return;

        string backupDir = StateFolder(root);
        Directory.CreateDirectory(backupDir);
        string backup = Path.Combine(backupDir, "options.original.txt");
        if (!File.Exists(backup))
            File.Copy(options, backup);
        string temporary = options + ".tw-optimizer-temp";
        try
        {
            File.WriteAllLines(temporary, lines, new UTF8Encoding(false));
            File.Move(temporary, options, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        log.Report("Profil wydajności: render ≤8, symulacja ≤6, mipmap ≤2, FPS ≤90. Zapisano kopię.");
    }

    public static void RestoreAll(string root, IProgress<string> log)
    {
        // Do not re-enable retired Every Compat via RestoreAll.
        string options = Path.Combine(root, "options.txt");
        string backup = Path.Combine(StateFolder(root), "options.original.txt");
        if (File.Exists(backup))
        {
            // User confirms explicitly in the Settings dialog because this replaces options.txt.
            File.Copy(backup, options, overwrite: true);
            File.Delete(backup);
            log.Report("Przywrócono ustawienia Minecrafta z kopii sprzed optymalizacji.");
        }
    }
}
