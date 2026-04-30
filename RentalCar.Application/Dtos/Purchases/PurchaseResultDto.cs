namespace RentalCar.Application.Dtos.Purchases;

public sealed class PurchaseResultDto
{
    public bool Success { get; init; }
    public int? PurchaseId { get; init; }
    public string? ErrorMessage { get; init; }
}
