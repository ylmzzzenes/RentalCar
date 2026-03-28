using RentalCar.Application.AI.Models;

namespace RentalCar.Application.Abstractions.AI
{
    public interface IRentalService
    {
        Task<List<ChatCarCard>> SearchCarsAsync(
            string? city,
            string? vehicleType,
            decimal? minPrice,
            decimal? maxPrice,
            string? fuelType,
            CancellationToken cancellationToken = default);

        Task<object> GetCarDetailAsync(int carId, CancellationToken cancellationToken = default);
    }
}
