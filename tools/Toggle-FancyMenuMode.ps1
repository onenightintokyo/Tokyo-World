# Toggle Tokyo World FancyMenu modpack/edit mode without changing other config.
param(
    [switch]$Edit,
    [switch]$Play,
    [string]$GameDir = ''
)
$ErrorActionPreference = 'Stop'
if ($Edit -eq $Play) { throw 'Choose exactly one switch: -Edit or -Play' }
if ([string]::IsNullOrWhiteSpace($GameDir)) {
    $GameDir = Join-Path $env:APPDATA 'PrismLauncher\instances\Tokyo_World_Forge_1.20.1\minecraft'
}
$stateDir = Join-Path $GameDir '.tokyo-world-state'
$marker = Join-Path $stateDir 'fancymenu_edit_mode'
if (!(Test-Path -LiteralPath $stateDir)) { New-Item -ItemType Directory -Path $stateDir -Force | Out-Null }
if ($Edit) {
    [System.IO.File]::WriteAllText($marker, 'Editor temporarily enabled. Use the PLAY mode launcher to hide again.')
    Write-Host '[Tokyo World] FancyMenu EDIT mode enabled for next launch.'
} else {
    if (Test-Path -LiteralPath $marker) { Remove-Item -LiteralPath $marker -Force }
    Write-Host '[Tokyo World] FancyMenu normal PLAY mode enabled for next launch.'
}
Write-Host '[Tokyo World] Restart Minecraft via Prism Launcher to apply.'
