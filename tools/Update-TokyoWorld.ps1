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
# Validate the Cucumber tag map before Minecraft starts. The old ZIP had an
# invalid fragment after the closing brace and caused repeated recipe errors.
try {
    $cucumberRel = 'config\cucumber-tags.json'
    $cucumberLocal = Join-Path $GameDir $cucumberRel
    $cucumberRepo = Join-Path $RepoDir $cucumberRel
    if ((Test-Path -LiteralPath $cucumberLocal -PathType Leaf) -and
        (Test-Path -LiteralPath $cucumberRepo -PathType Leaf)) {
        try {
            $null = Get-Content -LiteralPath $cucumberLocal -Raw -Encoding UTF8 | ConvertFrom-Json -ErrorAction Stop
            Write-Host "[Tokyo World] Cucumber JSON syntax: VALID before game start."
        } catch {
            Backup-File $cucumberRel $cucumberLocal
            Copy-Item -LiteralPath $cucumberRepo -Destination $cucumberLocal -Force
            Write-Warning "[Tokyo World] Malformed Cucumber JSON restored from repository."
        }
    }
} catch {
    Write-Warning ("[Tokyo World] Cannot verify Cucumber tag map: " + $_.Exception.Message)
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
                # This old layout provides button positions. The independent
                # tokyo_world.txt layout owns the background, so they cannot fight.
                $newLayout = Join-Path $GameDir 'config\fancymenu\customization\tokyo_world.txt'
                $enabled = !(Test-Path -LiteralPath $newLayout -PathType Leaf)
                $fixed = $block.Value.Replace('1true2', 'show_background = false')
                if ($enabled) {
                    $fixed = $fixed.Replace('show_background = false', 'show_background = true')
                } else {
                    $fixed = $fixed.Replace('show_background = true', 'show_background = false')
                }
                $after = $after.Replace($block.Value, $fixed)
            }
        }
        $buttons = [regex]::Matches($after, '(?ms)^vanilla_button \{.*?^\}')
        $labels = @{
            'mc_titlescreen_singleplayer_button' = 'Rozpocznij przygode'
            'mc_titlescreen_multiplayer_button' = 'Graj ze znajomymi'
            'mc_titlescreen_options_button' = 'Ustawienia'
            'mc_titlescreen_quit_button' = 'Wyjdz z gry'
            'titlescreen_mods_button' = 'Lista modow'
        }
        foreach ($button in $buttons) {
            $modified = $button.Value
            if ($modified -match 'instance_identifier = minecraft_logo_widget' -or
                $modified -match 'instance_identifier = minecraft_splash_widget') {
                $modified = $modified.Replace('is_hidden = false','is_hidden = true')
            }
            foreach ($id in @($labels.Keys)) {
                if ($modified -match ('(?m)^\s*instance_identifier = ' + [regex]::Escape($id) + '\s*$')) {
                    if ($modified -match '(?m)^\s*label =') {
                        $modified = [regex]::Replace($modified, '(?m)^(\s*label =).*$', '$1 ' + $labels[$id])
                    } else {
                        $modified = $modified.Replace('  element_type = vanilla_button', "  element_type = vanilla_button" + [Environment]::NewLine + "  label = " + $labels[$id])
                    }
                }
            }
            $after = $after.Replace($button.Value, $modified)
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

    $newLayout = Join-Path $GameDir 'config\fancymenu\customization\tokyo_world.txt'
    Write-Host ("[Tokyo World] FancyMenu independent title layout installed: " + (Test-Path -LiteralPath $newLayout -PathType Leaf))
    Write-Host ("[Tokyo World] FancyMenu background PNG available: " + (Test-Path -LiteralPath $background -PathType Leaf))
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


# Non-stock Expert Fun tuning using keys confirmed in the player's existing 1.20.1 configs.
# All edits are backed up and do not modify world saves.
try {
    $usageRel = 'config\Mekanism\machine-usage.toml'
    $usageFile = Join-Path $GameDir $usageRel
    if (Test-Path -LiteralPath $usageFile -PathType Leaf) {
        $original = [System.IO.File]::ReadAllText($usageFile)
        $modified = $original.Replace('digitalMiner = "1000"', 'digitalMiner = "2000"')
        $modified = $modified.Replace('crusher = "50"', 'crusher = "65"')
        Write-IfChanged $usageRel $modified
    }
    $generalRel = 'config\Mekanism\general.toml'
    $generalFile = Join-Path $GameDir $generalRel
    if (Test-Path -LiteralPath $generalFile -PathType Leaf) {
        $original = [System.IO.File]::ReadAllText($generalFile)
        $modified = $original.Replace('easyMinerFilters = false', 'easyMinerFilters = true')
        $modified = $modified.Replace('ticksPerMine = 80', 'ticksPerMine = 60')
        Write-IfChanged $generalRel $modified
    }
} catch { Write-Warning "[Tokyo World] Mekanism Expert Fun settings skipped: $($_.Exception.Message)" }

# FancyMenu modpack mode: hide the editing menu bar and its hotkeys.
# A local marker allows the owner to reopen the editor when needed.
try {
    $fmRel = 'config\fancymenu\options.txt'
    $fmPath = Join-Path $GameDir $fmRel
    $editMarker = Join-Path $GameDir '.tokyo-world-state\fancymenu_edit_mode'
    if (Test-Path -LiteralPath $fmPath -PathType Leaf) {
        $fmBefore = [System.IO.File]::ReadAllText($fmPath)
        $fmAfter = $fmBefore
        $editMode = Test-Path -LiteralPath $editMarker -PathType Leaf
        $modeValue = if ($editMode) { 'false' } else { 'true' }
        $overlayValue = if ($editMode) { 'true' } else { 'false' }
        $fmAfter = [regex]::Replace(
            $fmAfter,
            "(?m)^B:modpack_mode\s*=\s*'(?:true|false)';",
            ("B:modpack_mode = '" + $modeValue + "';")
        )
        if ($fmAfter -notmatch '(?m)^B:modpack_mode\s*=') {
            $fmAfter = $fmAfter.Replace('##[customization]', "##[customization]" + [Environment]::NewLine + "B:modpack_mode = '" + $modeValue + "';")
        }
        $fmAfter = [regex]::Replace(
            $fmAfter,
            "(?m)^B:show_customization_overlay\s*=\s*'(?:true|false)';",
            ("B:show_customization_overlay = '" + $overlayValue + "';")
        )
        $fmAfter = $fmAfter.Replace("B:show_welcome_screen = 'true';", "B:show_welcome_screen = 'false';")
        Write-IfChanged $fmRel $fmAfter
        Write-Host ("[Tokyo World] FancyMenu editing bar: " + $(if ($editMode) { 'EDIT MODE' } else { 'HIDDEN (modpack mode)' }))
    } else {
        Write-Warning "[Tokyo World] FancyMenu options not found; skipping modpack mode."
    }
} catch {
    Write-Warning ("[Tokyo World] FancyMenu modpack mode not applied: " + $_.Exception.Message)
}

Write-Host "[Tokyo World] Done. Files copied: $copied"
Write-Host "[Tokyo World] Safe config sync complete. Extra mods require a separate installation step."
exit 0
