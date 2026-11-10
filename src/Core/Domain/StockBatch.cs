using Core.Dto;

namespace Core.Domain;

// Партія товару: скільки надійшло, скільки лишилось, термін придатності і стан.
public sealed class StockBatch
{
    private int _remaining;

    public string Id { get; }
    public string ProductId { get; }
    public int InitialQuantity { get; }
    public int Remaining => _remaining;
    public DateOnly ArrivedOn { get; }
    public DateOnly? ExpiresOn { get; }
    public BatchStatus Status { get; private set; }

    private StockBatch(string id, string productId, int quantity, DateOnly arrivedOn, DateOnly? expiresOn)
    {
        Id = id;
        ProductId = productId;
        InitialQuantity = quantity;
        _remaining = quantity;
        ArrivedOn = arrivedOn;
        ExpiresOn = expiresOn;
        Status = BatchStatus.Pending;
    }

    public static StockBatch Create(string id, string productId, int quantity, DateOnly arrivedOn, DateOnly? expiresOn = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор партії обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Партія має належати товару", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Кількість у партії має бути більшою за нуль");
        if (expiresOn is { } expires && expires < arrivedOn)
            throw new ArgumentOutOfRangeException(nameof(expiresOn), expiresOn,
                $"Термін придатності {expires:yyyy-MM-dd} раніший за дату надходження {arrivedOn:yyyy-MM-dd}");

        return new StockBatch(id.Trim(), productId.Trim(), quantity, arrivedOn, expiresOn);
    }

    // Дата передається ззовні, а не береться з DateTime.Now: поведінка відтворювана і тестована.
    public bool IsExpiredOn(DateOnly date) => ExpiresOn is { } expires && date > expires;

    public void Accept() => MoveTo(BatchStatus.Accepted);

    public void WriteOff() => MoveTo(BatchStatus.WrittenOff);

    public void Issue(int amount, DateOnly onDate)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Кількість видачі має бути більшою за нуль");
        if (Status != BatchStatus.Accepted)
            throw new InvalidOperationException($"Партія {Id} у стані {Status}: видача можлива лише з прийнятої партії");
        if (IsExpiredOn(onDate))
            throw new InvalidOperationException($"Партія {Id} прострочена ({ExpiresOn:yyyy-MM-dd}): видача {onDate:yyyy-MM-dd} заборонена");
        if (amount > _remaining)
            throw new InvalidOperationException($"Не можна видати {amount}: у партії {Id} лишилось {_remaining}");

        _remaining -= amount;
        if (_remaining == 0)
            MoveTo(BatchStatus.Depleted);
    }

    // Таблиця допустимих переходів: switch expression по кортежу (from, to).
    public static bool CanTransition(BatchStatus from, BatchStatus to) => (from, to) switch
    {
        (BatchStatus.Pending, BatchStatus.Accepted) => true,
        (BatchStatus.Pending, BatchStatus.WrittenOff) => true,   // відмова від приймання (брак)
        (BatchStatus.Accepted, BatchStatus.Depleted) => true,    // лише через Issue, коли залишок став 0
        (BatchStatus.Accepted, BatchStatus.WrittenOff) => true,  // списання залишку
        _ => false
    };

    private void MoveTo(BatchStatus next)
    {
        if (!CanTransition(Status, next))
            throw new InvalidOperationException($"Партія {Id}: перехід {Status} → {next} заборонений");
        Status = next;
    }

    public StockBatchDto ToDto() => new(Id, ProductId, Remaining, ArrivedOn, ExpiresOn);

    // FromDto проходить ті самі перевірки, що й Create; партія з файлу — ще не прийнята (Pending).
    public static StockBatch FromDto(StockBatchDto dto) =>
        Create(dto.Id, dto.ProductId, dto.Quantity, dto.ArrivedOn, dto.ExpiresOn);

    public override string ToString() =>
        $"{Id} ({ProductId}) {Remaining}/{InitialQuantity} від {ArrivedOn:yyyy-MM-dd}" +
        $"{(ExpiresOn is { } e ? $" до {e:yyyy-MM-dd}" : "")} [{Status}]";
}
