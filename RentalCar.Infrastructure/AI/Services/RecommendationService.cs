using Microsoft.EntityFrameworkCore;
using RentalCar.Application.Abstractions.AI;
using RentalCar.Application.AI.Models;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.AI.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly RentalCarContext _context;

        public RecommendationService(RentalCarContext context)
        {
            _context = context;
        }

        public async Task<List<ChatCarCard>> RecommendCarsAsync(
            string? city,
            string? preference,
            string? fuelType,
            decimal? maxPrice,
            CancellationToken cancellationToken = default)
        {
            var pref = (preference ?? string.Empty).ToLowerInvariant();

            var query = _context.Cars
                .AsNoTracking()
                .Where(x => x.IsApproved)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(city))
                query = query.Where(x => x.sehir != null && x.sehir.Contains(city));

            if (maxPrice.HasValue)
                query = query.Where(x => (x.fiyat ?? x.DailyPrice) <= maxPrice.Value);

            if (!string.IsNullOrWhiteSpace(fuelType))
                query = query.Where(x => x.yakitTuru.ToString().Contains(fuelType, StringComparison.OrdinalIgnoreCase));

            var cars = await query.Take(40).ToListAsync(cancellationToken);

            var scored = cars
                .Select(car => new
                {
                    Car = car,
                    Score = CalculateScore(car, pref)
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Car.fiyat ?? x.Car.DailyPrice)
                .Take(6)
                .Select(x => RentalService.MapCarCard(x.Car))
                .ToList();

            return scored;
        }

        private static int CalculateScore(Data.Models.Car car, string preference)
        {
            var score = 0;

            if (preference.Contains("az yakan") || preference.Contains("ekonomik"))
            {
                if (car.lt_100km.HasValue && car.lt_100km.Value <= 6.5) score += 30;
                if (car.yakitTuru.ToString().Contains("Dizel", StringComparison.OrdinalIgnoreCase) ||
                    car.yakitTuru.ToString().Contains("Hibrit", StringComparison.OrdinalIgnoreCase) ||
                    car.yakitTuru.ToString().Contains("Elektrik", StringComparison.OrdinalIgnoreCase))
                    score += 25;
            }

            if (preference.Contains("aile"))
            {
                if (car.BodyType.ToString().Contains("Suv", StringComparison.OrdinalIgnoreCase) ||
                    car.BodyType.ToString().Contains("StationWagon", StringComparison.OrdinalIgnoreCase) ||
                    car.BodyType.ToString().Contains("Minivan", StringComparison.OrdinalIgnoreCase))
                    score += 25;
            }

            if (preference.Contains("performans"))
            {
                if (car.motorGuc_hp.HasValue && car.motorGuc_hp.Value > 170) score += 25;
            }

            if (car.fiyat.HasValue && car.fiyat.Value > 0)
                score += (int)Math.Max(0, 20 - (double)(car.fiyat.Value / 5000m));

            return score;
        }
    }
}
