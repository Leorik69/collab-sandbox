# NotifyIsland 🏝️

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)]()
[![Platform](https://img.shields.io/badge/Platform-Windows%2011-0078D4?logo=windows11&logoColor=white)](https://microsoft.com/windows/windows-11)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/UI-WinUI%203-0078D4?logo=microsoft&logoColor=white)](https://learn.microsoft.com/windows/apps/winui/winui3/)

**NotifyIsland** — это интерактивный оверлей-островок в стиле *Dynamic Island* для Windows 11, построенный на фреймворке **WinUI 3** и **Windows App SDK**. Проект обеспечивает плавающее интерактивное пространство на экране для вывода времени, погоды, уведомлений и системных статусов.

---

## 🚀 Основные возможности

### 🎨 Стили и кастомизация
- **Геометрия формы**:
  - `Capsule` — классическая закруглённая капсула.
  - `SoftRect` — прямоугольник с мягким скруглением углов.
  - `Oval` — овальная форма.
  - Регулируемый `CornerRadius` для тонкой настройки скруглений.
- **Материалы и фоны**:
  - `Solid` — сплошной цвет с настройкой непрозрачности.
  - `Acrylic` — эффекты размытия разной степени.
  - `Mica` — поддержка классического и Alt-материала Windows 11.
  - Настраиваемая палитра акцентных цветов и градиентов.

### 🧩 Модули
- **Часы**:
  - `Digital Modern` — современный цифровой циферблат.
  - `Digital Minimal` — лаконичный минималистичный вид.
  - `Seconds + Arc` — индикация секунд со круговой дугой прогресса.
- **Погода**:
  - `Compact Badge` — компактный виджет с текущей температурой и иконкой.
  - `Expanded Detail` — расширенный карточный вид с прогнозом, влажностью и ветром.
- **Иконки**:
  - `Fluent System` — системные иконки в стиле Fluent Icons.
  - `Minimal Line` — линейные минималистичные контуры.
  - `Filled Active` — залитые иконки активных состояний.
- **Эффекты**:
  - `Border Glow` — мягкое свечение границ при активности.
  - `Pulse Aura` — пульсирующая аура вокруг островка при поступлении уведомлений.
  - `Hover Highlight` — динамический подсвет при наведении курсора.

### ⚡ Реакции и интеграция с системой
- **Интерактивность**: ЛКМ по островку вызовет Центр уведомлений Windows 11 (`Win+N`).
- **Do Not Disturb (DND)**: поддержка режима «Не беспокоить» с адаптивным скрытием или изменением анимаций.
- **Конфигурация**: сохранение и загрузка настроек через `LocalSettings JSON`.

---

## 🛠️ Требования к сборке и окружению

Для сборки и запуска проекта необходимо следующее окружение:

- **ОС**: Windows 11 (Build 22000 или новее)
- **SDK**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **IDE / Workload**: Visual Studio 2022 с установленной нагрузкой **Developing App for Universal Windows Platform / WinUI 3 (Windows App SDK)**.

---

## 📦 Инструкция по сборке и запуску

1. **Клонируйте репозиторий**:
   ```bash
   git clone https://github.com/Leorik69/collab-sandbox.git
   ```

2. **Перейдите в директорию с исходным кодом островка**:
   ```bash
   cd collab-sandbox/island
   ```

3. **Запустите проект**:
   ```bash
   dotnet run
   ```

   Демо всех состояний: `dotnet run -- --demo` (или **F9** / ПКМ → Demo все состояния).

---

## 📁 Структура проекта

```text
collab-sandbox/
├── island/          # Исходный код приложения NotifyIsland (WinUI 3 / C#)
├── drop/            # Дамп результатов, артефактов и спек коллаборации (00X-*.md)
├── CURRENT.md       # Текущий статус задач и активная цель коллаборации
└── README.md        # Документация репозитория
```

---

## 📜 Лицензия и статус коллаборации

Проект разрабатывается в рамках совместной работы трех агентов/участников: **NB** + **SL** + **CC**.

- **Лицензия**: MIT License
- **Режим разработки**: Открытый коллаб (дамп спецификаций в `drop/`, синхронизация задач через `CURRENT.md`).
