# Tokyo World optional expansion installer — Windows PowerShell 5.1+
# Preview by default; use -Apply to install. Existing mods, saves, configs are untouched.
param(
    [ValidateSet('Core','Expanded','Adventure','Lab','World','Factory','Automation','Resources','Mega','All')][string]$Profile = 'Core',
    [switch]$Apply,
    [string]$GameDir = ''
)
$ErrorActionPreference = 'Stop'
$RepoDir = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $RepoDir 'modpacks\tokyo_modules.json'
if (!(Test-Path -LiteralPath $manifestPath)) { throw "Manifest missing: $manifestPath" }
if ([string]::IsNullOrWhiteSpace($GameDir)) {
    if (![string]::IsNullOrWhiteSpace($env:INST_MC_DIR)) {
        $GameDir = $env:INST_MC_DIR
    } else {
        $GameDir = Join-Path $env:APPDATA 'PrismLauncher\instances\Tokyo_World_Forge_1.20.1\minecraft'
    }
}
$modsDir = Join-Path $GameDir 'mods'
if (!(Test-Path -LiteralPath $modsDir -PathType Container)) { throw "Mods directory not found: $modsDir" }
$config = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($config.minecraft -ne '1.20.1' -or $config.loader -ne 'forge') {
    throw 'Manifest does not target Forge 1.20.1'
}
$existing = @(Get-ChildItem -LiteralPath $modsDir -Filter '*.jar' -File | Select-Object -ExpandProperty Name)
$script:planned = @{}
$script:planning = @{}
$script:completed = @{}
$script:projectCache = @{}
$script:versionCache = @{}
$script:apiCache = @{}
$script:unavailable = New-Object 'System.Collections.Generic.List[string]'
$script:knownExisting = @{
    'ae2' = '^(appliedenergistics2|ae2)[-_]'
    'applied-energistics-2' = '^(appliedenergistics2|ae2)[-_]'
    'guideme' = '^guideme[-_]'
    'applied-mekanistics' = '^Applied[-_]Mekanistics[-_]'
    'rftools-utility' = '^rftoolsutility[-_]'
    'rftools-power' = '^rftoolspower[-_]'
    'thermals-delight' = '^((Thermal.s Delight)|(thermals[-_]delight))[ _.\-].*\.jar$'
    'mekanism' = '^Mekanism[-_]'
    'create' = '^create[-_]'
    'architectury-api' = '^architectury[-_]'
    'cofh-core' = '^cofh_core[-_]'
    'geckolib' = '^geckolib[-_]'
    'curios' = '^curios[-_]'
    'cyclops-core' = '^CyclopsCore[-_]'
    'industrial-foregoing' = '^industrial-foregoing[-_]'
    'rftools-base' = '^rftoolsbase[-_]'
    'mcjtylib' = '^mcjtylib[-_]'
    'patchouli' = '^Patchouli[-_]'
    'moonlight' = '^moonlight[-_]'
    'framework' = '^framework[-_]'
    'ftb-library-forge' = '^ftb-library[-_]'
    'ftb-teams-forge' = '^ftb-teams[-_]'
    'botarium' = '^botarium[-_]'
    'thermal-foundation' = '^thermal_foundation[-_]'
}
function Get-ModrinthJson([string]$path) {
    $uri = 'https://api.modrinth.com/v2/' + $path
    if ($script:apiCache.ContainsKey($uri)) { return $script:apiCache[$uri] }
    $result = Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 50 -Headers @{
        'User-Agent' = 'TokyoWorldModpack/0.8 (Forge 1.20.1 MEGA edition)'
    }
    $script:apiCache[$uri] = $result
    return $result
}
function Existing-Matches([string]$slug, [string]$customRegex) {
    # Both explicit patterns and known upstream filenames count as installed.
    $patterns = New-Object 'System.Collections.Generic.List[string]'
    if (-not [string]::IsNullOrWhiteSpace($customRegex)) { $patterns.Add($customRegex) }
    if ($script:knownExisting.ContainsKey($slug)) {
        $patterns.Add($script:knownExisting[$slug])
    }
    if ($patterns.Count -eq 0) {
        $normalized = ($slug -replace '[-_ ]','')
        $patterns.Add(('^' + [regex]::Escape($normalized) + '[-_]'))
    }
    foreach ($name in $existing) {
        foreach ($pat in $patterns) {
            if ($name -match $pat) { return $true }
        }
    }
    return $false
}
function Expand-Versions($value) {
    # Windows PowerShell 5.1 sometimes treats a JSON array as one nested object.
    # Flatten exactly one level before filtering versions or dependencies.
    foreach ($entry in @($value)) {
        if ($null -eq $entry) { continue }
        if ($entry -is [System.Array]) {
            foreach ($child in $entry) {
                if ($null -ne $child) { Write-Output $child }
            }
        } else {
            Write-Output $entry
        }
    }
}
function Queue-Mod([string]$projectRef, [string]$exactVersionId, [string]$versionNeed, [string]$matchRegex) {
    $project = Get-ModrinthJson ('project/' + [uri]::EscapeDataString($projectRef))
    if ($project.project_type -ne 'mod') { throw "Not a mod: $projectRef" }
    $slug = [string]$project.slug
    $projectId = [string]$project.id
    if ($script:completed.ContainsKey($projectId)) { return }
    if (Existing-Matches $slug $matchRegex) {
        Write-Host "[Tokyo World] Already installed: $slug"
        $script:completed[$projectId] = $true
        return
    }
    if ($script:planned.ContainsKey($projectId)) { return }
    if ($script:planning.ContainsKey($projectId)) { return }
    $script:planning[$projectId] = $true
    try {
        if (![string]::IsNullOrWhiteSpace($exactVersionId)) {
            $version = Get-ModrinthJson ('version/' + [uri]::EscapeDataString($exactVersionId))
            if ([string]$version.project_id -ne $projectId) { throw "Pinned version belongs to another project: $slug" }
        } else {
            $query = 'project/' + [uri]::EscapeDataString($projectId) +
                '/version?game_versions=%5B%221.20.1%22%5D&loaders=%5B%22forge%22%5D'
            $rawVersions = Get-ModrinthJson $query
            $versions = @(Expand-Versions $rawVersions)
            $filtered = @($versions | Where-Object {
                $_ -isnot [System.Array] -and
                $_.game_versions -contains '1.20.1' -and $_.loaders -contains 'forge' -and
                $_.version_type -eq 'release' -and
                ([string]::IsNullOrWhiteSpace($versionNeed) -or $_.version_number -match [regex]::Escape($versionNeed))
            } | Sort-Object date_published -Descending)
            if ($filtered.Count -lt 1) { throw "No matching release for $slug / Forge 1.20.1 / $versionNeed" }
            $version = $filtered[0]
        }
        if ($null -eq $version -or $version -is [System.Array] -or
            @($version.version_number).Count -ne 1) {
            throw "Ambiguous or malformed Modrinth version response for $slug"
        }
        if ($version.game_versions -notcontains '1.20.1' -or $version.loaders -notcontains 'forge') {
            throw "Incompatible version selected for $slug"
        }
        $jar = @($version.files | Where-Object { $_.filename -match '\.jar$' -and $_.primary -eq $true } | Select-Object -First 1)
        if ($jar.Count -eq 0) { $jar = @($version.files | Where-Object { $_.filename -match '\.jar$' } | Select-Object -First 1) }
        if ($jar.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$jar[0].hashes.sha512)) {
            throw "No SHA-512 verified JAR for $slug"
        }
        $file = $jar[0]
        # Preserve valid upstream names including "Thermal's Delight 1.20.1 R.1.0.jar".
        # Reject path separators, invalid Windows filename characters, control
        # characters, reserved device names, and all non-JAR extensions.
        $filename = [string]$file.filename
        $invalidWindowsName = @($filename.ToCharArray() | Where-Object {
            [int][char]$_ -lt 32 -or '<>:"/\|?*'.Contains([string]$_)
        }).Count -gt 0
        $reserved = $filename -match '^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?$'
        if ([string]::IsNullOrWhiteSpace($filename) -or
            $filename -ne [IO.Path]::GetFileName($filename) -or
            $filename -notmatch '(?i)\.jar$' -or
            $filename -match '\p{C}' -or
            $filename.EndsWith(' ') -or
            $filename.EndsWith('.') -or
            $filename.Length -gt 200 -or $reserved -or $invalidWindowsName) {
            throw "Unsafe or invalid JAR filename for $($slug): $filename"
        }
        $uri = [Uri]$file.url
        if ($uri.Scheme -ne 'https' -or $uri.Host -notin @('cdn.modrinth.com','cdn-raw.modrinth.com')) {
            throw "Unexpected download host for ${slug}: $($uri.Host)"
        }
        $existingTarget = Join-Path $modsDir $filename
        if (Test-Path -LiteralPath $existingTarget -PathType Leaf) {
            $fileHash = (Get-FileHash -LiteralPath $existingTarget -Algorithm SHA512).Hash
            if ($fileHash -ine [string]$file.hashes.sha512) {
                throw "Existing JAR has different checksum (not overwritten): $filename"
            }
            $script:completed[$projectId] = $true
            Write-Host "[Tokyo World] Already installed (SHA-512): $filename"
            return
        }
        $script:planned[$projectId] = [pscustomobject]@{
            name = [string]$project.title
            slug = $slug
            version = [string]$version.version_number
            filename = $filename
            url = [string]$file.url
            sha512 = [string]$file.hashes.sha512
        }
        foreach ($dep in @(Expand-Versions $version.dependencies)) {
            if ($null -eq $dep -or $dep.dependency_type -ne 'required') { continue }
            if ($dep.project_id) {
                Queue-Mod ([string]$dep.project_id) '' '' ''
            } elseif ($dep.version_id) {
                $depVersion = Get-ModrinthJson ('version/' + [uri]::EscapeDataString([string]$dep.version_id))
                Queue-Mod ([string]$depVersion.project_id) ([string]$dep.version_id) '' ''
            }
        }
        $script:completed[$projectId] = $true
    } finally {
        [void]$script:planning.Remove($projectId)
    }
}
$wanted = @()
$megaGroups = @('tech-core','tech-expanded','tech-lab','world-explore',
                'factory-next','automation-next','resources-next',
                'mega-technology','mega-magic','mega-adventure','mega-building')
