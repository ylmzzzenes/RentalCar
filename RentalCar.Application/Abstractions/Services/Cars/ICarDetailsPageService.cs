using RentalCar.Application.Contracts.Cars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Application.Abstractions.Services.Cars
{
    public interface ICarDetailsPageService
    {
        Task<CarDetailsPageResult?> BuildAsync(int carId, string? currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    }
}
