# Tokyo World - safe private-GitHub updater for Prism Launcher (Windows)
# Updates authored config/assets/quest files only. Does not edit saves or RAM.
param([string]$GameDir = $env:INST_MC_DIR)
$ErrorActionPreference = 'Stop'
$RepoDir = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($GameDir) -or !(Test-Path -LiteralPath $GameDir -PathType Container)) {
    Write-Error "[Tokyo World] Missing Minecraft instance folder (INST_MC_DIR)."
    exit 1
}
if (!(Test-Path -LiteralPath (Join-Path $RepoDir '.git'))) {
    Write-Error "[Tokyo World] Repository must be a Git clone: $RepoDir"
    exit 1
}
$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) { Write-Error "[Tokyo World] Git is not installed or not in PATH."; exit 1 }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot = Join-Path $GameDir ('.tokyo-world-backups\' + $stamp)
function Backup-File([string]$relative, [string]$destination) {
    $backup = Join-Path $backupRoot $relative
    $parent = Split-Path -Parent $backup
    if (!(Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
    Copy-Item -LiteralPath $destination -Destination $backup -Force
}
function Write-IfChanged([string]$relative, [string]$content) {
    $dest = Join-Path $GameDir $relative
    if (!(Test-Path -LiteralPath $dest -PathType Leaf)) { return }
    $old = [System.IO.File]::ReadAllText($dest)
    if ($old -cne $content) {
        Backup-File $relative $dest
        [System.IO.File]::WriteAllText($dest, $content, ([System.Text.UTF8Encoding]::new($false)))
        Write-Host "[Tokyo World] Repaired: $relative"
    }
}
Write-Host "[Tokyo World] Checking GitHub for updates..."
& $git.Source -C $RepoDir pull --ff-only origin main
if ($LASTEXITCODE -ne 0) {
    Write-Warning "[Tokyo World] Cannot pull GitHub changes; using last downloaded files."
}
$managed = @('config', 'defaultconfigs', 'kubejs', 'resourcepacks', 'shaderpacks')
$copied = 0
foreach ($folder in $managed) {
    $srcRoot = Join-Path $RepoDir $folder
    if (!(Test-Path -LiteralPath $srcRoot -PathType Container)) { continue }
    foreach ($source in (Get-ChildItem -LiteralPath $srcRoot -Recurse -File)) {
        $relative = $source.FullName.Substring($RepoDir.Length).TrimStart([char[]]@('\', '/'))
        $dest = Join-Path $GameDir $relative
        if (!(Test-Path -LiteralPath (Split-Path -Parent $dest))) {
            New-Item -ItemType Directory -Path (Split-Path -Parent $dest) -Force | Out-Null
        }
        if (Test-Path -LiteralPath $dest -PathType Leaf) {
            if ((Get-FileHash -LiteralPath $source.FullName -Algorithm SHA256).Hash -eq
                (Get-FileHash -LiteralPath $dest -Algorithm SHA256).Hash) { continue }
            Backup-File $relative $dest
        }
        Copy-Item -LiteralPath $source.FullName -Destination $dest -Force
        $copied++
        Write-Host "[Tokyo World] Updated: $relative"
    }
}
# Repair options.txt damaged by previous updater. Do NOT change resource pack selection again.
try {
    $relative = 'options.txt'
    $file = Join-Path $GameDir $relative
    if (Test-Path -LiteralPath $file -PathType Leaf) {
        $old = [System.IO.File]::ReadAllText($file)
        $match = [regex]::Match($old, '(?m)^resourcePacks:(\[[^\r\n]*\])')
        if ($match.Success) {
            $invalid = $false
            try {
                $arr = ConvertFrom-Json -InputObject $match.Groups[1].Value
                foreach ($element in @($arr)) { if ($element -isnot [string]) { $invalid = $true } }
            } catch { $invalid = $true }
            if ($match.Groups[1].Value -match '\[\s*\{') { $invalid = $true }
            if ($invalid) {
                $new = $old.Replace($match.Value, 'resourcePacks:[]')
                Write-IfChanged $relative $new
                Write-Host "[Tokyo World] Fixed malformed resourcePacks in options.txt."
            }
        }
    }
} catch { Write-Warning "[Tokyo World] Cannot repair options.txt: $($_.Exception.Message)" }

# Make Tokyo World visible even on installations with vanilla title-screen fallback.
try {
    $menuRel = 'config\fancymenu\customization\title_screen_layout.txt'
    $menuPath = Join-Path $GameDir $menuRel
    $background = Join-Path $GameDir 'config\fancymenu\assets\tokyo_world_menu_background.png'
    if ((Test-Path -LiteralPath $menuPath -PathType Leaf) -and (Test-Path -LiteralPath $background -PathType Leaf)) {
        $before = [System.IO.File]::ReadAllText($menuPath)
        $after = $before
        $blockPattern = '(?ms)^menu_background \{.*?^\}'
        $blocks = [regex]::Matches($after, $blockPattern)
        foreach ($block in $blocks) {
            if ($block.Value -match 'background_type = image' -and
                $block.Value -match 'tokyo_world_menu_background\.png') {
                $fixed = $block.Value.Replace('1true2', 'show_background = true').Replace('show_background = false', 'show_background = true')
                $after = $after.Replace($block.Value, $fixed)
            }
        }
        $buttons = [regex]::Matches($after, '(?ms)^vanilla_button \{.*?^\}')
        foreach ($button in $buttons) {
            if ($button.Value -match 'instance_identifier = minecraft_logo_widget' -or
                $button.Value -match 'instance_identifier = minecraft_splash_widget') {
                $after = $after.Replace($button.Value, $button.Value.Replace('is_hidden = false','is_hidden = true'))
            }
        }
        Write-IfChanged $menuRel $after
    }
    $optionsRel = 'config\fancymenu\options.txt'
    $optionsPath = Join-Path $GameDir $optionsRel
    if ((Test-Path -LiteralPath $optionsPath -PathType Leaf) -and (Test-Path -LiteralPath $background -PathType Leaf)) {
        $before = [System.IO.File]::ReadAllText($optionsPath)
        $pattern = "(?m)^S:global_menu_background_texture = '[^']*';"
        $newValue = "S:global_menu_background_texture = '[source:local]/config/fancymenu/assets/tokyo_world_menu_background.png';"
        $after = [regex]::Replace($before, $pattern, $newValue)
        Write-IfChanged $optionsRel $after
    }
} catch { Write-Warning "[Tokyo World] FancyMenu repair skipped: $($_.Exception.Message)" }

# KubeJS exposes quest images as client resources. The existing ZIP is the image source.
try {
    $zipPath = Join-Path $GameDir 'resourcepacks\TokyoWorld_Visuals.zip'
    if (Test-Path -LiteralPath $zipPath -PathType Leaf) {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
        $assetCount = 0
        try {
            $assets = @('main_menu.png','logo.png','quests/tech_automation.png','quests/magic_arcana.png','quests/exploration_adventure.png','quests/building_farming.png')
            foreach ($sub in $assets) {
                $entryName = 'assets/tokyoworld/textures/gui/' + $sub
                $entry = $zip.GetEntry($entryName)
                if ($null -eq $entry) { Write-Warning "[Tokyo World] Missing image: $entryName"; continue }
                $relative = 'kubejs\assets\tokyoworld\textures\gui\' + $sub.Replace('/','\')
                $target = Join-Path $GameDir $relative
                if (!(Test-Path -LiteralPath (Split-Path -Parent $target))) {
                    New-Item -Path (Split-Path -Parent $target) -ItemType Directory -Force | Out-Null
                }
                $extract = $true
                if (Test-Path -LiteralPath $target -PathType Leaf) {
                    if ((Get-Item -LiteralPath $target).Length -eq $entry.Length) {
                        $stream = $entry.Open()
                        $sha = [System.Security.Cryptography.SHA256]::Create()
                        try { $sourceHash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') }
                        finally { $sha.Dispose(); $stream.Dispose() }
                        $extract = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $sourceHash
                    }
                    if ($extract) { Backup-File $relative $target }
                }
                if ($extract) {
                    [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $true)
                    $assetCount++
                }
            }
        } finally { $zip.Dispose() }
        Write-Host "[Tokyo World] Quest GUI images ready: $assetCount updated."
    } else {
        Write-Warning "[Tokyo World] Visual resource pack ZIP missing; cannot import quest artwork."
    }
} catch { Write-Warning "[Tokyo World] Quest artwork import skipped: $($_.Exception.Message)" }


# Tokyo World Expert Fun tuning: retain existing mod defaults except for selected keys.
# More expensive Digital Miner, faster operation, easier JEI filters and modest power tuning.
try {
    $usageRel = 'config\Mekanism\machine-usage.toml'
    $usageFile = Join-Path $GameDir $usageRel
    if (Test-Path -LiteralPath $usageFile -PathType Leaf) {
        $text = [System.IO.File]::ReadAllText($usageFile)
        $text = [regex]::Replace($text, '(?m)^(\s*digitalMiner\s*=\s*")[0-9]+("\s*)
Write-Host "[Tokyo World] Safe config sync complete. Extra mods require a separate installation step."
exit 0
, '120002')
        $text = [regex]::Replace($text, '(?m)^(\s*crusher\s*=\s*")[0-9]+("\s*)
Write-Host "[Tokyo World] Safe config sync complete. Extra mods require a separate installation step."
exit 0
, '1652')
        Write-IfChanged $usageRel $text
    }
    $generalRel = 'config\Mekanism\general.toml'
    $generalFile = Join-Path $GameDir $generalRel
    if (Test-Path -LiteralPath $generalFile -PathType Leaf) {
        $text = [System.IO.File]::ReadAllText($generalFile)
        $text = [regex]::Replace($text, '(?m)^(\s*easyMinerFilters\s*=\s*)false(\s*)
Write-Host "[Tokyo World] Safe config sync complete. Extra mods require a separate installation step."
exit 0
, '1true2')
        $text = [regex]::Replace($text, '(?m)^(\s*ticksPerMine\s*=\s*)80(\s*)
Write-Host "[Tokyo World] Safe config sync complete. Extra mods require a separate installation step."
exit 0
, '1602')
        Write-IfChanged $generalRel $text
    }
} catch { Write-Warning "[Tokyo World] Mekanism tuning skipped: $($_.Exception.Message)" }

Write-Host "[Tokyo World] Done. Files copied: $copied"
Write-Host "[Tokyo World] Safe config sync complete. Extra mods require a separate installation step."
exit 0
