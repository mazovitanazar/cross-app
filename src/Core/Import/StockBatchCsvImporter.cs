using System.Globalization;
using Core.Dto;

namespace Core.Import;

// Формат: id;productId;quantity;arrivedOn[;expiresOn], дати yyyy-MM-dd
public static class StockBatchCsvImporter
{
    private const string DateFormat = "yyyy-MM-dd";

    public static ImportResult<StockBatchDto> Load(string path) =>
        CsvImport.Load(path, "id", ParseLine);

    internal static ParseOutcome<StockBatchDto> ParseLine(string[] parts) => parts switch
    {
        not { Length: 4 or 5 } => Fail($"очікую 4-5 колонок, отримав {parts.Length}"),
        [_, "", ..] => Fail("не вказано товар партії"),
        [_, _, var qty, ..] when !int.TryParse(qty, NumberStyles.None, CultureInfo.InvariantCulture, out int q) || q == 0
            => Fail($"кількість '{qty}' не є додатним цілим числом"),
        [_, _, _, var arrived, ..] when !TryParseDate(arrived, out _)
            => Fail($"дата надходження '{arrived}' не у форматі {DateFormat}"),
        [_, _, _, _, var expires] when expires != "" && !TryParseDate(expires, out _)
            => Fail($"термін придатності '{expires}' не у форматі {DateFormat}"),
        [_, _, _, var arrived, var expires] when expires != "" && ParseDate(expires) < ParseDate(arrived)
            => Fail("термін придатності раніший за дату надходження"),
        [var id, var productId, var qty, var arrived]
            => Ok(new StockBatchDto(id, productId, ParseQuantity(qty), ParseDate(arrived))),
        [var id, var productId, var qty, var arrived, var expires]
            => Ok(new StockBatchDto(id, productId, ParseQuantity(qty), ParseDate(arrived),
                expires is "" ? null : ParseDate(expires)))
    };

    private static int ParseQuantity(string text) => int.Parse(text, CultureInfo.InvariantCulture);

    private static bool TryParseDate(string text, out DateOnly date) =>
        DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static DateOnly ParseDate(string text) =>
        DateOnly.ParseExact(text, DateFormat, CultureInfo.InvariantCulture);

    private static ParseOutcome<StockBatchDto> Ok(StockBatchDto value) => new ParseOk<StockBatchDto>(value);

    private static ParseOutcome<StockBatchDto> Fail(string reason) => new ParseFailed<StockBatchDto>(reason);
}
