# CrossApp
Наскрізний проєкт з крос-платформного програмування.
Автор: Назар Мазовіта.

Предметна область: **Склад**.
Сутності: Product (товар), StockBatch (партія), Warehouse (склад), Movement (переміщення).
Призначення: облік залишків товарів по партіях.

## Структура solution
```
CrossApp/
  CrossApp.slnx
  README.md
  .gitignore
  src/
    Core/                 class library (net8.0; net10.0)
      Core.csproj
      EnvironmentInfo.cs  збір інформації про середовище (namespace Core)
      EnvironmentReport.cs record з даними про середовище
    Cli/                  консольний застосунок, точка входу
      Cli.csproj          ProjectReference -> ../Core/Core.csproj
      Program.cs          лише виклик Core і форматування виводу
```
Напрямок залежності тільки один: **Cli → Core**. Core не знає про Cli, тому
його без змін підключать Api (тиждень 10) і Blazor-клієнт (тиждень 12).

Домовленість про каталоги в Core на весь семестр:
- `Core/Dto/` — record-типи формату даних (тиждень 3);
- `Core/Domain/` — сутності з поведінкою та інваріантами (тиждень 4);
- `Core/Storage/` — реалізації сховищ (тиждень 5).

## Збірка і запуск
```bash
dotnet build
dotnet build src/Core/Core.csproj
dotnet run --project src/Cli
dotnet run --project src/Cli -- --json
```

## Публікація
```bash
# self-contained: код + залежності + копія .NET runtime
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true
# framework-dependent: лише код, runtime має бути встановлений
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained false
# аналогічно для win-x64 / linux-x64
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
# додатково
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true -p:PublishTrimmed=true
```
Запуск з каталогу publish (без `dotnet run`):
```bash
cd src/Cli/bin/Release/net10.0/osx-arm64/publish
./Cli
```

**Self-contained** публікація містить власну копію .NET runtime: застосунок
запускається на машині без встановленого .NET, але каталог важить ~80 МБ і
прив'язаний до однієї RID. **Framework-dependent** містить лише наш код
(~0.2 МБ), проте на машині користувача має бути встановлений .NET 10.

| RID       | Режим                         | Розмір publish | Файлів | Потрібен runtime |
|-----------|-------------------------------|---------------:|-------:|------------------|
| win-x64   | self-contained                | 77 МБ          | 194    | ні               |
| win-x64   | framework-dependent           | 216 КБ         | 7      | так (.NET 10)    |
| linux-x64 | self-contained                | 79 МБ          | 194    | ні               |
| osx-arm64 | self-contained                | 83 МБ          | 193    | ні               |
| osx-arm64 | framework-dependent           | 180 КБ         | 7      | так (.NET 10)    |
| osx-arm64 | self-contained + SingleFile   | 76 МБ          | 3      | ні               |
| osx-arm64 | self-contained + Trimmed      | 20 МБ          | 35     | ні               |

Розміри виміряно `du -sh <каталог publish>` (macOS 27, .NET SDK 10.0.401).

## Multi-targeting
`src/Core/Core.csproj` використовує `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>`,
у `src/Core/bin/Debug/` з'являються два підкаталоги `net8.0` і `net10.0`.
Рядок `BuildNote` в `EnvironmentInfo` відрізняється для двох TFM через `#if NET10_0_OR_GREATER`.

## Середовище
.NET SDK 10.0, Windows 11 x64 / macOS (arm64)
