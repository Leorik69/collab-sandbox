# MSIX: оценка (почему пока только портатив)

**Решение: ship portable zip, MSIX отложить.**

**Запуск после unzip:** распаковать zip в папку и запустить `Запустить.bat` или
`NotifyIsland.exe` **из этого каталога** (рядом должны лежать `resources.pri`,
`Microsoft.ui.xaml.dll`, `Microsoft.WindowsAppRuntime.Bootstrap.dll` и прочий
self-contained payload). Это тот же layout, что `island/publish/NotifyIsland.exe`
после `tools/Publish-Portable.ps1`. Один скопированный exe APPCRASHит
(`Microsoft.UI.Xaml.dll` 0xc000027b) — WinUI грузит PRI/нативные DLL из папки exe.

Причины:
1. Проект сейчас `WindowsPackageType=None` (unpackaged) + `WindowsAppSDKSelfContained=true`
   + `EnableMsixTooling=true` (чтобы Publish положил app `resources.pri`).
   Портатив: VS MSBuild `/t:Publish` self-contained `win-x64` без сертификатов.
2. MSIX требует дополнительно: `Package.appxmanifest`, иконки-тайлы, связка
   `GenerateAppxPackageOnBuild`, тестовый сертификат (`MakeCert`/`New-Self-SignedCertificate`)
   + установка cert на каждой машине + подпись (`SignTool`). Без пайплайна подписи
   каждый пользователь получит SmartScreen-варнинг — хуже, чем честный zip.
3. Unpackaged + MSIX в одном csproj конфликтуют (`WindowsPackageType` один);
   нужен отдельный проект/конфигурация упаковки — лишний scope для 005.

Что добавить, когда MSIX понадобится:
- `island/Package/Package.appxmanifest` (+ `Package.StoreAssociation.xml` для Store),
- иконки `Assets/Tiles/` (44/50/150/ wide310 + targetsize-вариации),
- в publish-профиль `-p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=true`,
- CI-шаг подписи (`AzureSignTool` / `signtool sign /fd SHA256 /a`).
