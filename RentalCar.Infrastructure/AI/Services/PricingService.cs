using Microsoft.EntityFrameworkCore;
using RentalCar.Application.Abstractions.AI;
using RentalCar.Data.Enums;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.AI.Services
{
    public class PricingService : IPricingService
    {
        private readonly RentalCarContext _context;

        public PricingService(RentalCarContext context)
        {
            _context = context;
        }

        public async Task<object> CalculateRentalPriceAsync(
            int carId,
            RentalType rentalType,
            decimal duration,
            CancellationToken cancellationToken = default)
        {
            var car = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(x => x.Id == carId, cancellationToken);
            if (car is null)
                return new { found = false, message = "Arac bulunamadi." };

            if (duration <= 0)
                return new { found = true, valid = false, message = "Sure 0'dan buyuk olmali." };

            var daily = car.DailyPrice > 0 ? car.DailyPrice : (car.fiyat ?? 0);
            var weekly = car.WeeklyPrice > 0 ? car.WeeklyPrice : daily * 6;
            var monthly = car.MonthlyPrice > 0 ? car.MonthlyPrice : daily * 24;

            var total = rentalType switch
            {
                RentalType.Daily => daily * duration,
                RentalType.Weekly => weekly * duration,
                RentalType.Monthly => monthly * duration,
                RentalType.LongTerm => monthly * 12,
                _ => daily * duration
            };

            var optimized = OptimizeDuration(daily, weekly, monthly, duration);

            return new
            {
                found = true,
                valid = true,
                carId = car.Id,
                rentalType = rentalType.ToString(),
                duration,
                totalPrice = decimal.Round(total, 2),
                optimizedSuggestion = optimized
            };
        }

        private static object OptimizeDuration(decimal daily, decimal weekly, decimal monthly, decimal days)
        {
            var asDaily = daily * days;
            var asWeekly = weekly * Math.Ceiling(days / 7m);
            var asMonthly = monthly * Math.Ceiling(days / 30m);

            var best = new Dictionary<string, decimal>
            {
                ["Daily"] = asDaily,
                ["Weekly"] = asWeekly,
                ["Monthly"] = asMonthly
            }.OrderBy(x => x.Value).First();

            return new
            {
                recommendedPlan = best.Key,
                estimatedCost = decimal.Round(best.Value, 2),
                compared = new
                {
                    dailyCost = decimal.Round(asDaily, 2),
                    weeklyCost = decimal.Round(asWeekly, 2),
                    monthlyCost = decimal.Round(asMonthly, 2)
                }
            };
        }
    }
}
