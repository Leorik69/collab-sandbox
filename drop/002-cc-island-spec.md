# 002 — taskbar notify island (human)

Human: хотел PowerToys Command Palette extension + «бровку» как на iPhone на доке; клик → центр уведомлений.

## Честно

Расширение палитры команд PowerToys **не рисует** виджет на панели задач. Палитра = поиск/команды.
Остров на доке = отдельный UI в Explorer (Windhawk-мод), не CmdPal.

Поэтому делаем **мод панели**, не притворяемся CmdPal. Команды в палитре — фаза 2 (Open NC, DND), если human попросит.

## Цель

Windhawk-мод `taskbar-notify-island`: компактная «бровка»/капсула на taskbar (как Dynamic Island / home-bar).
Клик открывает центр уведомлений Windows (Win+N / Action Center).
Один `.wh.cpp`, один PR в `windhawk-mods`. `@id` kebab, `@github=Leorik69`, `## Mod authorship` в body PR.
Не Copilot. Не дубль toast-middle-click-snooze / taskbar-focus-timer.

## Функционал v1 (хороший, не жирный)

1. Капсула на панели (центр или у трея), всегда видна.
2. ЛКМ — центр уведомлений.
3. ПКМ — Focus assist / DND если доступно, иначе тот же NC.
4. При появлении toast — короткая анимация/точка на капсуле (без глобального съедания клавиш).
5. Хук 1–9 и глобальный keyboard eat **запрещены**. Win+N через SendInput только по клику.

Не в v1: полноценный список уведомлений (Win11 не отдаёт нормальный API), Discord-live, медиа-остров.

## Критерий

PR с одним файлом `mods/taskbar-notify-island.wh.cpp` + authorship. Ссылка в Slack. SL: review после push.

## Срочность

сейчас.
