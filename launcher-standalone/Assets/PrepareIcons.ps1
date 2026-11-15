# Create a multi-size Windows .ico and PNG from the TW neon artwork committed
# as base64 text. This avoids committing binary files with a text-only connector.
# Works in pwsh on the Windows GitHub runner; also works on local Windows PowerShell.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$source = Join-Path $PSScriptRoot 'TokyoWorldIcon.png.b64'
$png = Join-Path $PSScriptRoot 'TokyoWorldIcon.png'
$ico = Join-Path $PSScriptRoot 'TokyoWorldIcon.ico'

if (-not (Test-Path -LiteralPath $source)) {
    throw "Missing TW icon source: $source"
}
[byte[]]$bytes = [Convert]::FromBase64String(
    [System.IO.File]::ReadAllText($source).Trim()
)
if ($bytes.Length -lt 1000 -or
    [Convert]::ToHexString($bytes[0..7]) -ne '89504E470D0A1A0A') {
    throw 'TW icon source is not a valid PNG header.'
}
[System.IO.File]::WriteAllBytes($png, $bytes)

$inputStream = New-Object System.IO.MemoryStream(,$bytes)
$image = [System.Drawing.Image]::FromStream($inputStream)
$iconImages = New-Object 'System.Collections.Generic.List[object]'
try {
    foreach ($size in @(16, 24, 32, 48, 64, 128, 256)) {
        $bitmap = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.DrawImage($image, 0, 0, $size, $size)
            $imageStream = New-Object System.IO.MemoryStream
            $bitmap.Save($imageStream, [System.Drawing.Imaging.ImageFormat]::Png)
            $iconImages.Add([PSCustomObject]@{
                Size = $size
                Data = $imageStream.ToArray()
            })
            $imageStream.Dispose()
        } finally {
            $graphics.Dispose()
            $bitmap.Dispose()
        }
    }

    $out = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($out)
    try {
        $writer.Write([UInt16]0)       # Reserved.
        $writer.Write([UInt16]1)       # ICO, not CUR.
        $writer.Write([UInt16]$iconImages.Count)
        $offset = 6 + $iconImages.Count * 16
        foreach ($entry in $iconImages) {
            $dimension = if ($entry.Size -eq 256) { 0 } else { $entry.Size }
            $writer.Write([byte]$dimension)
            $writer.Write([byte]$dimension)
            $writer.Write([byte]0)    # Palette colors.
            $writer.Write([byte]0)
            $writer.Write([UInt16]1)  # Color planes.
            $writer.Write([UInt16]32) # Bits per pixel (PNG encoded).
            $writer.Write([UInt32]$entry.Data.Length)
            $writer.Write([UInt32]$offset)
            $offset += $entry.Data.Length
        }
        foreach ($entry in $iconImages) {
            $writer.Write([byte[]]$entry.Data)
        }
        $writer.Flush()
        [System.IO.File]::WriteAllBytes($ico, $out.ToArray())
    } finally {
        $writer.Dispose()
        $out.Dispose()
    }
} finally {
    $image.Dispose()
    $inputStream.Dispose()
}
Write-Host "[Tokyo World] Brand icons created: $ico and $png"
Write-Host "[Tokyo World] Included sizes: 16,24,32,48,64,128,256"
