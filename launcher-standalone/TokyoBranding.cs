using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace TokyoWorldLauncher;

/// <summary>
/// Branded TW icon is compiled into the EXE and applied both to the Windows
/// launcher window and the separate Minecraft/Forge taskbar window.
/// No modification of Mojang/Forge JARs or Java internals is required.
/// </summary>
internal static class TokyoBranding
{
    private const string IconResource = "TokyoWorldLauncher.Icon";
    private const string LogoResource = "TokyoWorldLauncher.IconPng";
    private const uint WmSetIcon = 0x0080;
    private const uint IconSmall = 0;
    private const uint IconBig = 1;
    private const uint IconSmall2 = 2;
    private const uint SmtoAbortIfHung = 0x0002;

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hwnd, StringBuilder title, int capacity);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint msg, UIntPtr wParam,
        IntPtr lParam, uint flags, uint timeout, out IntPtr result);

    public static Icon LoadIcon()
    {
        var stream = typeof(TokyoBranding).Assembly.GetManifestResourceStream(IconResource)
            ?? throw new InvalidOperationException("Brakuje logo TW w zasobach launchera.");
        using (stream)
        using (var ico = new Icon(stream))
            return (Icon)ico.Clone(); // independent of the resource stream
    }

    public static Bitmap LoadLogo()
    {
        var stream = typeof(TokyoBranding).Assembly.GetManifestResourceStream(LogoResource)
            ?? throw new InvalidOperationException("Brakuje grafiki TW w zasobach launchera.");
        using (stream)
        using (var bitmap = new Bitmap(stream))
            return new Bitmap(bitmap);
    }

    public static void CopyLogoToGame(string gameRoot, IProgress<string> report)
    {
        using var png = typeof(TokyoBranding).Assembly.GetManifestResourceStream(LogoResource)
            ?? throw new InvalidOperationException("Brakuje grafiki TW w zasobach launchera.");
        using var buffer = new MemoryStream();
        png.CopyTo(buffer);
        byte[] source = buffer.ToArray();

        // Make TW available to FancyMenu / KubeJS assets for custom in-game GUI.
        string[] destinations = {
            Path.Combine(gameRoot, "config", "fancymenu", "assets", "tokyo_world_tw_logo.png"),
            Path.Combine(gameRoot, "kubejs", "assets", "tokyoworld", "textures", "gui", "tw_logo.png")
        };
        foreach (var dest in destinations)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            if (File.Exists(dest) && File.ReadAllBytes(dest).AsSpan().SequenceEqual(source))
                continue;
            File.WriteAllBytes(dest, source);
            report.Report("Tokyo World: zainstalowano logo TW: " + Path.GetFileName(dest));
        }
    }

    public static void WatchMinecraftWindow(Process game, IProgress<string> report)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var icon = LoadIcon();
                var branded = new HashSet<nint>();
                // Minecraft/Forge/GLFW may recreate the window, or set its own
                // icon after initialization, so reapply for the game's lifetime.
                while (!game.HasExited)
                {
                    foreach (var hwnd in FindWindows((uint)game.Id))
                    {
                        ApplyIcon(hwnd, icon.Handle);
                        if (branded.Add(hwnd))
                            report.Report("Tokyo World: logo TW ustawione na oknie Minecraft.");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            }
            catch (Exception ex)
            {
                // OS taskbar icon is cosmetic; never crash the actual game.
                report.Report("Tokyo World: nie można ustawić ikonki gry: " + ex.Message);
            }
        });
    }

    private static IReadOnlyList<IntPtr> FindWindows(uint processId)
    {
        var found = new List<IntPtr>();
        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out uint owner);
            if (owner != processId || !IsWindowVisible(hwnd)) return true;
            var title = new StringBuilder(300);
            if (GetWindowText(hwnd, title, title.Capacity) <= 0) return true;
            var text = title.ToString();
            if (text.Contains("Minecraft", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Forge", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Tokyo World", StringComparison.OrdinalIgnoreCase))
                found.Add(hwnd);
            return true;
        }, IntPtr.Zero);
        return found;
    }

    private static void ApplyIcon(IntPtr hwnd, IntPtr icon)
    {
        _ = SendMessageTimeout(hwnd, WmSetIcon, (UIntPtr)IconBig,
            icon, SmtoAbortIfHung, 150, out _);
        _ = SendMessageTimeout(hwnd, WmSetIcon, (UIntPtr)IconSmall,
            icon, SmtoAbortIfHung, 150, out _);
        _ = SendMessageTimeout(hwnd, WmSetIcon, (UIntPtr)IconSmall2,
            icon, SmtoAbortIfHung, 150, out _);
    }
}
