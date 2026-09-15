# winui-build-fix

Триггер: сборка WinUI 3 / Windows App SDK падает (`MSB4062`, `Appx`, `Workload missing`, `WindowsAppSDK`).

Шаги:
1. Прочитай ошибку целиком: код (`MSB4062`), task name (`GenerateAppxManifest`, `ResolvePackageAssets`), версию `WindowsAppSDK` в csproj.
2. Проверь окружение: `dotnet workload list`, `global.json` (SDK pin), наличие `Windows App SDK / Single-project MSIX` workload.
3. Чини минимально: `dotnet workload restore` → `dotnet clean` → `dotnet build`; только потом трогай csproj.
4. Если MSB4062 от `Microsoft.WindowsAppSDK.BuildTools`: сверь версию пакета с TargetFramework (`net8.0-windows10.0.22621.0`) и `WindowsPackageType=None` для unpackaged.

Pitfalls:
- Не повышай версию WindowsAppSDK «наугад» — ломает manifest.
- Не коммить `bin/`, `obj/`, `*.user`, сгенерированный `AppxManifest`.
- Unpackaged ≠ упакованный: не чини Appx-ошибку добавлением packaging.

Проверка: `dotnet build island/NotifyIsland.csproj` зелёное, `git status` без `bin/obj`.
