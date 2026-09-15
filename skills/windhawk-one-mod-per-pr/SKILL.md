# windhawk-one-mod-per-pr

Триггер: PR в `windhawk-mods` (или ревью модов) с ≥2 `.wh.cpp`, или правило «один мод — один PR».

Шаги:
1. Один PR = один `mods/<id>.wh.cpp` (+ его changelog/docs при необходимости). Несвязанные моды не смешивай.
2. Если в ветке уже несколько модов — split: ветка и PR на каждый файл; в body одинаковый authorship.
3. Не трогай чужие репозитории и несвязанные PR (чужой draft, другие org/repos).
4. Body PR: что делает мод, authorship (`submitter, with AI assistance` если так принято), ссылка на split-источник.

Pitfalls:
- Не чини три мода одним коммитом «чтобы быстрее».
- Не добавляй N-й мод, если human/CC его не назвал.
- Не клади Windhawk `.wh.cpp` в `collab-sandbox` / `island/`.

Проверка: в diff PR ровно один новый или изменённый `.wh.cpp`.
