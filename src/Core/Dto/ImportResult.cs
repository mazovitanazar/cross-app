namespace Core.Dto;

public sealed record ImportResult<T>(IReadOnlyList<T> Items, IReadOnlyList<string> Errors)
{
    public int Total => Items.Count + Errors.Count;

    public double ErrorPercent => Total == 0 ? 0 : 100.0 * Errors.Count / Total;
}
