# GitHub Actions UI smoke test.
# Verifies the EXE remains runnable after attaching real JPEG artwork to its
# PE overlay, then snapshots the genuine WinForms window at 1280x720.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$exe = Join-Path (Get-Location) 'dist/TokyoWorldLauncher.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'Missing EXE' }
$testDir = Join-Path $env:RUNNER_TEMP 'tokyo-concept-art-smoke'
New-Item -ItemType Directory -Path $testDir -Force | Out-Null
$testExe = Join-Path $testDir 'TokyoWorldLauncher.exe'
$testJpg = Join-Path $testDir 'test-art.jpg'
$preview = Join-Path (Get-Location) 'dist/CrimsonTokyoUI-Preview.png'

$image = [System.Drawing.Bitmap]::new(1280, 720)
$g = [System.Drawing.Graphics]::FromImage($image)
try {
    $g.Clear([System.Drawing.Color]::FromArgb(40, 10, 45))
    $g.FillRectangle([System.Drawing.Brushes]::DeepPink, 0, 0, 1280, 211)
    $image.Save($testJpg, [System.Drawing.Imaging.ImageFormat]::Jpeg)
} finally {
    $g.Dispose()
    $image.Dispose()
}
Copy-Item -LiteralPath $exe -Destination $testExe -Force
[byte[]]$bytes = [System.IO.File]::ReadAllBytes($testJpg)
[byte[]]$magic = [System.Text.Encoding]::ASCII.GetBytes('TOKYO_CRIMSON_ART_V171__')
if ($magic.Length -ne 24) { throw 'Wrong magic length' }
$stream = [System.IO.File]::Open($testExe,
    [System.IO.FileMode]::Append,
    [System.IO.FileAccess]::Write,
    [System.IO.FileShare]::None)
try {
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Write($magic, 0, $magic.Length)
    [byte[]]$lengthBytes = [System.BitConverter]::GetBytes([long]$bytes.Length)
    $stream.Write($lengthBytes, 0, $lengthBytes.Length)
    [byte[]]$hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    $stream.Write($hash, 0, $hash.Length)
} finally { $stream.Dispose() }

# The GUI renders to a PNG with its real WinForms controls (without login/network).
# WinExe runs as a GUI subsystem process: use -Wait so the screenshot file
# exists before asserting it. Direct invocation in PowerShell may return early.
$render = Start-Process -FilePath $testExe -ArgumentList @(
    '--render-preview', ('"' + $preview + '"')) -PassThru -Wait
if ($render.ExitCode -ne 0 -or !(Test-Path -LiteralPath $preview)) {
    throw 'Concept UI render failed after attaching artwork to single EXE'
}
$shot = [System.Drawing.Bitmap]::FromFile($preview)
try {
    if ($shot.Width -ne 1280 -or $shot.Height -ne 720) {
        throw 'Unexpected screenshot dimensions'
    }
    $pixel = $shot.GetPixel(1100, 100)
    if ($pixel.R -lt 180 -or $pixel.G -gt 110 -or $pixel.B -lt 75) {
        throw "Canvas did not paint the embedded pink artwork; sampled pixel: $pixel"
    }
} finally { $shot.Dispose() }
Write-Host 'Crimson Concept 4 screenshot generated successfully.'
