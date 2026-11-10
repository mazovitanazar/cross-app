using Core.Dto;

namespace Core.Import;

// Додаткове завдання 2: різнорідні рядки в одному файлі.
//   P;id;sku;name;unit;price[;category]          – товар
//   B;id;productId;quantity;arrivedOn[;expiresOn] – партія
// Тип рядка визначає перша колонка; один switch розводить рядки на два різні типи результату.
public static class CatalogCsvImporter
{
    public static CatalogImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var batches = new List<StockBatchDto>();
        var errors = new List<string>();

        foreach (var (number, parts) in CsvImport.DataLines(path, headerStart: "type"))
        {
            switch (parts)
            {
                // Зріз .. var rest віддає решту колонок без префікса — їх розбирає вже наявний парсер.
                case ["P", .. var rest]:
                    Collect(ProductCsvImporter.ParseLine(rest), products, errors, number, "товар");
                    break;
                case ["B", .. var rest]:
                    Collect(StockBatchCsvImporter.ParseLine(rest), batches, errors, number, "партія");
                    break;
                case [var type, ..]:
                    errors.Add($"рядок {number}: невідомий тип запису '{type}' (очікую P або B)");
                    break;
            }
        }

        return new CatalogImportResult(products, batches, errors);
    }

    private static void Collect<T>(ParseOutcome<T> outcome, List<T> items, List<string> errors, int number, string kind)
    {
        switch (outcome)
        {
            case ParseOk<T> ok:
                items.Add(ok.Value);
                break;
            case ParseFailed<T> failed:
                errors.Add($"рядок {number} ({kind}): {failed.Reason}");
                break;
        }
    }
}
