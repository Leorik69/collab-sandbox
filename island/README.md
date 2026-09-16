# NotifyIsland

WinUI 3 capsule at top edge. Specs: drop/002–004, **drop/007** (Dynamic Island restyle).

```
cd island && dotnet run
```

## 007 (CC pack — DI restyle)

Ref: [Packt Dynamic Island](https://github.com/PacktPublishing/Mastering-WidgetKit-in-SwiftUI-4-iOS-16-with-Dynamic-Island) — host Live Activity only; Live Widget Swift files are **missing** (404). CompactLeading/Trailing / Expanded / Minimal are a WinUI adaptation of the system DI model, not copied widget UI.

- Capsule `#0A0A0A` Solid; text `#F5F5F7`; morph `~280ms` (WinUI mapping).
- Keyline / glow / progress / badge accent **`#FF9F0A`** (CC: FocusTimer-demo orange, not `#0A84FF`).
- `PresentationMode`: Compact | Expanded | Minimal. Hover Compact→Expanded and `HH:MM:SS` in expanded = WinUI additions.
- Fluent instead of SF Symbols. Pulse = scale + opacity, unread only. No idle blue dot.
- RMB: Presentation + existing clock/weather/icons. LMB → Win+N (unchanged).

## 004 (CC pack)

- Clock C1 Digital Modern (default, colon blink) · C2 Minimal · C3 Seconds+arc. Analog C4 later.
- Weather W1 compact glyph+° · W2 expand on hover. Mock 18°C.
- Icons I1 Fluent · I2 Thin · I3 Filled+Accent when DND/unread.
- Effects E1 Border Glow · E2 Pulse on **Pill** when unread · E3 hover.
