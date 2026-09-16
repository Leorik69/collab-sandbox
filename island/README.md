# NotifyIsland

WinUI 3 capsule at top edge. Specs: drop/002–004, **drop/007** (Dynamic Island restyle).

```
cd island && dotnet run
```

## 007 (CC pack — DI restyle)

Ref: [Packt Dynamic Island](https://github.com/PacktPublishing/Mastering-WidgetKit-in-SwiftUI-4-iOS-16-with-Dynamic-Island) — host Live Activity only; Live Widget Swift files are **missing** (404). CompactLeading/Trailing / Expanded / Minimal are a WinUI adaptation of the system DI model, not copied widget UI.

- Capsule `#0A0A0A` Solid; text `#F5F5F7`; morph `~280ms` (WinUI mapping).
- Keyline / glow / progress / badge accent **`#FF9F0A`** (CC: FocusTimer-demo orange, not `#0A84FF`).
- Host HWND is chromeless/transparent — only the capsule is visible. Compact↔Expanded morph ~280ms; hover does not shift the top edge; RMB menu stays open.
- `PresentationMode`: Compact | Expanded | Minimal. Hover Compact→Expanded and `HH:MM:SS` in expanded = WinUI additions.
- Fluent instead of SF Symbols. Pulse = scale + opacity, unread only. No idle blue dot.
- RMB: Presentation + existing clock/weather/icons + **Icon Pack** (light / dark / colorful / mono / neon / pastel). Pack switch applies PNG glyphs, clock digits (`Assets/Packs/clock-styles.md`), and pill background immediately — no restart. Default `dark`.

## Portable (без сборки из исходников)

Unpackaged self-contained `win-x64`, **не MSIX** (`island/packaging/MSIX-NOTE.md`). Бинарники в git не лежат.

**Скачать zip с PR:** GitHub → PR Checks / Actions → workflow **Portable win-x64** → Artifacts → `NotifyIsland-portable-win-x64`. Распаковать **всю папку**. Запуск: `Запустить.bat` (сначала `publish\NotifyIsland.exe`, иначе exe рядом) или `NotifyIsland.exe` в каталоге с `resources.pri` — как `island/publish/NotifyIsland.exe`. Не копировать exe отдельно.

**Локально (Windows, нужен VS 2022 + Windows app development; чистый `dotnet` SDK ловит MSB4062):**

```powershell
cd island
powershell -ExecutionPolicy Bypass -File tools\Publish-Portable.ps1
# zip: NotifyIsland-portable-win-x64.zip  |  exe: publish\NotifyIsland.exe
```

## 004 (CC pack)

- Clock C1 Digital Modern (default, colon blink) · C2 Minimal · C3 Seconds+arc. Analog C4 later.
- Weather W1 compact glyph+° · W2 expand on hover. Mock 18°C.
- Icons I1 Fluent · I2 Thin · I3 Filled+Accent when DND/unread.
- Effects E1 Border Glow · E2 Pulse on **Pill** when unread · E3 hover.
