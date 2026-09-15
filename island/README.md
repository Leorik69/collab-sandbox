# NotifyIsland

WinUI 3 capsule at top edge. Specs: drop/002, drop/003, drop/004.

```
cd island && dotnet run
```

## 004 (CC pack)

- Clock C1 Digital Modern (default, colon blink) · C2 Minimal · C3 Seconds+arc. Analog C4 later.
- Weather W1 compact glyph+° · W2 expand on hover. Mock 18°C.
- Icons I1 Fluent · I2 Thin · I3 Filled+Accent when DND/unread.
- Effects E1 Border Glow (off) · E2 Pulse on **Pill** when unread · E3 hover.
- RMB: clock/weather/icons/temp/glow/pulse. LMB → Win+N (no unread bump).

## 005 — Assets + portable (WinUI3 unpackaged self-contained, win-x64)

Assets: `Assets/Weather/` (sun/cloud/rain/snow/storm+thunder/fog, PNG+SVG) и
`Assets/Statuses/` (dnd-active/bell-unread/bell-none), детали — `Assets/README.md`.
ПКМ → `Icon source` → `FontIcon` (системные глифы, default) / `LocalAsset` (пак).
Настройки сохраняются в `LocalSettings`.

### Собрать портатив (на Windows)
```
cd island
dotnet publish NotifyIsland.csproj -c Release -r win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true -p:WindowsPackageType=None -o publish
```
или скриптом (сборка + zip `NotifyIsland-portable-win-x64.zip`):
```
powershell -ExecutionPolicy Bypass -File tools\Publish-Portable.ps1
```

### Запустить
- дабл-клик `Запустить.bat` (или `Start-Portable.ps1`) рядом с `publish\NotifyIsland.exe`;
- распакованный zip запускается так же — установка не нужна.

MSIX: пока только портатив, причины — `packaging/MSIX-NOTE.md`.
