using Microsoft.EntityFrameworkCore;
using RentalCar.Application.Abstractions.AI;
using RentalCar.Application.AI.Models;
using RentalCar.Data.Enums;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.AI.Services
{
    public class RentalService : IRentalService
    {
        private readonly RentalCarContext _context;

        public RentalService(RentalCarContext context)
        {
            _context = context;
        }

        public async Task<List<ChatCarCard>> SearchCarsAsync(
            string? city,
            string? vehicleType,
            decimal? minPrice,
            decimal? maxPrice,
            string? fuelType,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Cars
                .AsNoTracking()
                .Where(x => x.IsApproved)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(city))
            {
                var c = city.Trim();
                query = query.Where(x => x.sehir != null && x.sehir.Contains(c));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(x => (x.fiyat ?? x.DailyPrice) >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(x => (x.fiyat ?? x.DailyPrice) <= maxPrice.Value);
            }

            if (!string.IsNullOrWhiteSpace(vehicleType))
            {
                var body = ParseBodyType(vehicleType);
                if (body != BodyType.None)
                    query = query.Where(x => (x.BodyType & body) == body);
            }

            if (!string.IsNullOrWhiteSpace(fuelType))
            {
                var fuel = ParseFuelType(fuelType);
                if (fuel != FuelType.None)
                    query = query.Where(x => (x.yakitTuru & fuel) == fuel);
            }

            var cars = await query
                .OrderBy(x => x.fiyat ?? x.DailyPrice)
                .Take(8)
                .ToListAsync(cancellationToken);

            return cars.Select(MapCarCard).ToList();
        }

        public async Task<object> GetCarDetailAsync(int carId, CancellationToken cancellationToken = default)
        {
            var car = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(x => x.Id == carId, cancellationToken);
            if (car is null)
                return new { found = false, message = "Arac bulunamadi." };

            return new
            {
                found = true,
                carId = car.Id,
                title = BuildTitle(car),
                city = car.sehir,
                dailyPrice = car.DailyPrice,
                weeklyPrice = car.WeeklyPrice,
                monthlyPrice = car.MonthlyPrice,
                listedPrice = car.fiyat,
                fuelType = car.yakitTuru.ToString(),
                bodyType = car.BodyType.ToString(),
                description = car.aciklama_kisa ?? car.aciklama
            };
        }

        internal static ChatCarCard MapCarCard(Data.Models.Car car)
        {
            var firstImage = car.ImageUrls?.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return new ChatCarCard
            {
                CarId = car.Id,
                Title = BuildTitle(car),
                City = car.sehir,
                Price = car.fiyat ?? car.DailyPrice,
                PriceUnit = "TL/gun",
                ImageUrl = firstImage is null ? null : "/Images/Upload/" + firstImage,
                FuelType = car.yakitTuru.ToString(),
                BodyType = car.BodyType.ToString()
            };
        }

        private static string BuildTitle(Data.Models.Car car)
            => ((car.marka ?? car.Brand ?? "Arac") + " " + (car.model_adi ?? car.model ?? string.Empty)).Trim();

        private static FuelType ParseFuelType(string fuelType)
        {
            var text = Normalize(fuelType);
            if (text.Contains("dizel")) return FuelType.Dizel;
            if (text.Contains("hibrit")) return FuelType.Hibrit;
            if (text.Contains("elektr")) return FuelType.Elektrik;
            if (text.Contains("benzin")) return FuelType.Benzin;
            return FuelType.None;
        }

        private static BodyType ParseBodyType(string vehicleType)
        {
            var text = Normalize(vehicleType);
            if (text.Contains("suv")) return BodyType.Suv;
            if (text.Contains("sedan")) return BodyType.Sedan;
            if (text.Contains("hatch")) return BodyType.Hatchback;
            if (text.Contains("coupe")) return BodyType.Coupe;
            if (text.Contains("pickup") || text.Contains("pick up")) return BodyType.PıckUp;
            if (text.Contains("minivan")) return BodyType.Minivan;
            return BodyType.None;
        }

        private static string Normalize(string value)
            => value.ToLowerInvariant().Replace('ı', 'i');
    }
}
