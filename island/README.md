# NotifyIsland (product)

WinUI 3 Dynamic-Island капсула сверху экрана. Не чёрная полоса на taskbar.

Спеки: `drop/002-cc-island-spec.md`, `drop/003-island-product-spec.md`.

## Запуск (Windows)

```
cd island
dotnet run
```

Нужен .NET 8 SDK + Windows App SDK / VS workload с Appx packaging (иначе MSB4062 ExpandPriContent).

## Возможности (003)

- **ЛКМ** — центр уведомлений (Win+N). Без `SetTitleBar(Pill)` (клик не глотается).
- **ПКМ** — меню: DND, материал (Solid/Acrylic/Mica), форма, скругление, прозрачность, акцент, демо-счётчик.
- **Настройки** — `LocalSettings` JSON (`IslandSettings`).
- **Реакции** — badge/точка, пульсация при unread, hover scale, иконка DND.
- Always-on-top, ~176×40 top-center, часы/поиск не перекрываются на всю ширину.
