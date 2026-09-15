# NotifyIsland (product)

WinUI 3 Dynamic-Island капсула сверху экрана. Не чёрная полоса на taskbar.

Спеки: `drop/002-cc-island-spec.md`, `drop/003-island-product-spec.md`, `drop/004-island-clock-weather-styles.md`, `drop/004-sl-style-variants.md`.

## Запуск (Windows)

```
cd island
dotnet run
```

Нужен .NET 8 SDK + Windows App SDK / VS workload с Appx packaging (иначе MSB4062 ExpandPriContent).

## Возможности (003)

- **ЛКМ** — центр уведомлений (Win+N). Без `SetTitleBar(Pill)` (клик не глотается).
- **ПКМ** — меню: DND, материал, форма, скругление, прозрачность, акцент, демо-счётчик.
- **Настройки** — `LocalSettings` JSON (`IslandSettings` v2).
- **Реакции** — badge/точка, пульсация при unread, hover, иконка DND.
- Always-on-top, top-center; часы/поиск не перекрываются на всю ширину.

## Стили (004, CC package)

Дефолты: **C1 / W1 / I1 / E2+E3**. C4 Analog — не в этом шипе.

| | Варианты | Default |
|---|---|---|
| Clock | C1 Digital Modern, C2 Digital Minimal, C3 Seconds+arc | C1 |
| Weather | W1 Compact Badge, W2 Expanded (также peek по hover-dwell в W1) | W1 |
| Icons | I1 Fluent, I2 Minimal Line (1.5px Path), I3 Filled Active | I1 |
| Effects | E1 Border Glow (opt-in), E2 Pulse Aura, E3 Hover Highlight | E2+E3 on, E1 off |

Шрифт часов: Segoe UI Variable Display / Segoe Variable (Cascadia — только fallback). Макет и эффекты — ПКМ → Часы / Погода / Иконки / Эффекты.
