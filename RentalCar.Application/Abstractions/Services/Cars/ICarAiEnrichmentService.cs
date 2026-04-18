using RentalCar.Application.Contracts.Cars;
using RentalCar.Domain.Entities;

namespace RentalCar.Application.Abstractions.Services.Cars;

public interface ICarAiEnrichmentService
{
    Task<CarAiEnrichmentResult> EnrichAsync(Car car, CancellationToken cancellationToken = default);
}
