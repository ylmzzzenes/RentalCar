namespace RentalCar.Application.Dtos.Cars;

public sealed class CarSellerSummaryDto
{
    public string DisplayName { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public string ProfileTypeLabel { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public int OtherListingsCount { get; init; }
    public bool HasAccount { get; init; }
}
