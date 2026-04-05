using Microsoft.EntityFrameworkCore;
using RentalCar.Application.Abstractions.AI;
using RentalCar.Application.AI.Models;
using RentalCar.Domain.Entities;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.AI.Services;

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
            query = query.Where(x => x.City != null && x.City.Contains(city));

        if (maxPrice.HasValue)
            query = query.Where(x => (x.ListedPrice ?? x.DailyPrice) <= maxPrice.Value);

        if (!string.IsNullOrWhiteSpace(fuelType))
            query = query.Where(x => x.FuelType.ToString().Contains(fuelType, StringComparison.OrdinalIgnoreCase));

        var cars = await query.Take(40).ToListAsync(cancellationToken);

        var scored = cars
            .Select(car => new
            {
                Car = car,
                Score = CalculateScore(car, pref)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Car.ListedPrice ?? x.Car.DailyPrice)
            .Take(6)
            .Select(x => RentalService.MapCarCard(x.Car))
            .ToList();

        return scored;
    }

    private static int CalculateScore(Car car, string preference)
    {
        var score = 0;

        if (preference.Contains("az yakan") || preference.Contains("ekonomik"))
        {
            if (car.FuelConsumptionLPer100Km.HasValue && car.FuelConsumptionLPer100Km.Value <= 6.5) score += 30;
            if (car.FuelType.ToString().Contains("Dizel", StringComparison.OrdinalIgnoreCase) ||
                car.FuelType.ToString().Contains("Hibrit", StringComparison.OrdinalIgnoreCase) ||
                car.FuelType.ToString().Contains("Elektrik", StringComparison.OrdinalIgnoreCase))
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
            if (car.EnginePowerHp.HasValue && car.EnginePowerHp.Value > 170) score += 25;
        }

        if (car.ListedPrice.HasValue && car.ListedPrice.Value > 0)
            score += (int)Math.Max(0, 20 - (double)(car.ListedPrice.Value / 5000m));

        return score;
    }
}
