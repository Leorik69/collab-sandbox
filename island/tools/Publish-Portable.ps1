# Publish portable self-contained build (Windows, unpackaged WinUI3, win-x64)
# Usage: powershell -ExecutionPolicy Bypass -File tools\Publish-Portable.ps1 [-Zip]
param([switch]$Zip = $true)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
Set-Location (Join-Path $root '')
dotnet publish island\NotifyIsland.csproj -c Release -r win-x64 `
  --self-contained true `
  -p:WindowsAppSDKSelfContained=true `
  -p:WindowsPackageType=None `
  -o island\publish
if ($Zip) {
  $zip = Join-Path $root 'island\NotifyIsland-portable-win-x64.zip'
  if (Test-Path $zip) { Remove-Item $zip }
  Compress-Archive -Path (Join-Path $root 'island\publish\*') -DestinationPath $zip
  Write-Host "OK: $zip"
} else {
  Write-Host 'OK: island\publish\NotifyIsland.exe'
}
