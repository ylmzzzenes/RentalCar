using RentalCar.Application.Contracts.Cars;
using RentalCar.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Application.Abstractions.Services.Cars
{
    public interface ICarPriceInsightService
    {
        Task<CarPriceInsightResult> AnalyzeAsync(
        Car car,
        IReadOnlyList<Car> comparableCars,
        CancellationToken cancellationToken = default);
    }
}
