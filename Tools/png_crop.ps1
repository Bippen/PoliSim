param([Parameter(Mandatory = $true)][string]$In, [Parameter(Mandatory = $true)][string]$Out, [int]$X, [int]$Y, [int]$W, [int]$H, [int]$Scale = 1)
# A CROP OF A REAL FRAME, NATIVE OR SCALED BY NEAREST NEIGHBOUR (2026-09-29, COMPLETED.md s663: Design's D24 ask of Code, 3).
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/png_crop.ps1 -In frame.png -Out crop.png -X 80 -Y 296 -W 640 -H 30 [-Scale 4]
# Nearest neighbour with half-pixel offset: every source pixel becomes an exact Scale x Scale block - no smoothing, so what Design judges at
# 4x is the 16 px glyph as the frame drew it. Prints the output's size and its sha256.
# ASCII only: PowerShell 5.1 reads a BOM-less script as ANSI.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Bitmap]::FromFile((Resolve-Path $In).Path)
try {
  if ($X -lt 0 -or $Y -lt 0 -or $W -le 0 -or $H -le 0 -or ($X + $W) -gt $src.Width -or ($Y + $H) -gt $src.Height) { "CROP: REFUSED - $X,$Y ${W}x$H is outside the $($src.Width)x$($src.Height) frame"; exit 2 }
  $dst = New-Object System.Drawing.Bitmap ($W * $Scale), ($H * $Scale), ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [System.Drawing.Graphics]::FromImage($dst)
  $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
  $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
  $g.DrawImage($src, (New-Object System.Drawing.Rectangle 0, 0, ($W * $Scale), ($H * $Scale)), (New-Object System.Drawing.Rectangle $X, $Y, $W, $H), [System.Drawing.GraphicsUnit]::Pixel)
  $g.Dispose()
  $full = [IO.Path]::GetFullPath($Out)
  $dst.Save($full, [System.Drawing.Imaging.ImageFormat]::Png)
  $dst.Dispose()
  "CROP: $Out $($W * $Scale)x$($H * $Scale) sha256 $((Get-FileHash -Algorithm SHA256 $full).Hash.ToLowerInvariant())"
} finally { $src.Dispose() }
