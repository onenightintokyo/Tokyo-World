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
    [BitConverter]::ToString($bytes, 0, 8) -ne '89-50-4E-47-0D-0A-1A-0A') {
    throw 'TW icon source is not a valid PNG header.'
}
[System.IO.File]::WriteAllBytes($png, $bytes)

# v1.7 Crimson Tokyo: replace the tiny embedded PNG with a crisp 1024 px
# illustration made from vector GDI+ primitives. The ICO generator below uses
# this true-resolution source for every Windows icon size.
$canvas = [System.Drawing.Bitmap]::new(1024, 1024)
$g = [System.Drawing.Graphics]::FromImage($canvas)
try {
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $bg = [System.Drawing.SolidBrush]::new(
        [System.Drawing.Color]::FromArgb(255, 16, 15, 34))
    $shell = [System.Drawing.SolidBrush]::new(
        [System.Drawing.Color]::FromArgb(255, 35, 17, 48))
    $pink = [System.Drawing.SolidBrush]::new(
        [System.Drawing.Color]::FromArgb(255, 255, 65, 145))
    $dark = [System.Drawing.SolidBrush]::new(
        [System.Drawing.Color]::FromArgb(255, 20, 13, 37))
    $muted = [System.Drawing.SolidBrush]::new(
        [System.Drawing.Color]::FromArgb(255, 145, 44, 99))
    $white = [System.Drawing.SolidBrush]::new(
        [System.Drawing.Color]::FromArgb(255, 252, 248, 255))
    $glow = [System.Drawing.Pen]::new(
        [System.Drawing.Color]::FromArgb(110, 255, 67, 147), 26)
    $ring = [System.Drawing.Pen]::new(
        [System.Drawing.Color]::FromArgb(255, 255, 75, 156), 16)
    $cyan = [System.Drawing.Pen]::new(
        [System.Drawing.Color]::FromArgb(255, 91, 224, 230), 13)
    $gate = [System.Drawing.Pen]::new(
        [System.Drawing.Color]::FromArgb(255, 241, 54, 128), 20)
    $font = [System.Drawing.Font]::new("Segoe UI Black", 268,
        [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $tiny = [System.Drawing.Font]::new("Segoe UI", 35,
        [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    try {
        $g.FillEllipse($bg, 36, 36, 952, 952)
        $g.FillEllipse($shell, 73, 73, 878, 878)
        $g.DrawEllipse($glow, 65, 65, 893, 893)
        $g.DrawEllipse($ring, 74, 74, 875, 875)
        # Crimson sun (far behind the main TW monogram).
        $g.FillEllipse($pink, 216, 155, 590, 590)
        $g.FillEllipse($muted, 254, 204, 512, 512)
        $g.FillEllipse($dark, 298, 271, 424, 424)

        $rnd = [System.Random]::new(717)
        # Layer of tiny Tokyo rooftop silhouettes.
        for ($x = 172; $x -le 852; ) {
            $w = $rnd.Next(22, 60)
            $h = $rnd.Next(580, 704)
            $g.FillRectangle($dark, $x, $h, $w, 760 - $h)
            $x += $w + $rnd.Next(5, 20)
        }
        # Torii gate is legible even on compact taskbar thumbnails.
        $g.DrawLine($gate, 269, 674, 269, 860)
        $g.DrawLine($gate, 751, 674, 751, 860)
        $g.DrawLine($gate, 215, 662, 802, 662)
        $g.DrawLine($gate, 254, 707, 762, 707)
        $g.DrawLine($cyan, 313, 858, 705, 858)

        # High-resolution, readable white TW monogram with pink depth.
        $g.DrawString("TW", $font, $pink, [single]210, [single]322)
        $g.DrawString("TW", $font, $white, [single]197, [single]311)
        $g.DrawString("TOKYO WORLD", $tiny, $white, [single]358, [single]903)

        for ($i = 0; $i -lt 24; $i++) {
            $xx = $rnd.Next(118, 909)
            $yy = $rnd.Next(110, 845)
            if ($xx -gt 320 -and $xx -lt 721 -and $yy -gt 330 -and $yy -lt 585) {
                continue
            }
            $g.FillEllipse($pink, $xx, $yy, $rnd.Next(6, 14), $rnd.Next(9, 22))
        }
    } finally {
        foreach ($obj in @($bg, $shell, $pink, $dark, $muted, $white,
                $glow, $ring, $cyan, $gate, $font, $tiny)) {
            $obj.Dispose()
        }
    }
    $canvas.Save($png, [System.Drawing.Imaging.ImageFormat]::Png)
} finally {
    $g.Dispose()
    $canvas.Dispose()
}
[byte[]]$bytes = [System.IO.File]::ReadAllBytes($png)
Write-Host "[Tokyo World] Generated HQ Crimson TW artwork: 1024x1024"

$inputStream = [System.IO.MemoryStream]::new($bytes)
$image = [System.Drawing.Image]::FromStream($inputStream)
$iconImages = New-Object 'System.Collections.Generic.List[object]'
try {
    foreach ($size in @(16, 24, 32, 48, 64, 128, 256)) {
        $bitmap = [System.Drawing.Bitmap]::new($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.DrawImage($image, 0, 0, $size, $size)
            $imageStream = [System.IO.MemoryStream]::new()
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

    $out = [System.IO.MemoryStream]::new()
    $writer = [System.IO.BinaryWriter]::new($out)
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
