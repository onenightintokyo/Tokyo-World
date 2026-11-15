using System.Diagnostics;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Installer.Forge;

namespace TokyoWorldLauncher;

internal sealed class MinecraftEngine
{
    private const string McVersion = "1.20.1";
    private const string ForgeVersion = "47.4.28";
    private readonly JELoginHandler login = JELoginHandlerBuilder.BuildDefault();
    private MSession? premiumSession;
    private string? installedForgeVersion;

    public async Task<string> LoginMicrosoftAsync()
    {
        // Official browser/WebView2 flow. No passwords are requested by Tokyo World.
        premiumSession = await login.Authenticate();
        return premiumSession.Username;
    }

    public async Task<string> ChangeMicrosoftAccountAsync()
    {
        premiumSession = await login.AuthenticateInteractively();
        return premiumSession.Username;
    }

    public async Task PrepareGameAsync(string gameRoot, IProgress<string> log)
    {
        Directory.CreateDirectory(gameRoot);
        var launcher = new MinecraftLauncher(new MinecraftPath(gameRoot));
        launcher.FileProgressChanged += (_, e) =>
        {
            if (e.TotalTasks > 0 && e.ProgressedTasks % 25 == 0)
                log.Report($"Minecraft: {e.Name} ({e.ProgressedTasks}/{e.TotalTasks})");
        };

        log.Report("Minecraft: instaluję Minecraft 1.20.1 i Forge 47.4.28...");
        var forge = new ForgeInstaller(launcher);
        installedForgeVersion = await forge.Install(McVersion, ForgeVersion, new ForgeInstallOptions
        {
            InstallerOutput = new Progress<string>(message =>
            {
                if (!string.IsNullOrWhiteSpace(message)) log.Report("Forge: " + message);
            }),
            SkipIfAlreadyInstalled = true
        });

        log.Report("Minecraft: pobieram biblioteki, dźwięki, assety oraz Java 17...");
        await launcher.InstallAsync(installedForgeVersion);
        log.Report("Minecraft i Forge: instalacja zakończona.");
    }

    public async Task<Process> PlayAsync(string gameRoot, int ramMb,
        bool microsoft, string offlineNickname, IProgress<string> log)
    {
        if (installedForgeVersion == null)
            await PrepareGameAsync(gameRoot, log);
        MSession session;
        if (microsoft)
        {
            if (premiumSession == null)
            {
                log.Report("Microsoft: logowanie do Minecraft Java Edition...");
                await LoginMicrosoftAsync();
            }
            session = premiumSession ?? throw new InvalidOperationException("Nie udało się zalogować konta Microsoft.");
        }
        else
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(offlineNickname, @"\A[A-Za-z0-9_]{3,16}\z"))
                throw new InvalidOperationException("Nick offline musi mieć 3–16 znaków: A–Z, cyfry, _.");
            session = MSession.CreateOfflineSession(offlineNickname);
            log.Report("Offline: lokalny profil " + offlineNickname + ". Serwery premium wymagają konta Microsoft.");
        }

        var launcher = new MinecraftLauncher(new MinecraftPath(gameRoot));
        var command = await launcher.BuildProcessAsync(installedForgeVersion!, new MLaunchOption
        {
            Session = session,
            MinimumRamMb = 512,
            MaximumRamMb = ramMb,
            GameLauncherName = "TokyoWorldLauncher",
            GameLauncherVersion = "1.0"
        });
        if (!command.Start()) throw new InvalidOperationException("Nie udało się uruchomić procesu Minecraft.");
        log.Report("Minecraft uruchomiony. PID " + command.Id);
        return command;
    }
}
