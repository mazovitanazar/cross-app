namespace Core.Dto;

// Результат імпорту різнорідного файлу: два типи записів і спільний список помилок.
public sealed record CatalogImportResult(
    IReadOnlyList<ProductDto> Products,
    IReadOnlyList<StockBatchDto> Batches,
    IReadOnlyList<string> Errors)
{
    public int Total => Products.Count + Batches.Count + Errors.Count;
}
