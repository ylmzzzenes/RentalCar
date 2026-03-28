using RentalCar.Data.Enums;

namespace RentalCar.Application.Abstractions.AI
{
    public interface IPricingService
    {
        Task<object> CalculateRentalPriceAsync(
            int carId,
            RentalType rentalType,
            decimal duration,
            CancellationToken cancellationToken = default);
    }
}
