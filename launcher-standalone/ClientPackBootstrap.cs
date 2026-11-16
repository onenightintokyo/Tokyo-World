using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace TokyoWorldLauncher;

/// <summary>
/// Token-free, curated game-content snapshot bundled in the launcher EXE.
/// It contains only public/shareable configs, FTB Quests, KubeJS, resourcepacks,
/// shaderpacks and the mod version manifest. No token or private source code.
/// Fresh installations can use it without a private GitHub account.
/// Live updates still require a public distribution repository or per-user auth.
/// </summary>
internal static class ClientPackBootstrap
{
    private const string Resource = "TokyoWorldClientSnapshot";
    private const long MaxArchiveEntryBytes = 32L * 1024 * 1024;
    private const long MaxTotalBytes = 256L * 1024 * 1024;

    private static readonly string[] AllowedRoots =
    {
        "config/", "defaultconfigs/", "kubejs/", "resourcepacks/", "shaderpacks/"
    };

    private static string? ResolveTarget(string name)
    {
        if (name == "modpacks/tokyo_modules.json")
            return ".tokyo/modules.json";
        if (name == "modpacks/base_mod_hashes.txt")
            return ".tokyo/base_mod_hashes.txt";
        return AllowedRoots.Any(x => name.StartsWith(x, StringComparison.Ordinal)) ? name : null;
    }

    public static int InstallMissing(string gameRoot, IProgress<string> progress)
    {
        using var source = Assembly.GetExecutingAssembly().GetManifestResourceStream(Resource);
        if (source == null)
            throw new InvalidOperationException(
                "Ten EXE nie zawiera wbudowanych plików Tokyo World. Pobierz pełny launcher 1.8.1.");

        using var zip = new ZipArchive(source, ZipArchiveMode.Read);
        string root = Path.GetFullPath(gameRoot).TrimEnd(Path.DirectorySeparatorChar);
        string rootPrefix = root + Path.DirectorySeparatorChar;
        long total = 0;
        int files = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in zip.Entries)
        {
            string name = entry.FullName.Replace('\\', '/');
            if (name.EndsWith("/", StringComparison.Ordinal)) continue;
            if (name.Contains('\0') || name.StartsWith("/", StringComparison.Ordinal)
                || name.Split('/').Any(x => x == "." || x == ".." || x.Length == 0))
                throw new InvalidDataException("Niebezpieczna ścieżka w paczce Tokyo World.");
            string? relative = ResolveTarget(name);
            if (relative == null)
                throw new InvalidDataException("Paczka klienta zawiera niedozwolony plik: " + name);
            if (!seen.Add(relative))
                throw new InvalidDataException("Duplikat pliku w paczce klienta: " + relative);
            if (entry.Length < 0 || entry.Length > MaxArchiveEntryBytes)
                throw new InvalidDataException("Nieprawidłowy rozmiar pliku: " + name);
            total += entry.Length;
            if (total > MaxTotalBytes)
                throw new InvalidDataException("Przekroczony limit rozmiaru paczki klienta.");

            string dest = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!dest.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Ścieżka wykracza poza folder gry: " + name);
            if (File.Exists(dest)) continue; // Never overwrite player settings/worlds.

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            string temp = dest + ".tokyo-bootstrap";
            try
            {
                using (var input = entry.Open())
                using (var output = new FileStream(temp, FileMode.Create, FileAccess.Write))
                    input.CopyTo(output);
                File.Move(temp, dest, overwrite: false);
                files++;
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        // The essential manifests must be present even on a clean Windows user.
        foreach (var required in new[] { ".tokyo/modules.json", ".tokyo/base_mod_hashes.txt" })
            if (!File.Exists(Path.Combine(root, required.Replace('/', Path.DirectorySeparatorChar))))
                throw new InvalidDataException("Niepełny launcher: brak " + required);

        progress.Report("Tokyo World: wbudowana paczka klienta gotowa (" + files
            + " nowych plików), bez prywatnego tokenu.");
        return files;
    }
}
