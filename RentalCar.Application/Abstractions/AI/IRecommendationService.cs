using RentalCar.Application.AI.Models;

namespace RentalCar.Application.Abstractions.AI
{
    public interface IRecommendationService
    {
        Task<List<ChatCarCard>> RecommendCarsAsync(
            string? city,
            string? preference,
            string? fuelType,
            decimal? maxPrice,
            CancellationToken cancellationToken = default);
    }
}
