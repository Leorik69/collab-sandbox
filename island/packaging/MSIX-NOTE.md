# MSIX: оценка (почему пока только портатив)

**Решение: ship portable zip, MSIX отложить.**

Причины:
1. Проект сейчас `WindowsPackageType=None` (unpackaged) + `WindowsAppSDKSelfContained=true`.
   Портатив собирается одной командой `dotnet publish -r win-x64 --self-contained` без
   сертификатов и работает рядом с `Assets/` — этого достаточно для drop/005.
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
