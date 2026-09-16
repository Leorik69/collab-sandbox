# 007 — NotifyIsland: restyle под Dynamic Island (Packt) — LOCKED

CC: пакет утверждён. Код только в `island/`.  
Референс: [PacktPublishing/Mastering-WidgetKit-in-SwiftUI-4-iOS-16-with-Dynamic-Island](https://github.com/PacktPublishing/Mastering-WidgetKit-in-SwiftUI-4-iOS-16-with-Dynamic-Island) — **не клонировать**.

---

## 1. Packt на диске vs 404

**Есть (host Live Activity, не Island UI):**

- FocusTimer: `timerName` + `ContentState.endTime`; `Activity.request` / `end`.
- PizzaOrdering: `orderNumber` / `orderedItem` + `ContentState.status` (`received → inProgress → inOven → onTheWay`; raw = **SF Symbols**); `request` / `update` / `end`.
- `ContentView` показывает `PizzaOrderView()`.

**Нет в published tree (есть в pbxproj, файлы 404):**

`FocusTimerLiveWidget.swift`, `FocusTimerEntryView.swift`, `PizzaDeliveryWidget.swift`, `DevTechieWidgets/*`.

SwiftUI-регионы `compactLeading` / `compactTrailing` / `expanded` / `minimal` **не скопированы** из Packt — исходников нет. WinUI-лейаут — адаптация **системной модели DI** (книга) + host-паттернов выше, с именами регионов как у системы, не как цитата из отсутствующих файлов.

---

## 2. WinUI mapping — не из Packt tree

Чисел/цветов/layout острова в published widget UI нет. Помечено явно:

| Значение | Роль | Источник |
|----------|------|----------|
| `#0A0A0A` | фон капсулы (solid near-black) | **WinUI mapping**, не Packt |
| `#F5F5F7` | текст на чёрном | **WinUI mapping**, не Packt |
| `~280ms` | morph Width/Height (и окна) | **WinUI mapping**, не Packt |
| `HH:MM:SS` в Expanded | секунды в раскрытой капсуле | **WinUI addition** |
| hover / PointerEntered | триггер Compact→Expanded | **WinUI addition** |
| Fluent `FontIcon` | вместо SF Symbols | **WinUI adaptation** |

---

## 3. CC decision — оранжевый keyline (не синий)

FocusTimer-demo / iOS system orange. В published Packt **нет** hex ProgressView (Live Widget 404). Выбран один WinUI hex:

**`#FF9F0A`** — iOS system orange (не `#0A84FF`).

Обязательно этим цветом:

- keyline (`Pill.BorderBrush`)
- `keylineGlow` (`GlowBorder`)
- Progress accent (`MinuteArc`)
- active accents (badge, filled glyph при unread/DND)

---

## 4. PresentationMode (locked)

`Compact | Expanded | Minimal` — DI-like презентация (системная модель, не файл из Packt):

| Mode | Layout | Размер (WinUI mapping) |
|------|--------|------------------------|
| **Compact** (default) | `CompactLeading` = часы `HH:MM`; `CompactTrailing` = погода ° + Fluent glyph. Hover → morph Expanded (**WinUI addition**). | ~176×32 |
| **Expanded** | те же leading/trailing + `ExpandedLeading` (desc) / `ExpandedTrailing` (feels/range). Часы могут быть `HH:MM:SS` (**WinUI addition**). Не overlay, прячущий MainRow. | ~248×52 |
| **Minimal** | только leading (часы), trailing скрыт, без второй строки. | ~120×28 |

Idle: чёрная капсула, **без** синей точки. Pulse + opacity fade только при unread/activity (как Live Activity present). Morph: Width/Height капсулы **и** окна, `CornerRadius = Height/2`, `~280ms`.

**Defaults:** `BackgroundHex=#0A0A0A`, `Shape=Capsule`, `Material=Solid`, `AccentHex=#FF9F0A`, `PresentationMode=Compact`, `BorderGlow` визуально = orange keyline (тонкая обводка всегда; extra glow — существующий toggle). LocalSettings API / Win+N **не менять**.

---

## 5. Вне скоупа

Analog clock, новые weather API, ActivityKit, клон Packt, пицца/focus timer как фичи, синий `#0A84FF` на keyline/accent.
