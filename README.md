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
  data/                   вхідні файли (sample.csv, valid.csv, batches.csv, sample.json)
  src/
    Core/                 class library (net8.0; net10.0)
      Core.csproj
      EnvironmentInfo.cs  збір інформації про середовище (namespace Core)
      EnvironmentReport.cs record з даними про середовище
      Dto/                record-типи даних: ProductDto, StockBatchDto, ImportResult<T>
      Domain/             сутності з поведінкою: Product, StockBatch, BatchStatus (тиждень 4)
      Import/             імпортери CSV/JSON + DomainImport (DTO -> сутності)
      Services/           StockService — правило «товар + його партії» (FIFO-видача)
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

## Імпорт даних (лабораторна 3)
```bash
dotnet run --project src/Cli                         # data/sample.csv (з помилками)
dotnet run --project src/Cli -- data/valid.csv       # коректний файл без заголовка
dotnet run --project src/Cli -- data/sample.json     # JSON-імпортер (за розширенням)
dotnet run --project src/Cli -- --batches            # партії з data/batches.csv
dotnet run --project src/Cli -- data/nope.csv        # неіснуючий файл -> код виходу 1
dotnet run --project src/Cli -- --env                # + рядок про середовище
dotnet run --project src/Cli -- --catalog            # різнорідний файл data/catalog.csv (P/B)
```

Формат CSV:
- роздільник — крапка з комою `;` (константа `CsvImport.Separator`);
- кодування UTF-8, закінчення рядків LF або CRLF;
- заголовок необов'язковий: перший рядок, що починається з `id`, пропускається;
- порожні рядки та рядки, що починаються з `#`, ігноруються;
- числа — з крапкою (`38.90`), розбір через `CultureInfo.InvariantCulture`; `12,50` вважається помилкою;
- дати — `yyyy-MM-dd`.

| Файл                | Колонки                                         |
|---------------------|-------------------------------------------------|
| `data/sample.csv`   | `id;sku;name;unit;price[;category]`             |
| `data/batches.csv`  | `id;productId;quantity;arrivedOn[;expiresOn]`   |
| `data/catalog.csv`  | `P;<колонки товару>` або `B;<колонки партії>`   |

`data/catalog.csv` (додаткове завдання 2) містить товари і партії в одному файлі: тип рядка задає перша колонка,
`CatalogCsvImporter` одним `switch` (`["P", .. var rest]`, `["B", .. var rest]`) розводить їх на два списки
і перевикористовує `ParseLine` наявних імпортерів. Рядки 9–12 пошкоджені навмисно (ціна з комою, нульова
кількість, невідомий тип `W`, порожня назва).

У `data/sample.csv` рядки 13–15, а в `data/batches.csv` рядки 10–13 пошкоджені **навмисно** — це тестові дані
для перевірки обробки помилок.

## Доменна модель (лабораторна 4)
```bash
dotnet run --project src/Cli -- --domain                          # успіх + порушення інваріантів, ToDto/FromDto
dotnet run --project src/Cli -- --domain-import data/domain.json  # дод. 1: DTO -> сутності + помилки інваріантів
dotnet run --project src/Cli -- --fifo                            # дод. 2: правило двох сутностей у StockService
dotnet run --project src/Cli -- --status                          # дод. 3: стани партії і допустимі переходи
```

Сутності в `src/Core/Domain`:
- `Product` — товар каталогу: ціна в приватному полі `_price`, змінюється лише `ChangePrice`; `Discontinue` знімає з продажу (`IsDiscontinued { get; private set; }`).
- `StockBatch` — партія товару: залишок у приватному полі `_remaining`, змінюється лише `Issue`; стан `BatchStatus` змінюють `Accept`, `WriteOff` і `Issue`.
- Зв'язок: `StockBatch.ProductId` → `Product.Id` (у товару може бути багато партій).
- Records тижня 3 (`ProductDto`, `StockBatchDto`) лишились форматом даних; мапінг — `ToDto()` / `FromDto(dto)`, причому `FromDto` викликає `Create`.

Інваріанти:

| Правило | Тип винятку | Де перевіряється |
| --- | --- | --- |
| Id, SKU, назва, одиниця товару не порожні (SKU зберігається у верхньому регістрі) | ArgumentException | `Product.Create` |
| Ціна > 0 (і при створенні, і при зміні) | ArgumentOutOfRangeException | `Product.Create`, `Product.ChangePrice` |
| Ціну знятого з продажу товару змінити не можна; зняти з продажу двічі не можна | InvalidOperationException | `Product.ChangePrice`, `Product.Discontinue` |
| Id партії і товар партії не порожні | ArgumentException | `StockBatch.Create` |
| Кількість у партії > 0 | ArgumentOutOfRangeException | `StockBatch.Create` |
| Термін придатності не раніший за дату надходження | ArgumentOutOfRangeException | `StockBatch.Create` |
| Кількість видачі > 0 | ArgumentOutOfRangeException | `StockBatch.Issue`, `StockService.IssueFifo` |
| Видавати можна лише з прийнятої (Accepted) і непростроченої партії | InvalidOperationException | `StockBatch.Issue` |
| Не можна видати більше, ніж лишилось у партії | InvalidOperationException | `StockBatch.Issue` |
| Дозволені лише переходи Pending→Accepted, Pending→WrittenOff, Accepted→Depleted, Accepted→WrittenOff | InvalidOperationException | `StockBatch.CanTransition` (switch expression) |
| `FromDto` проходить ті самі перевірки | як у `Create` | `Product.FromDto`, `StockBatch.FromDto` |
| Видача товару за FIFO не більше, ніж є в прийнятих непрострочених партіях; лише власні партії; не для знятого з продажу товару; «все або нічого» | InvalidOperationException / ArgumentException | `StockService.IssueFifo` |

Перевірка: у `src/Core/Domain` і `src/Core/Services` немає `Console` і `File.`; `product.Price = -5`, `batch.Remaining = -1` і `batch.Status = ...` у Cli не компілюються (CS0200).

Чому правило FIFO в сервісі, а не в сутності: партія не знає про інші партії товару, а товар не зберігає партій.
Правило про сумарний доступний залишок охоплює кілька об'єктів, тому живе в `StockService`, який бачить їх усі;
з тижня 5 сервіс отримуватиме товар і партії зі сховищ.

## Середовище
.NET SDK 10.0, Windows 11 x64 / macOS (arm64)
