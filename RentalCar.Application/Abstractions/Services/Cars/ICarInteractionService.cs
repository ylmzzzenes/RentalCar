using RentalCar.Application.Dtos.Cars;

namespace RentalCar.Application.Abstractions.Services.Cars
{
    public interface ICarInteractionService
    {
        Task<CarDetailsDto> GetCarDetailAsync(int CarId, string? userId, bool isAdmin, CancellationToken cancellatonToken = default);
        Task ToggledFavoriteAsync(int carId, string userId, CancellationToken cancellationToken = default);
        Task RateAsync(int carId, string userId, int score, CancellationToken cancellationToken = default);
        Task AddCommentAsync(int carId, string userId, string content, CancellationToken cancellationToken = default);

    }
}
