using Microsoft.EntityFrameworkCore;
using RentalCar.Application.Abstractions.AI;
using RentalCar.Application.AI.Models;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.AI.Services;

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
            query = query.Where(x => x.City != null && x.City.Contains(c));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(x => (x.ListedPrice ?? x.DailyPrice) >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x => (x.ListedPrice ?? x.DailyPrice) <= maxPrice.Value);
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
                query = query.Where(x => (x.FuelType & fuel) == fuel);
        }

        var cars = await query
            .OrderBy(x => x.ListedPrice ?? x.DailyPrice)
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
            city = car.City,
            dailyPrice = car.DailyPrice,
            weeklyPrice = car.WeeklyPrice,
            monthlyPrice = car.MonthlyPrice,
            listedPrice = car.ListedPrice,
            fuelType = car.FuelType.ToString(),
            bodyType = car.BodyType.ToString(),
            description = car.ShortDescription ?? car.FullDescription
        };
    }

    internal static ChatCarCard MapCarCard(Car car)
    {
        var firstImage = car.ImageUrls.FirstOrDefault();
        return new ChatCarCard
        {
            CarId = car.Id,
            Title = BuildTitle(car),
            City = car.City,
            Price = car.ListedPrice ?? car.DailyPrice,
            PriceUnit = "TL/gun",
            ImageUrl = firstImage is null ? null : "/Images/Upload/" + firstImage,
            FuelType = car.FuelType.ToString(),
            BodyType = car.BodyType.ToString()
        };
    }

    private static string BuildTitle(Car car)
        => ((car.CatalogBrand ?? car.Brand ?? "Arac") + " " + (car.CatalogModelName ?? car.Model ?? string.Empty)).Trim();

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
