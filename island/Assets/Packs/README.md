# Icon Packs (`island/Assets/Packs/`)

6 паков как перекраски базы Microsoft Fluent UI System Icons (MIT, см.
`tools/FLUENT-LICENSE`; исходники базы — `tools/fluent-base/`).
Стиль — тонкая современная геометрия Fluent, ничего рисованного.
В каждом паке — SVG (исходник) + PNG 64×64 (рантайм) с одинаковыми именами.
Структура пака: `<pack>/Weather/{sun,cloud,rain,snow,storm,fog,thunder}.svg/.png`,
`<pack>/Statuses/{dnd-active,dnd-off,bell-unread,bell-none}.svg/.png`.

## Маппинг на Fluent-базу (24 Regular)

| Файл | Fluent-исходник | Состояние |
|---|---|---|
| `Weather/sun` | Weather Sunny | Clear/Sunny |
| `Weather/cloud` | Weather Cloudy | Cloud/Overcast |
| `Weather/rain` | Weather Rain | Rain/Drizzle |
| `Weather/snow` | Weather Snow | Snow |
| `Weather/storm` | Weather Thunderstorm | Thunderstorm, код использует `storm` |
| `Weather/thunder` | Алиас `storm` | как в PR #6 |
| `Statuses/dnd-active` | Alert Snooze (колокол + snooze = DND вкл) | DND вкл, приоритетнее unread |
| `Statuses/dnd-off` | Alert Off (перечёркнутый колокол) | DND выкл, про запас |
| `Statuses/bell-unread` | Alert + бейдж-точка (композиция) | Есть непрочитанные |
| `Statuses/bell-none` | Alert | Нет непрочитанных |

Адаптации (помечены): бейдж-точка дорисована; в `mono` у точки тёмное кольцо-зазор
(`#0A0A0A`), иначе сливается; в `neon` добавлен SVG glow-фильтр.
База масштабирована 24→56px с полями 4px на холсте 64×64.

Рантайм-путь: `ms-appx:///Assets/Packs/<pack>/Weather/sun.png` и т.д.
Фон везде прозрачный, viewBox 64×64.

## Паки (перекраски одной базы)

| Пак | Назначение |
|---|---|
| `light` | Светлая пилюля: glyph `#1C1C1E`, точка `#0A54FF` (кольцо `#FFFFFF`) |
| `dark` | Чёрная пилюля `#0A0A0A` (дефолт 007): glyph `#F5F5F7`, точка `#0A84FF` |
| `colorful` | Поющевые тинты: солнце `#FFB340`, дождь `#38BDF8`, гроза `#FACC15`, DND `#FF453A` |
| `mono` | Монохром под тёмную пилюлю: всё `#F5F5F7` + тёмное кольцо точки |
| `neon` | Неон + glow: циан `#00F0FF`, маджента `#FF2D95`, лайм `#B6FF00` |
| `pastel` | Пастель: солнце `#E8B84B`, колокол `#B9A8EE`, точка `#8AB8F5` |

## Совместимость с PR #6

PR #6 (`cursor/island-assets-portable-2beb`) кладёт плоские `Assets/Weather/` +
`Assets/Statuses/` + `Assets/README.md`. Паки лежат в `Assets/Packs/` и не
пересекаются с ними ни одним путём. Если PR #6 вмержится позже — конфликтов не будет.

## Подключение

`NotifyIsland.csproj` содержит `Content Include="Assets\**\*"` (та же строка, что в
PR #6 — если PR #6 вмержится первым, ханк csproj при накате патча пропустить).
Перегенерация: `python3 tools/generate-packs.py` (нужен `cairosvg`).
