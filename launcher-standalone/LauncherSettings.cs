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
    public string Repository { get; set; } = "onenightintokyo/Tokyo-World";
    public bool InstallAllModules { get; set; } = true;
    public int RamMb { get; set; } = 4096;
    public string GitHubTokenEncrypted { get; set; } = "";

    private static string SettingsFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TokyoWorldLauncher");
    private static string SettingsFile => Path.Combine(SettingsFolder, "launcher-settings.json");

    public static LauncherSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
                return JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(SettingsFile)) ?? new();
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
