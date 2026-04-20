using RentalCar.Application.Contracts.Cars;
using RentalCar.Domain.Entities;

namespace RentalCar.Application.Abstractions.Services.Cars;

public interface IListingReliabilitySignalsProvider
{
    Task<ListingReliabilitySignals> GetSignalsAsync(Car car, CancellationToken cancellationToken = default);

    /// <summary>Listeleme sayfaları için toplu sinyal; N+1 önlemek amacıyla.</summary>
    Task<IReadOnlyDictionary<int, ListingReliabilitySignals>> GetSignalsForCarsAsync(
        IReadOnlyList<Car> cars,
        CancellationToken cancellationToken = default);
}
