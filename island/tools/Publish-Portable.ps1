# Publish portable self-contained build (Windows, unpackaged WinUI3, win-x64)
# Usage: powershell -ExecutionPolicy Bypass -File tools\Publish-Portable.ps1 [-Zip]
# Prefer VS MSBuild: `dotnet publish` hits MSB4062 ExpandPriContent (Appx tasks missing from SDK).
#
# After unzip: run Запустить.bat (or NotifyIsland.exe) in the extracted folder —
# same directory as resources.pri, Microsoft.ui.xaml.dll, Bootstrap.dll.
# Equivalent launchable tree: island\publish\NotifyIsland.exe (not a lone copied exe).
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

function Ensure-AppPri {
  param([string]$Root, [string]$PublishDir)
  $dest = Join-Path $PublishDir 'resources.pri'
  $named = Join-Path $PublishDir 'NotifyIsland.pri'
  if (Test-Path -LiteralPath $dest) { return }
  if (Test-Path -LiteralPath $named) {
    Copy-Item -LiteralPath $named -Destination $dest -Force
    return
  }

  $hits = @()
  foreach ($dir in @((Join-Path $Root 'bin'), (Join-Path $Root 'obj'))) {
    if (-not (Test-Path -LiteralPath $dir)) { continue }
    $hits += @(Get-ChildItem -LiteralPath $dir -Recurse -File -ErrorAction SilentlyContinue |
      Where-Object { $_.Name -eq 'resources.pri' -or $_.Name -eq 'NotifyIsland.pri' })
  }
  if (-not $hits -or $hits.Count -eq 0) {
    throw @'
Unpackaged publish is missing the app PRI (resources.pri / NotifyIsland.pri).
Build generates it into bin/, but Publish often omits it (WindowsAppSDK #3451/#6720).
Without it, unzipped NotifyIsland.exe APPCRASHes: Microsoft.UI.Xaml.dll 0xc000027b + combase 0x80004005.
EnableMsixTooling/ExpandPriContent did not produce an app PRI.
'@
  }
  $src = $hits | Sort-Object LastWriteTime -Descending | Select-Object -First 1
  Copy-Item -LiteralPath $src.FullName -Destination $dest -Force
  Write-Host "Copied app PRI from $($src.FullName) -> resources.pri"
}

function Assert-PortablePayload {
  param([string]$PublishDir)
  $required = @(
    'NotifyIsland.exe',
    'resources.pri',
    'Microsoft.WindowsAppRuntime.Bootstrap.dll',
    'Microsoft.ui.xaml.dll',
    'Microsoft.WindowsAppRuntime.dll',
    'coreclr.dll'
  )
  $missing = @()
  foreach ($name in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $PublishDir $name))) { $missing += $name }
  }
  if ($missing.Count) {
    throw "Incomplete portable payload in ${PublishDir}: missing $($missing -join ', ')"
  }
}

function Compress-PublishDir {
  param([string]$SourceDir, [string]$DestinationZip)
  # UTF-8 entries: Windows PowerShell 5.1 Compress-Archive mangles Запустить.bat.
  Add-Type -AssemblyName System.IO.Compression
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  if (Test-Path -LiteralPath $DestinationZip) { Remove-Item -LiteralPath $DestinationZip -Force }
  $utf8 = New-Object System.Text.UTF8Encoding $false
  $archive = [System.IO.Compression.ZipFile]::Open(
    $DestinationZip,
    [System.IO.Compression.ZipArchiveMode]::Create,
    $utf8)
  try {
    $rootFull = [System.IO.Path]::GetFullPath($SourceDir).TrimEnd('\')
    Get-ChildItem -LiteralPath $rootFull -Recurse -File | ForEach-Object {
      $rel = $_.FullName.Substring($rootFull.Length).TrimStart('\').Replace('\', '/')
      [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
        $archive,
        $_.FullName,
        $rel,
        [System.IO.Compression.CompressionLevel]::Optimal)
    }
  } finally {
    $archive.Dispose()
  }
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
  /p:EnableMsixTooling=true `
  /p:GenerateAppxPackageOnBuild=false `
  /p:PublishTrimmed=false `
  /p:PublishSingleFile=false `
  /p:PublishDir="$publishDir\"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Ensure-AppPri -Root $root -PublishDir $publishDir

# Zip is publish files at root — copy one-click launchers beside the exe.
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

Assert-PortablePayload -PublishDir $publishDir

if ($Zip) {
  $zipPath = Join-Path $root 'NotifyIsland-portable-win-x64.zip'
  if (Test-Path $zipPath) { Remove-Item $zipPath }
  Compress-PublishDir -SourceDir $publishDir -DestinationZip $zipPath
  Write-Host "OK: $zipPath"
} else {
  Write-Host 'OK: publish\NotifyIsland.exe'
}
