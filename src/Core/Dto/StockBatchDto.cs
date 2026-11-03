namespace Core.Dto;

public record StockBatchDto(
    string Id,
    string ProductId,
    int Quantity,
    DateOnly ArrivedOn,
    DateOnly? ExpiresOn = null);
