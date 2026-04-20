using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Application.Contracts.Cars;
using RentalCar.Domain.Entities;

namespace RentalCar.Application.Services.Cars;

public sealed class CarListingReliabilityService(
    IListingReliabilitySignalsProvider reliabilitySignalsProvider,
    ListingReliabilityEngine reliabilityEngine) : ICarListingReliabilityService
{
    public async Task<ListingReliabilityResult> CalculateAsync(Car car, CancellationToken cancellationToken = default)
    {
        var signals = await reliabilitySignalsProvider.GetSignalsAsync(car, cancellationToken);
        return reliabilityEngine.Compute(car, signals);
    }

    public async Task<IReadOnlyDictionary<int, ListingReliabilityResult>> CalculateBatchAsync(
        IReadOnlyList<Car> cars,
        CancellationToken cancellationToken = default)
    {
        if (cars.Count == 0)
            return new Dictionary<int, ListingReliabilityResult>();

        var signalsMap = await reliabilitySignalsProvider.GetSignalsForCarsAsync(cars, cancellationToken);
        var dict = new Dictionary<int, ListingReliabilityResult>(cars.Count);
        foreach (var car in cars)
        {
            if (!signalsMap.TryGetValue(car.Id, out var signals))
                signals = new ListingReliabilitySignals { CarId = car.Id };

            dict[car.Id] = reliabilityEngine.Compute(car, signals);
        }

        return dict;
    }
}
