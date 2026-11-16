using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TokyoWorldLauncher;

internal sealed class LauncherSettings
{
    public string InstallDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TokyoWorld");
    public string OfflineNickname { get; set; } = "TokyoPlayer";
    public bool UseMicrosoft { get; set; } = true;
    public string PremiumNickname { get; set; } = "";
    public bool FirstRunCompleted { get; set; } = false;
    // Friends can use a separate PUBLIC read-only distribution repository,
    // while the owner's original Tokyo-World source remains private.
    public const string PublicDistributionRepository = "onenightintokyo/Tokyo-World-Client";
    public const string PrivateSourceRepository = "onenightintokyo/Tokyo-World";
    public string Repository { get; set; } = PublicDistributionRepository;
    public bool InstallAllModules { get; set; } = true;
    public int RamMb { get; set; } = 10240;
    public bool OptimizeGraphics { get; set; } = true;
    public bool ExtraVisualMods { get; set; } = false;
    public bool EnableShaders { get; set; } = false;
    public long LastLauncherRunId { get; set; } = 0;
    public string PendingUpdateHash { get; set; } = "";
    public bool LightweightMode { get; set; } = false;
    public bool LightweightPromptShown { get; set; } = false;
    public bool PerfMigrationCompleted { get; set; } = false;
    public string GitHubTokenEncrypted { get; set; } = "";

    private static string SettingsFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TokyoWorldLauncher");
    private static string SettingsFile => Path.Combine(SettingsFolder, "launcher-settings.json");

    public static LauncherSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
                {
                    var loaded = JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(SettingsFile)) ?? new();
                    // Keep the owner's private source when their Windows account
                    // has a valid local token. Friends never need the owner's token;
                    // they use the separate public read-only modpack distribution.
                    if (string.IsNullOrWhiteSpace(loaded.GitHubTokenEncrypted) &&
                        string.Equals(loaded.Repository, PrivateSourceRepository,
                            StringComparison.OrdinalIgnoreCase))
                        loaded.Repository = PublicDistributionRepository;
                    if (!loaded.PerfMigrationCompleted)
                    {
                        // Existing 14+ GB allocations are scaled down only once.
                        if (loaded.RamMb > 12288) loaded.RamMb = 10240;
                        loaded.PerfMigrationCompleted = true;
                    }
                    return loaded;
                }
        }
        catch { /* An invalid preferences file must not block launch. */ }
        return new();
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsFolder);
        string temp = SettingsFile + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, SettingsFile, overwrite: true);
    }

    public string GetToken()
    {
        if (string.IsNullOrWhiteSpace(GitHubTokenEncrypted)) return "";
        try
        {
            byte[] bytes = Convert.FromBase64String(GitHubTokenEncrypted);
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser));
        }
        catch { return ""; }
    }

    public void SetToken(string plain)
    {
        GitHubTokenEncrypted = string.IsNullOrWhiteSpace(plain) ? "" :
            Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(plain.Trim()), null, DataProtectionScope.CurrentUser));
    }
}
