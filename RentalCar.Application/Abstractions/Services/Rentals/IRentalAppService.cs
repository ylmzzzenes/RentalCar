using RentalCar.Application.Dtos.Rentals;
using RentalCar.Domain.Enums;

namespace RentalCar.Application.Abstractions.Services.Rentals
{
    public interface IRentalAppService
    {
        Task<RentalPageDto?> GetRentCarPageAsync(int carId, CancellationToken cancellationToken = default);
        Task<RentalResultDto> CreateRentalAsync(int carId, RentalType rentalType, decimal duration, DateTime startDate, string? userId = null, CancellationToken cancellationToken = default);
        Task<RentalResultDto?> GetRentalresultAsync(int rentalId, CancellationToken cancellationToken = default);
    }
}
