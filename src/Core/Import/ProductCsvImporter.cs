using System.Globalization;
using Core.Dto;

namespace Core.Import;

// Формат: id;sku;name;unit;price[;category]
public static class ProductCsvImporter
{
    // Без AllowThousands: "12,50" має бути помилкою, а не 1250.
    private const NumberStyles PriceStyle = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

    public static ImportResult<ProductDto> Load(string path) =>
        CsvImport.Load(path, "id", ParseLine);

    private static ParseOutcome<ProductDto> ParseLine(string[] parts) => parts switch
    {
        { Length: < 5 } => Fail($"очікую 5-6 колонок, отримав {parts.Length}"),
        { Length: > 6 } => Fail($"занадто багато колонок: {parts.Length}"),
        [_, "", ..] or [_, _, "", ..] => Fail("SKU або назва порожні"),
        [_, _, _, "", ..] => Fail("не вказано одиницю виміру"),
        [_, _, _, _, var price, ..] when !TryParsePrice(price, out _)
            => Fail($"ціна '{price}' не є додатним числом"),
        [var id, var sku, var name, var unit, var price]
            => Ok(new ProductDto(id, sku, name, unit, ParsePrice(price))),
        [var id, var sku, var name, var unit, var price, var category]
            => Ok(new ProductDto(id, sku, name, unit, ParsePrice(price), category is "" ? null : category)),
        _ => Fail("нерозпізнаний формат рядка")
    };

    private static bool TryParsePrice(string text, out decimal price) =>
        decimal.TryParse(text, PriceStyle, CultureInfo.InvariantCulture, out price) && price > 0;

    private static decimal ParsePrice(string text) =>
        decimal.Parse(text, PriceStyle, CultureInfo.InvariantCulture);

    private static ParseOutcome<ProductDto> Ok(ProductDto value) => new ParseOk<ProductDto>(value);

    private static ParseOutcome<ProductDto> Fail(string reason) => new ParseFailed<ProductDto>(reason);
}
