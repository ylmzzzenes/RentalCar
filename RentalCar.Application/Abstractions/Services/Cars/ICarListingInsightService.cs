using RentalCar.Application.Contracts.Cars;

namespace RentalCar.Application.Abstractions.Services.Cars;

public interface ICarListingInsightService
{
    Task<CarListingInsightResult> GetInsightsAsync(int carId, string? userId, CancellationToken cancellationToken = default);
}