foreach ($name in @('tech-core','tech-expanded','adventure','tech-lab','world-explore',
                    'factory-next','automation-next','resources-next',
                    'mega-technology','mega-magic','mega-adventure','mega-building')) {
    $use = ($Profile -eq 'All') -or
        ($Profile -eq 'Mega' -and $name -in $megaGroups) -or
        ($Profile -eq 'Core' -and $name -eq 'tech-core') -or
        ($Profile -eq 'Expanded' -and $name -in @('tech-core','tech-expanded')) -or
        ($Profile -eq 'Adventure' -and $name -eq 'adventure') -or
        ($Profile -eq 'Lab' -and $name -eq 'tech-lab') -or
        ($Profile -eq 'World' -and $name -eq 'world-explore') -or
        ($Profile -eq 'Factory' -and $name -eq 'factory-next') -or
        ($Profile -eq 'Automation' -and $name -eq 'automation-next') -or
        ($Profile -eq 'Resources' -and $name -eq 'resources-next')
    if ($use) { $wanted += @($config.profiles.$name) }
}
Write-Host "[Tokyo World] Profile=$Profile, Minecraft=1.20.1, Forge, apply=$Apply"
Write-Host "[Tokyo World] Resolving versions and required dependencies from Modrinth..."
foreach ($mod in $wanted) {
    if ($Profile -notin @('Mega','All')) {
        Queue-Mod ([string]$mod.project) ([string]$mod.versionId) ([string]$mod.version) ([string]$mod.match)
        continue
    }

    # MEGA should not fail at one project with no Forge release or absent Modrinth page.
    # Restore the plan and completion cache to their previous state for that mod
    # (including any dependencies queued before the failure).
    $beforePlan = @($script:planned.Keys)
    $beforeDone = @($script:completed.Keys)
    try {
        Queue-Mod ([string]$mod.project) ([string]$mod.versionId) ([string]$mod.version) ([string]$mod.match)
    } catch {
        foreach ($key in @($script:planned.Keys)) {
            if ($beforePlan -notcontains $key) { [void]$script:planned.Remove($key) }
        }
        foreach ($key in @($script:completed.Keys)) {
            if ($beforeDone -notcontains $key) { [void]$script:completed.Remove($key) }
        }
        $msg = ([string]$mod.project + ': ' + $_.Exception.Message)
        $script:unavailable.Add($msg)
        Write-Warning ("[Tokyo World] SKIPPED " + $msg)
    }
}
$toInstall = @($script:planned.Values | Sort-Object name)
if ($Profile -in @('Mega','All')) {
    Write-Host ("[Tokyo World] Requested projects: {0}; planned JARs (incl. dependencies): {1}; unavailable: {2}" -f $wanted.Count, $toInstall.Count, $script:unavailable.Count)
    if ($script:unavailable.Count) {
        $report = Join-Path $GameDir '.tokyo-world-mega-skipped.txt'
        $script:unavailable | Out-File -LiteralPath $report -Encoding UTF8
        Write-Warning ("[Tokyo World] Missing/unavailable mods saved to: " + $report)
    }
}
if ($toInstall.Count -eq 0) { Write-Host '[Tokyo World] All requested mods appear to be installed.'; exit 0 }
foreach ($mod in $toInstall) { Write-Host ("  + {0} [{1}] -> {2}" -f $mod.name,$mod.version,$mod.filename) }
Write-Host "[Tokyo World] New JAR files planned: $($toInstall.Count)"
if (!$Apply) {
    Write-Host '[Tokyo World] Dry run only. Add -Apply to download and install.'
    exit 0
}
$staging = Join-Path $GameDir '.tokyo-world-mod-staging'
New-Item -Path $staging -ItemType Directory -Force | Out-Null
$added = New-Object 'System.Collections.Generic.List[string]'
try {
    foreach ($mod in $toInstall) {
        $target = Join-Path $modsDir $mod.filename
        if (Test-Path -LiteralPath $target) { throw "File already exists: $target" }
        $temp = Join-Path $staging $mod.filename
        Write-Host "[Tokyo World] Downloading $($mod.filename)"
        Invoke-WebRequest -Uri $mod.url -UseBasicParsing -TimeoutSec 120 -OutFile $temp
        if ((Get-FileHash -LiteralPath $temp -Algorithm SHA512).Hash -ine $mod.sha512) {
            throw "SHA-512 checksum mismatch: $($mod.filename)"
        }
    }
    foreach ($mod in $toInstall) {
        $target = Join-Path $modsDir $mod.filename
        Move-Item -LiteralPath (Join-Path $staging $mod.filename) -Destination $target
        $added.Add($target)
    }
    $lockdir = Join-Path $GameDir '.tokyo-world-state'
    New-Item -Path $lockdir -ItemType Directory -Force | Out-Null
    $lockfile = Join-Path $lockdir ('installed-' + $Profile.ToLowerInvariant() + '.json')
    ConvertTo-Json -InputObject $toInstall -Depth 8 | Out-File -LiteralPath $lockfile -Encoding UTF8
    Write-Host "[Tokyo World] INSTALLED $($toInstall.Count) JAR files with verified SHA-512."
    Write-Host "[Tokyo World] Restart the instance. If Forge reports a dependency error, share the Prism log."
} catch {
    foreach ($addedFile in $added) { Remove-Item -LiteralPath $addedFile -Force -ErrorAction SilentlyContinue }
    Write-Error "[Tokyo World] Install cancelled / rolled back: $($_.Exception.Message)"
    exit 1
} finally {
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Force -Recurse -ErrorAction SilentlyContinue }
}
