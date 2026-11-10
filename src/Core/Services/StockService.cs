using Core.Domain;

namespace Core.Services;

// Додаткове завдання 2: правило, що охоплює ДВІ сутності — товар і всі його партії.
// «Видати N одиниць товару з партій за FIFO: спершу найстаріші; не більше, ніж є в прийнятих
// непрострочених партіях; не видавати знятий з продажу товар.»
// Жодна сутність окремо не має даних для перевірки: партія не знає про інші партії, товар — про партії взагалі.
public static class StockService
{
    // Скільки можна видати на дату: сума залишків прийнятих і непрострочених партій товару.
    public static int Available(Product product, IEnumerable<StockBatch> batches, DateOnly onDate) =>
        IssuableBatches(product, batches, onDate).Sum(b => b.Remaining);

    // Повертає, скільки взято з кожної партії. «Все або нічого»: усі перевірки до першої зміни стану.
    public static IReadOnlyList<(string BatchId, int Taken)> IssueFifo(
        Product product, IReadOnlyList<StockBatch> batches, int amount, DateOnly onDate)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(batches);
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Кількість видачі має бути більшою за нуль");
        if (batches.FirstOrDefault(b => b.ProductId != product.Id) is { } foreign)
            throw new ArgumentException($"Партія {foreign.Id} належить товару {foreign.ProductId}, а не {product.Id}", nameof(batches));
        if (product.IsDiscontinued)
            throw new InvalidOperationException($"Товар {product.Sku} знято з продажу: видача заборонена");

        var source = IssuableBatches(product, batches, onDate).ToList();
        int available = source.Sum(b => b.Remaining);
        if (amount > available)
            throw new InvalidOperationException(
                $"Не можна видати {amount} ({product.Unit}) товару {product.Sku}: доступно {available} у {source.Count} партіях на {onDate:yyyy-MM-dd}");

        var taken = new List<(string, int)>();
        int left = amount;
        foreach (StockBatch batch in source)
        {
            int part = Math.Min(left, batch.Remaining);
            batch.Issue(part, onDate);
            taken.Add((batch.Id, part));
            left -= part;
            if (left == 0)
                break;
        }
        return taken;
    }

    // FIFO: спершу найстаріша партія; за однакової дати — та, що раніше спливає.
    private static IEnumerable<StockBatch> IssuableBatches(Product product, IEnumerable<StockBatch> batches, DateOnly onDate) =>
        batches.Where(b => b.ProductId == product.Id && b.Status == BatchStatus.Accepted && !b.IsExpiredOn(onDate))
               .OrderBy(b => b.ArrivedOn)
               .ThenBy(b => b.ExpiresOn ?? DateOnly.MaxValue)
               .ThenBy(b => b.Id, StringComparer.Ordinal);
}
