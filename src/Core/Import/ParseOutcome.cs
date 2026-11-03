namespace Core.Import;

// Результат розбору одного рядка: або готовий запис, або причина помилки.
internal abstract record ParseOutcome<T>;
internal sealed record ParseOk<T>(T Value) : ParseOutcome<T>;
internal sealed record ParseFailed<T>(string Reason) : ParseOutcome<T>;
