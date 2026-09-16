# Icon Packs (`island/Assets/Packs/`)

6 паков, в каждом — SVG (исходник) + PNG 64×64 (рантайм) с одинаковыми именами.
Структура пака: `<pack>/Weather/{sun,cloud,rain,snow,storm,fog,thunder}.svg/.png`,
`<pack>/Statuses/{dnd-active,dnd-off,bell-unread,bell-none}.svg/.png`.

## Маппинг

| Файл | Состояние | Примечание |
|---|---|---|
| `Weather/sun` | Clear/Sunny | |
| `Weather/cloud` | Cloud/Overcast | |
| `Weather/rain` | Rain/Drizzle | |
| `Weather/snow` | Snow | |
| `Weather/storm` | Thunderstorm | код использует `storm` |
| `Weather/thunder` | Алиас `storm` | как в PR #6 |
| `Statuses/dnd-active` | DND вкл | приоритетнее unread |
| `Statuses/dnd-off` | DND выкл | контур, про запас |
| `Statuses/bell-unread` | Есть непрочитанные | колокол + точка |
| `Statuses/bell-none` | Нет непрочитанных | |

Рантайм-путь: `ms-appx:///Assets/Packs/<pack>/Weather/sun.png` и т.д.
Фон везде прозрачный, viewBox 64×64.

## Паки

| Пак | Назначение | Палитра |
|---|---|---|
| `light` | Светлая пилюля | тёмные штрихи `#1C1C1E`, акцент `#0A54FF` |
| `dark` | Чёрная пилюля `#0A0A0A` (дефолт 007) | светлые `#F5F5F7`, акцент `#0A84FF` |
| `colorful` | Цветной, реалистичный | солнце `#FFC83C`, капли `#0A84FF`, молния `#FFD60A` |
| `mono` | Монохром под тёмную пилюлю | всё `#F5F5F7`, молния/плашка DND тёмные для читаемости |
| `neon` | Неон с glow | циан `#00F0FF`, маджента `#FF2D95`, лайм `#B6FF00` |
| `pastel` | Пастель | приглушённые тона (солнце `#FFB84D`, колокол `#B9A8EE`) |

## Совместимость с PR #6

PR #6 (`cursor/island-assets-portable-2beb`) кладёт плоские `Assets/Weather/` +
`Assets/Statuses/` + `Assets/README.md`. Паки лежат в `Assets/Packs/` и не
пересекаются с ними ни одним путём. Если PR #6 вмержится позже — конфликтов не будет.

## Подключение

`NotifyIsland.csproj` уже содержит `Content Include="Assets\**\*"` —
паки попадают в publish автоматически. Перегенерация:
`python3 tools/generate-packs.py` (нужен `cairosvg`).
