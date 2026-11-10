using Core;
using Core.Dto;
using Core.Domain;
using Core.Import;
using Core.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Contains("--env"))
{
    EnvironmentReport env = EnvironmentInfo.Collect();
    Console.WriteLine($"{env.OsDescription} | {env.FrameworkDescription} | {env.ReportedRid} | {env.BuildNote}");
}

// Лабораторна 4: доменна модель.
if (args.Contains("--domain"))
    return RunDomain();
if (args.Contains("--domain-import"))
    return RunDomainImport(args.FirstOrDefault(a => !a.StartsWith("--")) ?? Path.Combine("data", "domain.json"));
if (args.Contains("--fifo"))
    return RunFifo();
if (args.Contains("--status"))
    return RunStatus();

if (args.Contains("--catalog"))
    return RunCatalog(args.FirstOrDefault(a => !a.StartsWith("--")) ?? Path.Combine("data", "catalog.csv"));

bool batches = args.Contains("--batches");
string[] paths = args.Where(a => !a.StartsWith("--")).ToArray();
string path = paths.Length > 0
    ? paths[0]
    : Path.Combine("data", batches ? "batches.csv" : "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

if (batches)
{
    ImportResult<StockBatchDto> result = StockBatchCsvImporter.Load(path);
    Console.WriteLine($"Завантажено партій: {result.Items.Count}");
    foreach (StockBatchDto b in result.Items.Take(5))
        Console.WriteLine($"  {b.Id,-6} {b.ProductId,-6} {b.Quantity,6} {b.ArrivedOn:yyyy-MM-dd}  до {b.ExpiresOn?.ToString("yyyy-MM-dd") ?? "—"}");
    PrintErrors(result);
}
else
{
    ImportResult<ProductDto> result = Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".json" => ProductJsonImporter.Load(path),
        _ => ProductCsvImporter.Load(path)
    };
    Console.WriteLine($"Завантажено товарів: {result.Items.Count}");
    foreach (ProductDto p in result.Items.Take(5))
        Console.WriteLine($"  {p.Id,-6} {p.Sku,-8} {p.Name,-26} {p.Price,9:F2} грн/{p.Unit,-3} {p.Category ?? "—"}");
    PrintErrors(result);
}

return 0;

// Додаткове завдання 2: різнорідний файл (P — товари, B — партії).
static int RunCatalog(string path)
{
    if (!File.Exists(path))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
        return 1;
    }

    CatalogImportResult result = CatalogCsvImporter.Load(path);
    Console.WriteLine($"Товарів: {result.Products.Count}");
    foreach (ProductDto p in result.Products)
        Console.WriteLine($"  {p.Id,-6} {p.Sku,-8} {p.Name,-26} {p.Price,9:F2} грн/{p.Unit,-3} {p.Category ?? "—"}");
    Console.WriteLine($"Партій: {result.Batches.Count}");
    foreach (StockBatchDto b in result.Batches)
        Console.WriteLine($"  {b.Id,-6} {b.ProductId,-6} {b.Quantity,6} {b.ArrivedOn:yyyy-MM-dd}  до {b.ExpiresOn?.ToString("yyyy-MM-dd") ?? "—"}");
    if (result.Errors.Count > 0)
    {
        Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
        foreach (string e in result.Errors)
            Console.WriteLine($"  ! {e}");
    }
    Console.WriteLine($"Усього: {result.Total} / товарів: {result.Products.Count} / партій: {result.Batches.Count} / пропущено: {result.Errors.Count}");
    return 0;
}

