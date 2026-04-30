using RentalCar.Application.Abstractions.Services.Rentals;
using RentalCar.Application.Dtos.Rentals;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;
using RentalCar.Domain.Extensions;
using RentalCar.Domain.Rules;

namespace RentalCar.Infrastructure.Services.Rentals
{
    public sealed class RentalAppService : IRentalAppService
    {
        private readonly RentalServices _rentalServices;

        public RentalAppService(RentalServices rentalServices)
        {
            _rentalServices = rentalServices;
        }
        public async Task<RentalResultDto> CreateRentalAsync(int carId, RentalType rentalType, decimal duration, DateTime startDate, string? userId = null, CancellationToken cancellationToken = default)
        {
            if (duration <= 0)
            {
                return new RentalResultDto
                {
                    Success = false,
                    ErrorMessage = "Kiralama süresi 0'dan büyük olmalıdır."
                };
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return new RentalResultDto
                {
                    Success = false,
                    ErrorMessage = "Kiralama için giriş yapmalısınız."
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

            if (!car.IsApproved)
            {
                return new RentalResultDto
                {
                    Success = false,
                    ErrorMessage = "Bu araç henüz onaylı değil."
                };
            }

            var utcStart = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
            var computedEnd = RentalDateRules.ComputeEndUtc(utcStart, rentalType, duration);
            var hasOverlap = await _rentalServices.HasOverlappingRentalAsync(carId, utcStart, computedEnd, cancellationToken);
            if (hasOverlap)
            {
                return new RentalResultDto
                {
                    Success = false,
                    ErrorMessage = "Secilen tarih araliginda bu arac zaten kiralanmis."
                };
            }

            var total = RentalPricing.ComputeTotal(car, rentalType, duration);
            if (total <= 0)
            {
                return new RentalResultDto
                {
                    Success = false,
                    ErrorMessage = "Bu araç için kiralama fiyatı tanımlı değil. Lütfen günlük / ilan fiyatını kontrol edin."
                };
            }

            var rental = new Rental
            {
                CarId = carId,
                UserId = userId,
                RentalType = rentalType,
                Duration = duration,
                TotalPrice = total,
                StartDate = utcStart,
                Status = RentalStatus.Confirmed,
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

                FuelType = car.FuelType.GetDisplayName(),
                Transmission = car.Transmission.GetDisplayName(),
                BodyType = car.BodyType.GetDisplayName(),

                Color = car.Color ?? string.Empty,
                Security = car.Security.ToString(),
                InternalEquipment = car.InternalEquipment.ToString(),
                ExternalEquipment = car.ExternalEquipment.ToString(),
                ImageUrls = car.ImageUrls?.ToList() ?? new List<string>(),

                DailyPrice = car.DailyPrice,
                WeeklyPrice = car.WeeklyPrice,
                MonthlyPrice = car.MonthlyPrice,

                RentalType = RentalType.Daily,
                Duration = 1,
                StartDate = DateTime.Today
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
