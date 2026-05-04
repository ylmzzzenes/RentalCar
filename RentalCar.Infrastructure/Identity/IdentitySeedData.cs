using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.Infrastructure.Identity;

public static class IdentitySeedData
{
    private const string AdminUser = "Admin";
    private const string AdminPassword = "Admin1234,";
    private const string AdminEmail = "admin@enesyilmaz.com";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var context = scopedProvider.GetRequiredService<RentalCarContext>();
        await context.Database.MigrateAsync();

        var roleManager = scopedProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scopedProvider.GetRequiredService<UserManager<AppUser>>();

        var roles = new[] { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new AppRole { Name = role });
            }
        }

        var user = await userManager.FindByNameAsync(AdminUser);
        if (user == null)
        {
            user = new AppUser
            {
                FullName = "Enes Yılmaz",
                UserName = AdminUser,
                Email = AdminEmail,
                PhoneNumber = "05458309222"
            };

            await userManager.CreateAsync(user, AdminPassword);
        }

        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }

        await SeedCarsAsync(context);
    }

    private static async Task SeedCarsAsync(RentalCarContext context)
    {
        var stockImages = new[]
        {
            "https://images.pexels.com/photos/170811/pexels-photo-170811.jpeg",
            "https://images.pexels.com/photos/358070/pexels-photo-358070.jpeg",
            "https://images.pexels.com/photos/3802510/pexels-photo-3802510.jpeg",
            "https://images.pexels.com/photos/1149831/pexels-photo-1149831.jpeg",
            "https://images.pexels.com/photos/3729464/pexels-photo-3729464.jpeg"
        };

        var existingCars = await context.Cars.ToListAsync();
        var existingUpdated = false;
        for (var i = 0; i < existingCars.Count; i++)
        {
            if (existingCars[i].ImageUrls is null || existingCars[i].ImageUrls.Count == 0)
            {
                existingCars[i].ImageUrls = new List<string> { stockImages[i % stockImages.Length] };
                existingCars[i].ModifiedOn = DateTime.UtcNow;
                existingUpdated = true;
            }
        }
        if (existingUpdated)
            await context.SaveChangesAsync();

        if (await context.Cars.CountAsync() >= 30)
            return;

        var brands = new[] { "Toyota", "Renault", "Fiat", "Volkswagen", "Ford", "Peugeot", "Hyundai", "Honda", "BMW", "Mercedes" };
        var models = new[] { "Corolla", "Megane", "Egea", "Golf", "Focus", "308", "i20", "Civic", "320i", "C200" };
        var cities = new[] { "Istanbul", "Ankara", "Izmir", "Bursa", "Antalya", "Konya" };
        var colors = new[] { "Beyaz", "Siyah", "Gri", "Mavi", "Kirmizi" };

        var seedCars = new List<Car>();
        var random = new Random(42);
        var existingCount = await context.Cars.CountAsync();
        var target = 30 - existingCount;
        for (var idx = 0; idx < target; idx++)
        {
            var brand = brands[idx % brands.Length];
            var model = models[idx % models.Length];
            var year = 2014 + (idx % 12);
            var km = 15_000 + (idx * 7_500);
            var listed = 650_000m + (idx * 35_000m);
            var city = cities[idx % cities.Length];
            var fuel = (idx % 4) switch
            {
                0 => FuelType.Benzin,
                1 => FuelType.Dizel,
                2 => FuelType.Hibrit,
                _ => FuelType.Elektrik
            };
            var gear = (idx % 3) switch
            {
                0 => Gear.Otomatik,
                1 => Gear.Manuel,
                _ => Gear.YarıOtomatik
            };
            var body = (idx % 4) switch
            {
                0 => BodyType.Sedan,
                1 => BodyType.Hatchback,
                2 => BodyType.Suv,
                _ => BodyType.StationWagon
            };

            seedCars.Add(new Car
            {
                Vin = $"VIN{idx + 1000:D8}",
                Brand = brand,
                CatalogBrand = brand,
                Model = model,
                CatalogModelName = model,
                Plate = $"34 ABC {100 + idx}",
                ModelYear = year,
                OdometerKm = km,
                FuelType = fuel,
                Transmission = gear,
                Drivetrain = idx % 2 == 0 ? RentalCar.Domain.Enums.DriveType.FWD : RentalCar.Domain.Enums.DriveType.AWD,
                BodyType = body,
                BodyStyleLabel = body.ToString(),
                Color = colors[idx % colors.Length],
                City = city,
                ListedPrice = listed,
                PredictedPriceMin = Math.Round(listed * 0.9m, 0),
                PredictedPriceMid = listed,
                PredictedPriceMax = Math.Round(listed * 1.1m, 0),
                DailyPrice = Math.Max(900, Math.Round(listed / 1200m, 0)),
                WeeklyPrice = Math.Max(6000, Math.Round(listed / 240m, 0)),
                MonthlyPrice = Math.Max(18000, Math.Round(listed / 90m, 0)),
                ImageUrls = new List<string> { stockImages[idx % stockImages.Length] },
                Security = Security.ABS,
                InternalEquipment = InternalEquipment.Climate,
                ExternalEquipment = ExternalEquipment.Sunroof,
                HasAccidentRecord = idx % 5 == 0 ? 1 : 0,
                HasServiceHistory = 1,
                PreviousOwnerCount = random.Next(1, 4),
                EnginePowerHp = 95 + (idx % 8) * 15,
                TorqueNm = 170 + (idx % 8) * 20,
                EngineDisplacementLiters = 1.2 + ((idx % 5) * 0.2),
                FuelConsumptionLPer100Km = 4.5 + ((idx % 5) * 0.4),
                BodyWorkNotes = idx % 5 == 0 ? "2 parca boyali" : "Temiz",
                ShortDescription = $"{brand} {model} hem kiralama hem satis icin uygun bir ilandir.",
                FullDescription = $"{year} model {brand} {model}, {km:N0} km ve {city} lokasyonundadir. Kiralama ve satis icin hazirdir.",
                TaxAmount = 1500 + idx * 25,
                IsApproved = true,
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            });
        }

        if (seedCars.Count > 0)
        {
            await context.Cars.AddRangeAsync(seedCars);
            await context.SaveChangesAsync();
        }
    }
}
