using System.Text;
using Core.Dto;

namespace Core.Import;

// Спільний цикл по рядках CSV: пропуск порожніх, коментарів і заголовка,
// збирання записів разом із помилками (з номерами рядків).
internal static class CsvImport
{
    public const char Separator = ';';

    public static ImportResult<T> Load<T>(string path, string headerStart, Func<string[], ParseOutcome<T>> parse)
    {
        var items = new List<T>();
        var errors = new List<string>();

        foreach (var (number, parts) in DataLines(path, headerStart))
        {
            switch (parse(parts))
            {
                case ParseOk<T> ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed<T> failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<T>(items, errors);
    }

    // Рядки з даними: без порожніх, коментарів (#) і заголовка; кожен — номер у файлі + колонки.
    // Спільний обхід для всіх CSV-імпортерів, зокрема різнорідного CatalogCsvImporter.
    public static IEnumerable<(int Number, string[] Parts)> DataLines(string path, string? headerStart)
    {
        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;
            if (number == 1 && headerStart is not null && line.StartsWith(headerStart, StringComparison.OrdinalIgnoreCase))
                continue;

            yield return (number, line.Split(Separator, StringSplitOptions.TrimEntries));
        }
    }
}
