using Core;
using Core.Dto;
using Core.Import;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Contains("--env"))
{
    EnvironmentReport env = EnvironmentInfo.Collect();
    Console.WriteLine($"{env.OsDescription} | {env.FrameworkDescription} | {env.ReportedRid} | {env.BuildNote}");
}

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
