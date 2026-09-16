# Assets (drop/005, стили 004: C1 W1 I1 E2+E3)

Минимальный пак. PNG — основной рантайм (`ms-appx:///Assets/...`),
SVG — исходники/будущий `SvgImageSource`.

## Weather (`Weather/`)
| Файл | Состояние |
|---|---|
| `sun.png/.svg` | Clear/Sunny (мок W1/W2: всегда Clear, 18°C) |
| `cloud.png/.svg` | Cloud/Overcast |
| `rain.png/.svg` | Rain/Drizzle |
| `snow.png/.svg` | Snow |
| `storm.png` + `thunder.png` (+`.svg`) | Thunderstorm (алиас: оба имени, код использует `storm`) |
| `fog.png/.svg` | Fog/Mist |

64×64, прозрачный фон, светлые заливки — читаются на тёмной пилюле
(`#1C1C1E`, C1/W1 по умолчанию) и на светлой (`#F2F2F7`).

## Statuses (`Statuses/`)
| Файл | Состояние |
|---|---|
| `dnd-active.png/.svg` | DND вкл (красный + белая полоса) |
| `dnd-off.png/.svg` | DND выкл (контур) — сейчас не используется, для будущего тумблера |
| `bell-unread.png/.svg` | Есть непрочитанные (колокол + синяя точка I3) |
| `bell-none.png/.svg` | Нет непрочитанных |

Логика: DND приоритетнее unread (см. `ApplyStateIcon`).

## Подключение
`NotifyIsland.csproj` → `Content Include="Assets\**\*"`, `PreserveNewest`.
Переключатель: ПКМ → `Icon source` → `FontIcon` / `LocalAsset` (сохраняется в `LocalSettings`).
