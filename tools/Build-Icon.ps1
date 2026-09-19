param([string]$OutputDirectory = (Join-Path $PSScriptRoot '..\SleepGuard\Assets'))

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$images = [System.Collections.Generic.List[byte[]]]::new()

foreach ($size in $sizes) {
    $canvas = [System.Drawing.Bitmap]::new(1024, 1024)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.ScaleTransform(4, 4)
    $shield = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $shield.AddBezier(128, 16, 156, 36, 190, 47, 221, 51)
    $shield.AddLine(221, 51, 216, 129)
    $shield.AddBezier(216, 129, 212, 181, 176, 217, 128, 240)
    $shield.AddBezier(128, 240, 80, 217, 44, 181, 40, 129)
    $shield.AddLine(40, 129, 35, 51)
    $shield.AddBezier(35, 51, 66, 47, 100, 36, 128, 16)
    $shield.CloseFigure()
    $gradient = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.Point]::new(54, 34), [System.Drawing.Point]::new(194, 227),
        [System.Drawing.ColorTranslator]::FromHtml('#27D4BE'),
        [System.Drawing.ColorTranslator]::FromHtml('#3459BC'))
    $graphics.FillPath($gradient, $shield)
    $outline = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(170, 135, 247, 226), 4)
    $graphics.DrawPath($outline, $shield)

    $outer = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $outer.AddEllipse(75, 65, 111, 111)
    $cutout = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $cutout.AddEllipse(111, 48, 103, 103)
    $moon = [System.Drawing.Region]::new($outer)
    $moon.Exclude($cutout)
    $white = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#F4FFFC'))
    $graphics.FillRegion($white, $moon)
    $star = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(174, 76), [System.Drawing.PointF]::new(179, 88),
        [System.Drawing.PointF]::new(191, 93), [System.Drawing.PointF]::new(179, 98),
        [System.Drawing.PointF]::new(174, 110), [System.Drawing.PointF]::new(169, 98),
        [System.Drawing.PointF]::new(157, 93), [System.Drawing.PointF]::new(169, 88))
    $graphics.FillPolygon($white, $star)

    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $resize = [System.Drawing.Graphics]::FromImage($bitmap)
    $resize.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $resize.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $resize.DrawImage($canvas, 0, 0, $size, $size)
    $stream = [System.IO.MemoryStream]::new()
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $images.Add($stream.ToArray())
    if ($size -eq 256) { $bitmap.Save((Join-Path $OutputDirectory 'SleepGuard.png'), [System.Drawing.Imaging.ImageFormat]::Png) }
    $stream.Dispose()
    $resize.Dispose()
    $bitmap.Dispose()
    $white.Dispose()
    $moon.Dispose()
    $cutout.Dispose()
    $outer.Dispose()
    $outline.Dispose()
    $gradient.Dispose()
    $shield.Dispose()
    $graphics.Dispose()
    $canvas.Dispose()
}

$iconPath = Join-Path $OutputDirectory 'SleepGuard.ico'
$file = [System.IO.File]::Create($iconPath)
$writer = [System.IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($index = 0; $index -lt $sizes.Count; $index++) {
        $dimension = if ($sizes[$index] -eq 256) { 0 } else { $sizes[$index] }
        $writer.Write([byte]$dimension)
        $writer.Write([byte]$dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$images[$index].Length)
        $writer.Write([uint32]$offset)
        $offset += $images[$index].Length
    }
    foreach ($bytes in $images) { $writer.Write($bytes) }
}
finally { $writer.Dispose(); $file.Dispose() }
Write-Output "Created $iconPath with $($sizes.Count) sizes."
