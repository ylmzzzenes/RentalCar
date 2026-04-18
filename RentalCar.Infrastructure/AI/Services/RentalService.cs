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
        string? searchQuery,
        string? city,
        string? vehicleType,
        decimal? minPrice,
        decimal? maxPrice,
        string? fuelType,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Car> BuildQuery(bool includeCity)
        {
            var q = _context.Cars
                .AsNoTracking()
                .Where(x => x.IsApproved)
                .AsQueryable();

            if (includeCity && !string.IsNullOrWhiteSpace(city))
            {
                var c = city.Trim();
                q = q.Where(x => x.City != null && x.City.Contains(c));
            }

            if (minPrice.HasValue)
                q = q.Where(x => (x.ListedPrice ?? x.DailyPrice) >= minPrice.Value);

            if (maxPrice.HasValue)
                q = q.Where(x => (x.ListedPrice ?? x.DailyPrice) <= maxPrice.Value);

            if (!string.IsNullOrWhiteSpace(vehicleType))
            {
                var body = ParseBodyType(vehicleType);
                if (body != BodyType.None)
                    q = q.Where(x => (x.BodyType & body) == body);
            }

            if (!string.IsNullOrWhiteSpace(fuelType))
            {
                var fuel = ParseFuelType(fuelType);
                if (fuel != FuelType.None)
                    q = q.Where(x => (x.FuelType & fuel) == fuel);
            }

            foreach (var term in SplitSearchTerms(searchQuery))
            {
                var t = term;
                if (t.Contains('%') || t.Contains('_') || t.Contains('[')) continue;
                q = q.Where(c =>
                    (c.Brand != null && c.Brand.Contains(t)) ||
                    (c.CatalogBrand != null && c.CatalogBrand.Contains(t)) ||
                    (c.Model != null && c.Model.Contains(t)) ||
                    (c.CatalogModelName != null && c.CatalogModelName.Contains(t)) ||
                    (c.Series != null && c.Series.Contains(t)) ||
                    (c.Color != null && c.Color.Contains(t)) ||
                    (c.City != null && c.City.Contains(t)) ||
                    (c.EngineCode != null && c.EngineCode.Contains(t)));
            }

            return q;
        }

        var query = BuildQuery(includeCity: true);
        var cars = await query
            .OrderBy(x => x.ListedPrice ?? x.DailyPrice)
            .Take(8)
            .ToListAsync(cancellationToken);

        if (cars.Count == 0 && !string.IsNullOrWhiteSpace(city) &&
            (!string.IsNullOrWhiteSpace(searchQuery) || !string.IsNullOrWhiteSpace(vehicleType) || !string.IsNullOrWhiteSpace(fuelType) || minPrice.HasValue || maxPrice.HasValue))
        {
            query = BuildQuery(includeCity: false);
            cars = await query
                .OrderBy(x => x.ListedPrice ?? x.DailyPrice)
                .Take(8)
                .ToListAsync(cancellationToken);
        }

        return cars.Select(MapCarCard).ToList();
    }

    private static IEnumerable<string> SplitSearchTerms(string? searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            yield break;

        var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ilan", "arac", "araba", "otomobil", "bul", "ara", "kirala", "kiralik", "satilik", "satin", "icin", "veya", "ile", "the", "ve", "bir", "gun", "gunluk", "hafta", "ay"
        };

        foreach (var raw in searchQuery.Split(new[] { ' ', ',', ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var t = raw.Trim();
            if (t.Length < 2) continue;
            if (t.Length == 4 && t.All(char.IsAsciiDigit)) continue;
            var norm = t.ToLowerInvariant().Replace('ı', 'i');
            if (stop.Contains(norm)) continue;
            yield return t;
        }
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
            ImageUrl = firstImage is null
                ? null
                : (firstImage.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? firstImage
                    : "/Images/Upload/" + firstImage),
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
        if (text.Contains("lpg")) return FuelType.Benzin;
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
