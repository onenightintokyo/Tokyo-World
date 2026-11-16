#Requires -Version 5.1
<#
Tokyo World 1.20.1: repair Oculus 1.8.0 + ImmediatelyFast 1.2.4.
No world deletion. Original JAR is moved to a timestamped backup.
Close Minecraft and the launcher before running.
#>
[CmdletBinding()]
param([string]$GameDir = (Join-Path $env:LOCALAPPDATA 'TokyoWorld'))
$ErrorActionPreference = 'Stop'
$mods = Join-Path $GameDir 'mods'
if (!(Test-Path -LiteralPath $mods -PathType Container)) {
    throw "Brak folderu mods: $mods. Podaj prawidłowe -GameDir."
}
$oculus = @(Get-ChildItem -LiteralPath $mods -Filter '*.jar' -File |
    Where-Object { $_.Name -match '(?i)^oculus[-_.]' })
$fast = @(Get-ChildItem -LiteralPath $mods -Filter '*.jar' -File |
    Where-Object { $_.Name -match '(?i)^ImmediatelyFast[-_.]' })
Write-Host 'TOKYO WORLD - NAPRAWA URUCHAMIANIA' -ForegroundColor Magenta
Write-Host "Instalacja: $GameDir"
Write-Host "Oculus: $($oculus.Count) | ImmediatelyFast: $($fast.Count)"
if ($oculus.Count -eq 0 -or $fast.Count -eq 0) {
    Write-Host 'Brak wykrytej konfliktowej pary - nic nie zmieniono.' -ForegroundColor Yellow
    exit 0
}
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backup = Join-Path $GameDir ".tokyo\backups\startup-fix-$timestamp"
New-Item -Path $backup -ItemType Directory -Force | Out-Null
$moved = @()
try {
    foreach ($jar in $fast) {
        $dst = Join-Path $backup $jar.Name
        if (Test-Path -LiteralPath $dst) { throw "Kopia już istnieje: $dst" }
        Move-Item -LiteralPath $jar.FullName -Destination $dst -ErrorAction Stop
        $moved += @{from=$jar.FullName; to=$dst}
        Write-Host "Wyłączono: $($jar.Name)" -ForegroundColor Green
    }
} catch {
    foreach ($x in $moved) {
        if ((Test-Path -LiteralPath $x.to) -and -not (Test-Path -LiteralPath $x.from)) {
            Move-Item -LiteralPath $x.to -Destination $x.from -ErrorAction SilentlyContinue
        }
    }
    throw
}
Write-Host "Kopia zapasowa: $backup"
Write-Host 'Zamknij to okno i kliknij GRAJ w launcherze.' -ForegroundColor Cyan
Write-Host 'Jeśli nadal nie startuje, wyślij nowy logs\latest.log / crash-reports.' -ForegroundColor Cyan
