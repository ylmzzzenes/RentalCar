using RentalCar.Application.Contracts.Cars;
using RentalCar.Domain.Entities;

namespace RentalCar.Application.Abstractions.Services.Cars;

/// <summary>İlan güvenilirlik skoru (ICarAppService’ten ayrı; gün bazlı commit’lerde net sınır).</summary>
public interface ICarListingReliabilityService
{
    Task<ListingReliabilityResult> CalculateAsync(Car car, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, ListingReliabilityResult>> CalculateBatchAsync(
        IReadOnlyList<Car> cars,
        CancellationToken cancellationToken = default);
}
