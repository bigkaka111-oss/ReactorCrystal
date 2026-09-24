param([string]$Version = '0.1.0')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$buildRoot = Join-Path $projectRoot 'Builds/Contour-Windows'
$releaseRoot = Join-Path $projectRoot 'Release'
foreach ($relative in @('Contour.exe', 'UnityPlayer.dll', 'Contour_Data/globalgamemanagers')) {
    if (-not (Test-Path -LiteralPath (Join-Path $buildRoot $relative))) {
        throw "Incomplete Windows build: $relative"
    }
}
Copy-Item -LiteralPath (Join-Path $releaseRoot 'README-RU.txt') -Destination $buildRoot -Force
Copy-Item -LiteralPath (Join-Path $releaseRoot 'LiberationSans-OFL.txt') -Destination $buildRoot -Force
$archive = Join-Path $releaseRoot "Contour-$Version-Windows-x64.zip"
Compress-Archive -Path (Join-Path $buildRoot '*') -DestinationPath $archive -CompressionLevel Optimal -Force
$hash = Get-FileHash -LiteralPath $archive -Algorithm SHA256
"$($hash.Hash.ToLowerInvariant())  $([System.IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath (Join-Path $releaseRoot 'SHA256SUMS.txt') -Encoding ascii
Get-Item -LiteralPath $archive | Select-Object FullName, Length
