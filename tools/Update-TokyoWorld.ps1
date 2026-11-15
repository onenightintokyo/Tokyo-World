# Tokyo World config/assets synchronizer for Prism Launcher (Windows).
# Syncs only authored pack files from this private Git repository.
# Does NOT download/update mods, delete files, or touch world saves.
param(
    [string]$GameDir = $env:INST_MC_DIR
)

$ErrorActionPreference = 'Stop'
$RepoDir = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    Write-Error "INST_MC_DIR is missing. Run with -GameDir 'C:\path\to\instance\minecraft'."
    exit 1
}
if (!(Test-Path -LiteralPath $GameDir -PathType Container)) {
    Write-Error "Game directory not found: $GameDir"
    exit 1
}
if (!(Test-Path -LiteralPath (Join-Path $RepoDir '.git'))) {
    Write-Error "This script must be run from a Git clone of Tokyo-World: $RepoDir"
    exit 1
}

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) {
    Write-Error "Git is not installed or not in PATH. Install Git for Windows: https://git-scm.com/downloads/win"
    exit 1
}

Write-Host "[Tokyo World] Checking GitHub for updates..."
& $git.Source -C $RepoDir pull --ff-only origin main
if ($LASTEXITCODE -ne 0) {
    Write-Warning "[Tokyo World] Git update failed (offline, auth, or local changes). Using last local files."
}

$ManagedFolders = @('config', 'defaultconfigs', 'kubejs', 'resourcepacks', 'shaderpacks')
$changed = 0
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot = Join-Path $GameDir (".tokyo-world-backups\" + $stamp)

foreach ($folder in $ManagedFolders) {
    $srcRoot = Join-Path $RepoDir $folder
    if (!(Test-Path -LiteralPath $srcRoot -PathType Container)) { continue }

    foreach ($file in (Get-ChildItem -LiteralPath $srcRoot -File -Recurse)) {
        $relative = $file.FullName.Substring($RepoDir.Length).TrimStart([char[]]@('\', '/'))
        $dest = Join-Path $GameDir $relative
        $destFolder = Split-Path -Parent $dest

        if (!(Test-Path -LiteralPath $destFolder)) {
            New-Item -ItemType Directory -Path $destFolder -Force | Out-Null
        }

        if (Test-Path -LiteralPath $dest -PathType Leaf) {
            $oldHash = (Get-FileHash -LiteralPath $dest -Algorithm SHA256).Hash
            $newHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
            if ($oldHash -eq $newHash) { continue }

            $backup = Join-Path $backupRoot $relative
            $backupFolder = Split-Path -Parent $backup
            if (!(Test-Path -LiteralPath $backupFolder)) {
                New-Item -ItemType Directory -Path $backupFolder -Force | Out-Null
            }
            Copy-Item -LiteralPath $dest -Destination $backup -Force
        }

        Copy-Item -LiteralPath $file.FullName -Destination $dest -Force
        $changed++
        Write-Host "[Tokyo World] Updated: $relative"
    }
}


# Optional visual setup for existing Tokyo World installations. Keep edits surgical,
# back up each changed file and never block Minecraft startup if a layout differs.
try {
    $layout = Join-Path $GameDir 'config\fancymenu\customization\title_screen_layout.txt'
    $menuImage = Join-Path $GameDir 'config\fancymenu\assets\tokyo_world_menu_background.png'
    if ((Test-Path -LiteralPath $layout -PathType Leaf) -and (Test-Path -LiteralPath $menuImage -PathType Leaf)) {
        $before = [System.IO.File]::ReadAllText($layout)
        $needle = '(?ms)(menu_background\s*\{\s*instance_identifier\s*=\s*[^\r\n]+\r?\n\s*background_type\s*=\s*image\s*\r?\n\s*show_background\s*=\s*)false(\s*\r?\n\s*image_path\s*=\s*\[source:local\]/config/fancymenu/assets/tokyo_world_menu_background\.png)'
        $after = [System.Text.RegularExpressions.Regex]::Replace($before, $needle, '1true2')
        if ($after -cne $before) {
            $relative = 'config\fancymenu\customization\title_screen_layout.txt'
            $backup = Join-Path $backupRoot $relative
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backup) | Out-Null
            Copy-Item -LiteralPath $layout -Destination $backup -Force
            [System.IO.File]::WriteAllText($layout, $after, (New-Object System.Text.UTF8Encoding($false)))
            Write-Host '[Tokyo World] FancyMenu: activated Tokyo World background.'
        }
    }

    $pack = Join-Path $GameDir 'resourcepacks\TokyoWorld_Visuals.zip'
    $options = Join-Path $GameDir 'options.txt'
    if ((Test-Path -LiteralPath $pack -PathType Leaf) -and (Test-Path -LiteralPath $options -PathType Leaf)) {
        $before = [System.IO.File]::ReadAllText($options)
        $pattern = '(?m)^resourcePacks:(\[[^\r\n]*\])'
        $match = [System.Text.RegularExpressions.Regex]::Match($before, $pattern)
        if ($match.Success) {
            $names = @($match.Groups[1].Value | ConvertFrom-Json)
            $wanted = 'file/TokyoWorld_Visuals.zip'
            if ($names -notcontains $wanted) {
                $names = @($names) + @($wanted)
                $serialized = ConvertTo-Json -InputObject $names -Compress
                $after = $before.Substring(0, $match.Index) + 'resourcePacks:' + $serialized + $before.Substring($match.Index + $match.Length)
                $backup = Join-Path $backupRoot 'options.txt'
                New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backup) | Out-Null
                Copy-Item -LiteralPath $options -Destination $backup -Force
                [System.IO.File]::WriteAllText($options, $after, (New-Object System.Text.UTF8Encoding($false)))
                Write-Host '[Tokyo World] Enabled TokyoWorld_Visuals resource pack.'
            }
        }
    }
}
catch {
    Write-Warning ('[Tokyo World] Optional visual setup skipped: ' + $_.Exception.Message)
}

Write-Host "[Tokyo World] Done. Files copied: $changed"
Write-Host "[Tokyo World] Mods are NOT updated by this script. Packwiz will handle that later."
exit 0
