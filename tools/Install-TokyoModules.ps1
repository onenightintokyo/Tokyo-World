# Tokyo World optional expansion installer — Windows PowerShell 5.1+
# Preview by default; use -Apply to install. Existing mods, saves, configs are untouched.
param(
    [ValidateSet('Core','Expanded','Adventure','Lab','World','All')][string]$Profile = 'Core',
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
$script:projectCache = @{}
$script:versionCache = @{}
$script:knownExisting = @{
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
    return Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 40 -Headers @{ 'User-Agent' = 'TokyoWorldModpack/0.4 (modpack setup)' }
}
function Existing-Matches([string]$slug, [string]$customRegex) {
    $pat = $customRegex
    if ([string]::IsNullOrWhiteSpace($pat) -and $script:knownExisting.ContainsKey($slug)) {
        $pat = $script:knownExisting[$slug]
    }
    if ([string]::IsNullOrWhiteSpace($pat)) {
        $normalized = ($slug -replace '[-_ ]','')
        $pat = '^' + [regex]::Escape($normalized) + '[-_]'
    }
    foreach ($name in $existing) { if ($name -match $pat) { return $true } }
    return $false
}
function Queue-Mod([string]$projectRef, [string]$exactVersionId, [string]$versionNeed, [string]$matchRegex) {
    $project = Get-ModrinthJson ('project/' + [uri]::EscapeDataString($projectRef))
    if ($project.project_type -ne 'mod') { throw "Not a mod: $projectRef" }
    $slug = [string]$project.slug
    if (Existing-Matches $slug $matchRegex) {
        Write-Host "[Tokyo World] Already installed: $slug"
        return
    }
    $projectId = [string]$project.id
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
            $versions = @(Get-ModrinthJson $query)
            $filtered = @($versions | Where-Object {
                $_.game_versions -contains '1.20.1' -and $_.loaders -contains 'forge' -and
                $_.version_type -eq 'release' -and
                ([string]::IsNullOrWhiteSpace($versionNeed) -or $_.version_number -match [regex]::Escape($versionNeed))
            } | Sort-Object date_published -Descending)
            if ($filtered.Count -lt 1) { throw "No matching release for $slug / Forge 1.20.1 / $versionNeed" }
            $version = $filtered[0]
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
        $filename = [IO.Path]::GetFileName([string]$file.filename)
        if ($filename -ne [string]$file.filename -or $filename -notmatch '^[a-zA-Z0-9_\-+.() ]+\.jar$') {
            throw "Unsafe or invalid filename for $slug"
        }
        $uri = [Uri]$file.url
        if ($uri.Scheme -ne 'https' -or $uri.Host -notin @('cdn.modrinth.com','cdn-raw.modrinth.com')) {
            throw "Unexpected download host for ${slug}: $($uri.Host)"
        }
        $script:planned[$projectId] = [pscustomobject]@{
            name = [string]$project.title
            slug = $slug
            version = [string]$version.version_number
            filename = $filename
            url = [string]$file.url
            sha512 = [string]$file.hashes.sha512
        }
        foreach ($dep in @($version.dependencies)) {
            if ($null -eq $dep -or $dep.dependency_type -ne 'required') { continue }
            if ($dep.project_id) {
                Queue-Mod ([string]$dep.project_id) '' '' ''
            } elseif ($dep.version_id) {
                $depVersion = Get-ModrinthJson ('version/' + [uri]::EscapeDataString([string]$dep.version_id))
                Queue-Mod ([string]$depVersion.project_id) ([string]$dep.version_id) '' ''
            }
        }
    } finally {
        [void]$script:planning.Remove($projectId)
    }
}
$wanted = @()
foreach ($name in @('tech-core','tech-expanded','adventure','tech-lab','world-explore')) {
    $use = ($Profile -eq 'All') -or
        ($Profile -eq 'Core' -and $name -eq 'tech-core') -or
        ($Profile -eq 'Expanded' -and $name -in @('tech-core','tech-expanded')) -or
        ($Profile -eq 'Adventure' -and $name -eq 'adventure') -or
        ($Profile -eq 'Lab' -and $name -eq 'tech-lab') -or
        ($Profile -eq 'World' -and $name -eq 'world-explore')
    if ($use) { $wanted += @($config.profiles.$name) }
}
Write-Host "[Tokyo World] Profile=$Profile, Minecraft=1.20.1, Forge, apply=$Apply"
Write-Host "[Tokyo World] Resolving versions and required dependencies from Modrinth..."
foreach ($mod in $wanted) {
    Queue-Mod ([string]$mod.project) ([string]$mod.versionId) ([string]$mod.version) ([string]$mod.match)
}
$toInstall = @($script:planned.Values | Sort-Object name)
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