// Лабораторна 4, сценарії «успіх» і «порушення інваріантів».
static int RunDomain()
{
    Console.WriteLine("=== Сценарій 1: успіх ===");
    Product cable = Product.Create("P-001", "el-1001", "Кабель ВВГ 3x2.5", "м", 38.90m, "Електрика");
    Console.WriteLine(cable);
    cable.ChangePrice(41.50m);
    Console.WriteLine(cable);

    StockBatch batch = StockBatch.Create("B-001", "P-001", 500, new DateOnly(2026, 9, 1));
    Console.WriteLine(batch);
    batch.Accept();
    batch.Issue(120, new DateOnly(2026, 9, 15));
    Console.WriteLine(batch);

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
    TryDo("порожній SKU", () => Product.Create("P-002", "  ", "Пісок", "т", 10m));
    TryDo("нульова ціна", () => Product.Create("P-003", "SN-2001", "Труба PPR 20мм", "м", 0m));
    TryDo("від'ємна нова ціна", () => cable.ChangePrice(-5m));
    TryDo("партія з нульовою кількістю", () => StockBatch.Create("B-009", "P-005", 0, new DateOnly(2026, 9, 25)));
    TryDo("термін раніший за надходження", () =>
        StockBatch.Create("B-011", "P-009", 15, new DateOnly(2026, 10, 1), new DateOnly(2026, 9, 1)));
    TryDo("видача понад залишок партії", () => batch.Issue(1000, new DateOnly(2026, 9, 20)));
    TryDo("нульова видача", () => batch.Issue(0, new DateOnly(2026, 9, 20)));

    StockBatch pending = StockBatch.Create("B-002", "P-001", 40, new DateOnly(2026, 9, 3));
    TryDo("видача з неприйнятої партії", () => pending.Issue(5, new DateOnly(2026, 9, 4)));

    cable.Discontinue();
    TryDo("зміна ціни знятого з продажу товару", () => cable.ChangePrice(45m));
    TryDo("повторне зняття з продажу", cable.Discontinue);

    Console.WriteLine();
    Console.WriteLine("Стан після всіх відмов:");
    Console.WriteLine($"  {cable}");
    Console.WriteLine($"  {batch}");

    Console.WriteLine();
    Console.WriteLine("=== ToDto / FromDto ===");
    ProductDto dto = cable.ToDto();
    Console.WriteLine($"DTO:        {dto}");
    Console.WriteLine($"Відновлено: {Product.FromDto(dto)}");
    TryDo("FromDto з від'ємною ціною", () => Product.FromDto(dto with { Price = -1m }));
    TryDo("FromDto партії з нульовою кількістю", () => StockBatch.FromDto(batch.ToDto() with { Quantity = 0 }));
    return 0;
}

// Лабораторна 4, додаткове завдання 1: ImportResult<DTO> -> сутності + рядки, що не пройшли інваріанти.
static int RunDomainImport(string path)
{
    if (!File.Exists(path))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
        return 1;
    }

    ImportResult<ProductDto> dtos = Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".json" => ProductJsonImporter.Load(path),
        _ => ProductCsvImporter.Load(path)
    };
    ImportResult<Product> products = DomainImport.ToProducts(dtos);

    Console.WriteLine($"DTO прочитано: {dtos.Items.Count}, помилок формату: {dtos.Errors.Count}");
    Console.WriteLine($"Сутностей створено: {products.Items.Count}");
    foreach (Product p in products.Items)
        Console.WriteLine($"  {p}");
    PrintErrors(products);
    return 0;
}

