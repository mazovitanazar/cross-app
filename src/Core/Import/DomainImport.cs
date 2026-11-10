using Core.Domain;
using Core.Dto;

namespace Core.Import;

// Додаткове завдання 1: зв'язок із тижнем 3. ImportResult<DTO> -> ImportResult<сутність>:
// помилки формату переносяться як є, до них додаються записи, що не пройшли інваріанти.
public static class DomainImport
{
    public static ImportResult<Product> ToProducts(ImportResult<ProductDto> source) =>
        Convert(source, Product.FromDto, dto => dto.Id);

    public static ImportResult<StockBatch> ToBatches(ImportResult<StockBatchDto> source) =>
        Convert(source, StockBatch.FromDto, dto => dto.Id);

    private static ImportResult<TEntity> Convert<TDto, TEntity>(
        ImportResult<TDto> source, Func<TDto, TEntity> fromDto, Func<TDto, string?> key)
    {
        var items = new List<TEntity>();
        var errors = new List<string>(source.Errors);

        foreach (TDto dto in source.Items)
        {
            try
            {
                items.Add(fromDto(dto));
            }
            catch (ArgumentException ex) // ArgumentOutOfRangeException — нащадок ArgumentException
            {
                errors.Add($"запис '{key(dto)}': {ex.Message}");
            }
        }

        return new ImportResult<TEntity>(items, errors);
    }
}
