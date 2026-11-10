using Core.Dto;

namespace Core.Domain;

// Товар каталогу. Створюється лише через Create, змінюється лише методами з бізнес-назвами.
public sealed class Product
{
    private decimal _price;

    public string Id { get; }
    public string Sku { get; }
    public string Name { get; }
    public string Unit { get; }
    public string? Category { get; }
    public decimal Price => _price;
    public bool IsDiscontinued { get; private set; }

    private Product(string id, string sku, string name, string unit, decimal price, string? category)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Unit = unit;
        _price = price;
        Category = category;
    }

    // Єдиний спосіб створити товар: усі перевірки до виклику конструктора.
    public static Product Create(string id, string sku, string name, string unit, decimal price, string? category = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор товару обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU не може бути порожнім", nameof(sku));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва товару не може бути порожньою", nameof(name));
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Одиниця виміру обов'язкова", nameof(unit));
        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price), price, "Ціна має бути більшою за нуль");

        return new Product(id.Trim(), sku.Trim().ToUpperInvariant(), name.Trim(), unit.Trim(), price,
            string.IsNullOrWhiteSpace(category) ? null : category.Trim());
    }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice), newPrice, "Ціна має бути більшою за нуль");
        if (IsDiscontinued)
            throw new InvalidOperationException($"Товар {Sku} знято з продажу: змінити ціну не можна");
        _price = newPrice;
    }

    public void Discontinue()
    {
        if (IsDiscontinued)
            throw new InvalidOperationException($"Товар {Sku} уже знято з продажу");
        IsDiscontinued = true;
    }

    // Мапінг у формат тижня 3 і назад; FromDto проходить ті самі перевірки, що й Create.
    public ProductDto ToDto() => new(Id, Sku, Name, Unit, Price, Category);

    public static Product FromDto(ProductDto dto) =>
        Create(dto.Id, dto.Sku, dto.Name, dto.Unit, dto.Price, dto.Category);

    public override string ToString() =>
        $"{Id} [{Sku}] {Name} — {Price:F2} грн/{Unit}{(IsDiscontinued ? " (знято з продажу)" : "")}";
}