// Лабораторна 4, додаткове завдання 2: правило «товар + усі його партії» у сервісі (FIFO-видача).
static int RunFifo()
{
    var day = new DateOnly(2026, 10, 15);
    Product glue = Product.Create("P-020", "BL-5001", "Клей для плитки 25кг", "мішок", 289m, "Будівельні суміші");
    List<StockBatch> batches =
    [
        StockBatch.Create("B-101", "P-020", 30, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1)),   // уже прострочена
        StockBatch.Create("B-102", "P-020", 20, new DateOnly(2026, 9, 10), new DateOnly(2027, 3, 10)),
        StockBatch.Create("B-103", "P-020", 50, new DateOnly(2026, 9, 25), new DateOnly(2027, 3, 25)),
        StockBatch.Create("B-104", "P-020", 40, new DateOnly(2026, 10, 10), new DateOnly(2027, 4, 10)),  // не прийнята
    ];
    foreach (StockBatch b in batches.Take(3))
        b.Accept();

    Console.WriteLine($"Товар: {glue}");
    Console.WriteLine($"Дата видачі: {day:yyyy-MM-dd}");
    PrintBatches(batches);
    Console.WriteLine($"Доступно до видачі: {StockService.Available(glue, batches, day)} (B-101 прострочена, B-104 не прийнята)");

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 1: видати 35 за FIFO ===");
    foreach (var (batchId, taken) in StockService.IssueFifo(glue, batches, 35, day))
        Console.WriteLine($"  з {batchId}: {taken}");
    PrintBatches(batches);

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 2: порушення правила двох сутностей ===");
    TryDo("видати 100 (більше за доступне)", () => StockService.IssueFifo(glue, batches, 100, day));
    TryDo("чужа партія у списку", () => StockService.IssueFifo(glue,
        [.. batches, StockBatch.Create("B-900", "P-001", 10, new DateOnly(2026, 9, 1))], 5, day));
    glue.Discontinue();
    TryDo("видача знятого з продажу товару", () => StockService.IssueFifo(glue, batches, 5, day));
    Console.WriteLine("Стан після відмов (жодна партія не змінилась):");
    PrintBatches(batches);
    return 0;
}

// Лабораторна 4, додаткове завдання 3: enum-стан партії і таблиця допустимих переходів.
static int RunStatus()
{
    BatchStatus[] statuses = Enum.GetValues<BatchStatus>();
    Console.WriteLine("=== Допустимі переходи (рядок -> стовпець) ===");
    Console.WriteLine($"{"",-11}" + string.Concat(statuses.Select(s => $"{s,-11}")));
    foreach (BatchStatus from in statuses)
        Console.WriteLine($"{from,-11}" + string.Concat(statuses.Select(to => $"{(StockBatch.CanTransition(from, to) ? "+" : "-"),-11}")));

    var day = new DateOnly(2026, 10, 1);
    Console.WriteLine();
    Console.WriteLine("=== Сценарій 1: Pending -> Accepted -> Depleted ===");
    StockBatch batch = StockBatch.Create("B-201", "P-002", 10, new DateOnly(2026, 9, 20));
    Console.WriteLine($"  {batch}");
    batch.Accept();
    Console.WriteLine($"  {batch}");
    batch.Issue(10, day);
    Console.WriteLine($"  {batch}");

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 2: заборонені переходи ===");
    TryDo("видача з вичерпаної партії", () => batch.Issue(1, day));
    TryDo("списання вичерпаної партії", batch.WriteOff);
    StockBatch rejected = StockBatch.Create("B-202", "P-002", 15, new DateOnly(2026, 9, 21));
    rejected.WriteOff();
    Console.WriteLine($"  {rejected}");
    TryDo("приймання списаної партії", rejected.Accept);
    TryDo("повторне списання", rejected.WriteOff);
    StockBatch accepted = StockBatch.Create("B-203", "P-002", 5, new DateOnly(2026, 9, 22));
    accepted.Accept();
    TryDo("повторне приймання", accepted.Accept);
    return 0;
}

static void PrintBatches(IEnumerable<StockBatch> batches)
{
    foreach (StockBatch b in batches)
        Console.WriteLine($"  {b}");
}

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}

static void PrintErrors<T>(ImportResult<T> result)
{
    if (result.Errors.Count > 0)
    {
        Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
        foreach (string e in result.Errors)
            Console.WriteLine($"  ! {e}");
    }
    Console.WriteLine($"Усього: {result.Total} / прийнято: {result.Items.Count} / пропущено: {result.Errors.Count} / помилок: {result.ErrorPercent:F1}%");
}
