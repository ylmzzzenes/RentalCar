using RentalCar.Application.Abstractions.Services.Rentals;
using RentalCar.Application.Dtos.Rentals;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;

namespace RentalCar.Infrastructure.Services.Rentals
{
    public sealed class RentalAppService : IRentalAppService
    {
        private readonly RentalServices _rentalServices;

        public RentalAppService(RentalServices rentalServices)
        {
            _rentalServices = rentalServices;
        }
        public async Task<RentalResultDto> CreateRentalAsync(int carId, RentalType rentalType, int duration, CancellationToken cancellationToken = default)
        {
            if (duration <= 0)
            {
                return new RentalResultDto
                {
                    Success = false,
                    ErrorMessage = "Kiralama süresi 0'dan büyük olmalıdır."
                };
            }

            var car = await _rentalServices.GetCarByIdAsync(carId, cancellationToken);
            if(car == null)
            {
                return new RentalResultDto
                {
                    Success = false,
                    ErrorMessage = "Belirtilen araba bulunamadı."
                };
            }

            var rental = new Rental
            {
                CarId = carId,
                RentalType = rentalType,
                Duration = duration,
                StartDate = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };

            await _rentalServices.CreateAsync(rental, cancellationToken);

            rental.Car = car;

            return new RentalResultDto
            {
                Success = true,
                Rental = rental
            };
        }

        public async Task<RentalPageDto?> GetRentCarPageAsync(int carId, CancellationToken cancellationToken = default)
        {
            var car = await _rentalServices.GetCarByIdAsync(carId, cancellationToken);
            if (car == null)
                return null;
            return new RentalPageDto
            {
                CarId = car.Id,
                Brand = car.Brand ?? string.Empty,
                Model = car.Model ?? string.Empty,
                Plate = car.Plate ?? string.Empty,
                ModelYear = car.ModelYear,

                FuelType = car.FuelType.ToString(),
                Transmission = car.Transmission.ToString(),
                BodyType = car.BodyType.ToString(),

                Color = car.Color ?? string.Empty,
                Security = car.Security.ToString(),
                InternalEquipment = car.InternalEquipment.ToString(),
                ExternalEquipment = car.ExternalEquipment.ToString(),
                ImageUrls = car.ImageUrls?.ToList() ?? new List<string>(),

                DailyPrice = car.DailyPrice,
                WeeklyPrice = car.WeeklyPrice,
                MonthlyPrice = car.MonthlyPrice,

                RentalType = RentalType.Daily,
                Duration = 1
            };
        }

        public async Task<RentalResultDto?> GetRentalresultAsync(int rentalId, CancellationToken cancellationToken = default)
        {
            var rental = await _rentalServices.GetRentalByIdWithCarAsync(rentalId, cancellationToken);
            if (rental == null)
                return null;

            return new RentalResultDto
            {
                Success = true,
                Rental = rental
            };
            
        }

       
    }
}
