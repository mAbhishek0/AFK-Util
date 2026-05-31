# Converts app.png into a multi-resolution .ico file

$inputPng = "d:\AFK\AfkBot\app.png"
$outputIco = "d:\AFK\AfkBot\app.ico"

if (-not (Test-Path $inputPng)) {
    Write-Error "Source PNG not found: $inputPng"
    exit 1
}

Add-Type -AssemblyName System.Drawing

$srcImg = [System.Drawing.Image]::FromFile($inputPng)
$sizes = @(16, 32, 48, 256)
$pngDataList = @()

foreach ($size in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($srcImg, 0, 0, $size, $size)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()

    $pngDataList += ,$ms.ToArray()
    $ms.Dispose()
}

$srcImg.Dispose()

# Build ICO binary
$count = $sizes.Count
$headerSize = 6
$entrySize = 16
$offset = $headerSize + ($entrySize * $count)

$totalSize = $offset
foreach ($data in $pngDataList) { $totalSize += $data.Length }

$icoBytes = New-Object byte[] $totalSize

# ICONDIR header
$icoBytes[0] = 0; $icoBytes[1] = 0   # reserved
$icoBytes[2] = 1; $icoBytes[3] = 0   # type = icon
$icoBytes[4] = $count -band 0xFF
$icoBytes[5] = ($count -shr 8) -band 0xFF

$dataOffset = $offset

for ($i = 0; $i -lt $count; $i++) {
    $size = $sizes[$i]
    $data = $pngDataList[$i]
    $dataSize = $data.Length
    $idx = $headerSize + ($entrySize * $i)

    $w = if ($size -eq 256) { 0 } else { $size }
    $h = if ($size -eq 256) { 0 } else { $size }

    $icoBytes[$idx + 0] = $w
    $icoBytes[$idx + 1] = $h
    $icoBytes[$idx + 2] = 0    # color count
    $icoBytes[$idx + 3] = 0    # reserved
    $icoBytes[$idx + 4] = 1; $icoBytes[$idx + 5] = 0    # planes
    $icoBytes[$idx + 6] = 32; $icoBytes[$idx + 7] = 0   # bpp

    $icoBytes[$idx + 8]  = $dataSize -band 0xFF
    $icoBytes[$idx + 9]  = ($dataSize -shr 8) -band 0xFF
    $icoBytes[$idx + 10] = ($dataSize -shr 16) -band 0xFF
    $icoBytes[$idx + 11] = ($dataSize -shr 24) -band 0xFF

    $icoBytes[$idx + 12] = $dataOffset -band 0xFF
    $icoBytes[$idx + 13] = ($dataOffset -shr 8) -band 0xFF
    $icoBytes[$idx + 14] = ($dataOffset -shr 16) -band 0xFF
    $icoBytes[$idx + 15] = ($dataOffset -shr 24) -band 0xFF

    [System.Array]::Copy($data, 0, $icoBytes, $dataOffset, $dataSize)
    $dataOffset += $dataSize
}

[System.IO.File]::WriteAllBytes($outputIco, $icoBytes)
Write-Host "Created $outputIco"
