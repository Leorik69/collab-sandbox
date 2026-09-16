# Publish portable self-contained build (Windows, unpackaged WinUI3, win-x64)
# Usage: powershell -ExecutionPolicy Bypass -File tools\Publish-Portable.ps1 [-Zip]
# Prefer VS MSBuild: `dotnet publish` hits MSB4062 ExpandPriContent (Appx tasks missing from SDK).
param([switch]$Zip = $true)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
Set-Location $root

function Find-MSBuild {
  $cmd = Get-Command msbuild -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Source }
  $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
  if (-not (Test-Path $vswhere)) { return $null }
  $found = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
    Select-Object -First 1
  if ($found -and (Test-Path $found)) { return $found }
  return $null
}

$msbuild = Find-MSBuild
if (-not $msbuild) {
  throw 'MSBuild not found. Install VS 2022 with Windows app development (dotnet SDK alone hits MSB4062).'
}

$publishDir = Join-Path $root 'publish'
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

& $msbuild NotifyIsland.csproj /restore /t:Publish /v:m `
  /p:Configuration=Release `
  /p:RuntimeIdentifier=win-x64 `
  /p:SelfContained=true `
  /p:WindowsAppSDKSelfContained=true `
  /p:WindowsPackageType=None `
  /p:GenerateAppxPackageOnBuild=false `
  /p:PublishDir="$publishDir\"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Zip is publish\* at root — copy one-click launchers beside the exe.
$launchers = @(
  (Join-Path $root 'Запустить.bat'),
  (Join-Path $root 'Start-Portable.ps1')
)
foreach ($launcher in $launchers) {
  if (-not (Test-Path -LiteralPath $launcher)) {
    throw "Portable launcher missing: $launcher"
  }
  Copy-Item -LiteralPath $launcher -Destination $publishDir -Force
}

if ($Zip) {
  $zipPath = Join-Path $root 'NotifyIsland-portable-win-x64.zip'
  if (Test-Path $zipPath) { Remove-Item $zipPath }
  Compress-Archive -Path (Join-Path $root 'publish\*') -DestinationPath $zipPath
  Write-Host "OK: $zipPath"
} else {
  Write-Host 'OK: publish\NotifyIsland.exe'
}
