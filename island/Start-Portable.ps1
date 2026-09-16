# NotifyIsland portable launcher (unpackaged, self-contained, win-x64)
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$candidates = @(
  (Join-Path $root 'publish\NotifyIsland.exe'),
  (Join-Path $root 'NotifyIsland.exe')
)
foreach ($exe in $candidates) {
  if (Test-Path $exe) { Start-Process $exe; exit 0 }
}
Write-Host "NotifyIsland.exe not found. Build first:"
Write-Host "  powershell -ExecutionPolicy Bypass -File tools\Publish-Portable.ps1"
exit 1
