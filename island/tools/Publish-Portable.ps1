# Publish portable self-contained build (Windows, unpackaged WinUI3, win-x64)
# Usage: powershell -ExecutionPolicy Bypass -File tools\Publish-Portable.ps1 [-Zip]
param([switch]$Zip = $true)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
Set-Location $root
dotnet publish NotifyIsland.csproj -c Release -r win-x64 `
  --self-contained true `
  -p:WindowsAppSDKSelfContained=true `
  -p:WindowsPackageType=None `
  -o publish
if ($Zip) {
  $zipPath = Join-Path $root 'NotifyIsland-portable-win-x64.zip'
  if (Test-Path $zipPath) { Remove-Item $zipPath }
  Compress-Archive -Path (Join-Path $root 'publish\*') -DestinationPath $zipPath
  Write-Host "OK: $zipPath"
} else {
  Write-Host 'OK: publish\NotifyIsland.exe'
}
