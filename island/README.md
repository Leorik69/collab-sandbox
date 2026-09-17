# NotifyIsland

WinUI 3 top-center overlay (unpackaged). Capsule `#080808`, Segoe UI Variable, one blue accent.

```
cd island && dotnet run
```

Демо всех состояний:

```
dotnet run -- --demo
```

Или после запуска: **F9**, либо ПКМ по капсуле → «Demo все состояния (F9)».

## Portable (без сборки из исходников)

Unpackaged self-contained `win-x64`, **не MSIX** (`island/packaging/MSIX-NOTE.md`). Бинарники в git не лежат.

**Скачать zip:** GitHub Actions → **Portable win-x64** → Artifacts → `NotifyIsland-portable-win-x64`. Распаковать **всю папку**. Запуск: `Запустить.bat` или `NotifyIsland.exe` рядом с `resources.pri`. Не копировать exe отдельно.

Демо из zip:

```
NotifyIsland.exe --demo
```

**Локально (Windows, VS 2022 + Windows app development):**

```powershell
cd island
powershell -ExecutionPolicy Bypass -File tools\Publish-Portable.ps1
```
