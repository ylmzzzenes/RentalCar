using RentalCar.Application.Dtos.Purchases;

namespace RentalCar.Application.Abstractions.Services.Purchases;

public interface IPurchaseAppService
{
    Task<PurchasePageDto?> GetPurchasePageAsync(int carId, CancellationToken cancellationToken = default);
    Task<PurchaseResultDto> CreatePurchaseAsync(int carId, string userId, decimal agreedPrice, string? buyerNote, CancellationToken cancellationToken = default);
}
