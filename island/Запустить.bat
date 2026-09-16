@echo off
REM NotifyIsland portable launcher (unpackaged, self-contained, win-x64)
setlocal
cd /d "%~dp0"
if exist "publish\NotifyIsland.exe" (
  start "" "publish\NotifyIsland.exe"
) else if exist "NotifyIsland.exe" (
  start "" "NotifyIsland.exe"
) else (
  echo NotifyIsland.exe not found. Build first:
  echo   powershell -ExecutionPolicy Bypass -File tools\Publish-Portable.ps1
  pause
)
