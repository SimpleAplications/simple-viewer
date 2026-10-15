# Draws the app's icon at every size the Microsoft Store package asks for, into
# installer\store\Assets, and the 300 x 300 and 1080 x 1080 logos for the Store listing into store\images.
#   powershell -ExecutionPolicy Bypass -File tools\make_store_assets.ps1
# The drawing is the same as app.ico's: a black tile with a picture frame around a
# "step forward" mark; below 48 pixels only the mark, as large as fits.
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName PresentationCore, WindowsBase
$root = Split-Path $PSScriptRoot
$assets = Join-Path $root "installer\store\Assets"
$images = Join-Path $root "store\images"
New-Item -ItemType Directory -Force $assets, $images | Out-Null

# The icon at $iconSize pixels, centred on a transparent square of $size pixels.
function Save-Icon([string]$path, [int]$size, [int]$iconSize) {
    $white = [System.Windows.Media.Brushes]::White
    $black = [System.Windows.Media.Brushes]::Black
    $visual = New-Object System.Windows.Media.DrawingVisual
    $dc = $visual.RenderOpen()
    $offset = ($size - $iconSize) / 2.0
    $dc.PushTransform((New-Object System.Windows.Media.TranslateTransform $offset, $offset))
    $dc.PushTransform((New-Object System.Windows.Media.ScaleTransform ($iconSize / 256.0), ($iconSize / 256.0)))
    $dc.DrawRoundedRectangle($black, $null, (New-Object System.Windows.Rect 0, 0, 256, 256), 56, 56)
    if ($iconSize -ge 48) {
        $pen = New-Object System.Windows.Media.Pen $white, 12
        $dc.DrawRoundedRectangle($null, $pen, (New-Object System.Windows.Rect 38, 38, 180, 180), 30, 30)
        $tri = @(92, 86, 92, 170, 158, 128); $bar = @(168, 86, 18, 84)
    } else {
        $tri = @(58, 52, 58, 204, 166, 128); $bar = @(180, 52, 30, 152)
    }
    $geometry = New-Object System.Windows.Media.StreamGeometry
    $ctx = $geometry.Open()
    $ctx.BeginFigure((New-Object System.Windows.Point $tri[0], $tri[1]), $true, $true)
    $ctx.LineTo((New-Object System.Windows.Point $tri[2], $tri[3]), $true, $true)
    $ctx.LineTo((New-Object System.Windows.Point $tri[4], $tri[5]), $true, $true)
    $ctx.Close()
    $dc.DrawGeometry($white, $null, $geometry)
    $dc.DrawRoundedRectangle($white, $null, (New-Object System.Windows.Rect $bar[0], $bar[1], $bar[2], $bar[3]), 4, 4)
    $dc.Pop(); $dc.Pop()
    $dc.Close()
    $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $size, $size, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [System.IO.File]::Create($path)
    try { $encoder.Save($stream) } finally { $stream.Close() }
}

$scales = @{ 100 = 1.0; 125 = 1.25; 150 = 1.5; 200 = 2.0; 400 = 4.0 }

# App list, taskbar and Start: the tile is part of the icon, so it fills the square.
foreach ($scale in $scales.Keys) {
    $size = [int][math]::Round(44 * $scales[$scale])
    Save-Icon (Join-Path $assets "Square44x44Logo.scale-$scale.png") $size ([int][math]::Round($size * 0.92))
}
foreach ($target in 16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 256) {
    Save-Icon (Join-Path $assets "Square44x44Logo.targetsize-$target.png") $target $target
    Save-Icon (Join-Path $assets "Square44x44Logo.targetsize-${target}_altform-unplated.png") $target $target
}
# Medium tile (Windows 10 Start): the icon in the middle with room around it.
foreach ($scale in $scales.Keys) {
    $size = [int][math]::Round(150 * $scales[$scale])
    Save-Icon (Join-Path $assets "Square150x150Logo.scale-$scale.png") $size ([int][math]::Round($size * 0.6))
}
# The package logo the Store and the installer dialog show.
foreach ($scale in $scales.Keys) {
    $size = [int][math]::Round(50 * $scales[$scale])
    Save-Icon (Join-Path $assets "StoreLogo.scale-$scale.png") $size $size
}
Save-Icon (Join-Path $images "logo-300.png") 300 300
Save-Icon (Join-Path $images "logo-1080.png") 1080 1080
"wrote " + (Get-ChildItem $assets).Count + " files to $assets, and the logos in store\images"
