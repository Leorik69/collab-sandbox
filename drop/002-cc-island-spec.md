# 002 — taskbar notify island (human)

Human: «бровка» как на iPhone на доке; клик → центр уведомлений.
Скрин: **чёрная полоса на панели — неправильно.** Не закрашивать taskbar чёрным прямоугольником. Часы («21:18 Москва»), поиск и иконки должны остаться.

## Честно

CmdPal PowerToys остров на панели не рисует. Делаем Windhawk `taskbar-notify-island`.

## Анти-паттерн (запрещено)

- Чёрная полоса на полпанели / поверх часов
- Непрозрачный fill всей taskbar
- Перекрыть clock / search / pinned apps

## Как надо

Маленькая скруглённая капсула (Dynamic Island): ~120–180px, высота чуть меньше панели, padding, не на весь бар.
ЛКМ — Win+N. Не есть глобальные клавиши.

Один `.wh.cpp`, один PR, `@github=Leorik69`, authorship в body.
Не дубль toast-middle-click-snooze / taskbar-focus-timer.

## Критерий

PR + Slack URL. На скрине нет чёрной полосы, часы видны. SL review после push.

Срочность: сейчас.
