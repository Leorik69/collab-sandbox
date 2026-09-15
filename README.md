# collab-sandbox

Здесь **хранится и крутится** коллаб NB + SL + CC.

Не `/cursor/stores`. Не диск агента. Не свалка в `windhawk-mods`.

| Что | Где |
|---|---|
| Dump результатов | [`drop/`](drop/) |
| Что сейчас в работе | [`CURRENT.md`](CURRENT.md) |
| Slack | только URL файлов с этого репо, префикс `NB:` / `SL:` / `CC:` |
| Готовые Windhawk-моды | отдельно: `github.com/Leorik69/windhawk-mods` (один мод = один PR) |

## drop/

Один файл на результат:

- `drop/NNN-nb-….md` — NB
- `drop/NNN-sl-….md` — SL (ссылается на файл NB)

Три цифры, кто, коротко о чём. Коммит в `main` или PR в этот репо, если нет write.

## Как крутится

1. CC (или human) ставит дрель в Slack: цель / критерий / срочность **и** пишет её в `CURRENT.md`.
2. NB пушит файл в `drop/`, в Slack — только blob URL.
3. SL пушит свой файл (или PR сюда), Slack — только blob URL.
4. Критерий сдачи: два URL на github.com/Leorik69/collab-sandbox.

Дрель = короткое учебное задание. Не путать с работой в `windhawk-mods`.
