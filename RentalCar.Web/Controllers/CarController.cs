using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalCar.Application.Dtos.AI;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;
using RentalCar.Infrastructure.AI.Services;
using RentalCar.Infrastructure.Persistence.Context;
using RentalCar.Infrastructure.Services;
using RentalCar.ViewModels;
using RentalCar.Web.Models.Requests;
using System.Security.Claims;
using VehicleDriveType = RentalCar.Domain.Enums.DriveType;
using RentalCar.Application.Abstractions.Services;
using System.Xml.Linq;

namespace RentalCar.Controllers;

public class CarController : Controller
{
    private readonly CarServices _carsServices;
    private readonly PricingApiClient _pricing;
    private readonly DescriptionService _descriptionService;
    private readonly UserManager<AppUser> _userManager;
    public readonly ICarInteractionService _carInteractionService;

    public CarController(CarServices carServices, PricingApiClient pricing, DescriptionService descriptionService, RentalCarContext context, UserManager<AppUser> userManager, ICarInteractionService carInteractionService)
    {
        _carsServices = carServices;
        _pricing = pricing;
        _descriptionService = descriptionService;
        _userManager = userManager;
        _carInteractionService = carInteractionService;
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Index() { return View(); }

    [HttpGet]
    public IActionResult Get(int id)
    {
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateCar request, List<int> SelectedSecurity, List<int> SelectedInternal, List<int> SelectedExternal, List<int> SelectedBodyType, List<int> SelectedFuelType, List<int> SelectedGear, List<int> SelectedDriveType, CancellationToken cancellationToken = default)
    {
        Car newCar = new Car();
        var fileNames = new List<string>();

        if (request.PhotoUploads != null && request.PhotoUploads.Any())
        {
            foreach (var file in request.PhotoUploads)
            {
                var extension = Path.GetExtension(file.FileName);
                var newFileName = Guid.NewGuid() + extension;
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images/Upload/", newFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                using var stream = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(stream);

                fileNames.Add(newFileName);
            }
        }

        newCar.ImageUrls = fileNames;

        var x = DateTime.Now;

        newCar.Model = request.Model;
        newCar.ModelYear = request.ModelYear;
        newCar.Color = request.Color;

        newCar.ModelRaw = request.ModelRaw;
        newCar.CatalogBrand = request.CatalogBrand;
        newCar.CatalogModelName = request.CatalogModelName;
        newCar.TrimPackage = request.TrimPackage;
        newCar.EngineCode = request.EngineCode;
        newCar.TransmissionCode = request.TransmissionCode;

        newCar.OdometerKm = request.OdometerKm;
        newCar.TaxAmount = request.TaxAmount;
        newCar.FuelConsumptionLPer100Km = request.FuelConsumptionLPer100Km;
        newCar.EngineDisplacementLiters = request.EngineDisplacementLiters;
        newCar.ListedPrice = request.ListedPrice;
        newCar.City = request.City;
        newCar.TrimLevelLabel = request.TrimLevelLabel;
        newCar.HasAccidentRecord = request.HasAccidentRecord;
        newCar.HasServiceHistory = request.HasServiceHistory;
        newCar.EnginePowerHp = request.EnginePowerHp;
        newCar.TorqueNm = request.TorqueNm;
        newCar.PreviousOwnerCount = request.PreviousOwnerCount;
        newCar.BodyStyleLabel = request.BodyStyleLabel;
        newCar.BodyWorkNotes = request.BodyWorkNotes;

        newCar.Security = (SelectedSecurity != null && SelectedSecurity.Any())
            ? SelectedSecurity.Aggregate(Security.None, (acc, val) => acc | (Security)val)
            : Security.None;

        newCar.InternalEquipment = (SelectedInternal != null && SelectedInternal.Any())
            ? SelectedInternal.Aggregate(InternalEquipment.None, (acc, val) => acc | (InternalEquipment)val)
            : InternalEquipment.None;

        newCar.ExternalEquipment = (SelectedExternal != null && SelectedExternal.Any())
            ? SelectedExternal.Aggregate(ExternalEquipment.None, (acc, val) => acc | (ExternalEquipment)val)
            : ExternalEquipment.None;

        newCar.BodyType = (SelectedBodyType != null && SelectedBodyType.Any())
            ? SelectedBodyType.Aggregate(BodyType.None, (acc, val) => acc | (BodyType)val)
            : BodyType.None;

        newCar.FuelType = (SelectedFuelType != null && SelectedFuelType.Any())
            ? SelectedFuelType.Aggregate(FuelType.None, (acc, val) => acc | (FuelType)val)
            : FuelType.None;

        newCar.Transmission = (SelectedGear != null && SelectedGear.Any())
            ? SelectedGear.Aggregate(Gear.None, (acc, val) => acc | (Gear)val)
            : Gear.None;

        newCar.Drivetrain = (SelectedDriveType != null && SelectedDriveType.Any())
            ? SelectedDriveType.Aggregate(VehicleDriveType.None, (acc, val) => acc | (VehicleDriveType)val)
            : VehicleDriveType.None;

        if (newCar.FuelType == FuelType.None && request.FuelType != FuelType.None)
            newCar.FuelType = request.FuelType;
        if (newCar.Transmission == Gear.None && request.Transmission != Gear.None)
            newCar.Transmission = request.Transmission;
        if (newCar.Drivetrain == VehicleDriveType.None && request.Drivetrain != VehicleDriveType.None)
            newCar.Drivetrain = request.Drivetrain;

        newCar.CreatedOn = x;
        newCar.ModifiedOn = x;

        try
        {
            var payload = MapCarToPredict(newCar);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(payload));

            var pred = await _pricing.PredictAsync(payload, cancellationToken);

            var mid = pred.Mid ?? pred.Prediction;
            if (mid == null)
                throw new Exception("Python response içinde mid/prediction yok.");

            var low = pred.Low ?? mid.Value;
            var high = pred.High ?? mid.Value;

            newCar.PredictedPriceMid = mid.Value;
            newCar.PredictedPriceMin = low;
            newCar.PredictedPriceMax = high;

            if (newCar.ListedPrice == 0)
                newCar.ListedPrice = newCar.PredictedPriceMid;

            Dictionary<string, object?> dataDict = new Dictionary<string, object?>
            {
                ["model"] = newCar.Model,
                ["marka"] = newCar.CatalogBrand,
                ["model_adi"] = newCar.CatalogModelName,
                ["paket"] = newCar.TrimPackage,
                ["motor_kodu"] = newCar.EngineCode,
                ["sanziman_kodu"] = newCar.TransmissionCode,
                ["yil"] = newCar.ModelYear,
                ["kilometre"] = newCar.OdometerKm,
                ["yakitTuru"] = newCar.FuelType.ToString(),
                ["vites"] = newCar.Transmission.ToString(),
                ["cekis"] = newCar.Drivetrain.ToString(),
                ["vergi"] = newCar.TaxAmount,
                ["lt_100km"] = newCar.FuelConsumptionLPer100Km,
                ["motorHacmi"] = newCar.EngineDisplacementLiters,
                ["renk"] = newCar.Color,
                ["sehir"] = newCar.City,
                ["kasaTipi"] = newCar.BodyStyleLabel,
                ["donanimSeviyesi"] = newCar.TrimLevelLabel,
                ["hasarKaydi"] = newCar.HasAccidentRecord,
                ["degisenBoyanan"] = newCar.BodyWorkNotes,
                ["servisGecmisi"] = newCar.HasServiceHistory,
                ["motorGuc_hp"] = newCar.EnginePowerHp,
                ["tork_nm"] = newCar.TorqueNm,
                ["sahipSayisi"] = newCar.PreviousOwnerCount,
            };

            var descReq = new DescribeRequestDto
            {
                data = dataDict,
                predicted_mid = newCar.PredictedPriceMid,
                predicted_low = newCar.PredictedPriceMin,
                predicted_high = newCar.PredictedPriceMax
            };

            var desc = await _descriptionService.DescribeAsync(descReq, cancellationToken);

            newCar.ShortDescription = desc.@short;
            newCar.FullDescription = desc.@long;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        await _carsServices.CreateAsync(newCar, cancellationToken);
        return RedirectToAction(nameof(Details), new { id = newCar.Id });
    }

    private static PredictRequestDto MapCarToPredict(Car car)
    {
        var yilInt = car.ModelYear ?? 0;

        return new PredictRequestDto
        {
            marka = car.CatalogBrand ?? car.Brand ?? "Bilinmiyor",
            model_adi = car.CatalogModelName ?? car.Model ?? "Bilinmiyor",
            paket = car.TrimPackage ?? "Standard",
            motor_kodu = car.EngineCode ?? "Bilinmiyor",
            cekis = car.Drivetrain.ToString(),
            sanziman_kodu = car.TransmissionCode ?? "Bilinmiyor",
            vites = car.Transmission.ToString(),
            yakitTuru = car.FuelType.ToString(),
            renk = car.Color ?? "Bilinmiyor",
            sehir = car.City,
            kasaTipi = car.BodyStyleLabel,
            donanimSeviyesi = car.TrimLevelLabel,
            hasarKaydi = car.HasAccidentRecord,
            degisenBoyanan = car.BodyWorkNotes,
            servisGecmisi = car.HasServiceHistory,
            motorGuc_hp = car.EnginePowerHp,
            tork_nm = car.TorqueNm,
            sahipSayisi = car.PreviousOwnerCount,
            yil = yilInt,
            kilometre = car.OdometerKm ?? 0,
            vergi = car.TaxAmount ?? 0,
            lt_100km = car.FuelConsumptionLPer100Km ?? 0.0,
            motorHacmi = car.EngineDisplacementLiters ?? 0.0,
        };
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(string? brand, string? model, string? renk, FuelType? yakitTuru, Gear? vites, BodyType? bodyType, int? yil, string? searchString, CancellationToken cancellationToken = default)
    {
        var all = await _carsServices.GetAllAsync(cancellationToken);
        if (all == null)
            return Problem("Cars is null");

        var query = all.AsQueryable();
        if (!User.IsInRole("Admin"))
            query = query.Where(c => c.IsApproved);

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var s = searchString.Trim();

            query = query.Where(c =>
                (c.Brand != null && c.Brand.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (c.Model != null && c.Model.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (c.Color != null && c.Color.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (c.CatalogBrand != null && c.CatalogBrand.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (c.CatalogModelName != null && c.CatalogModelName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (c.TrimPackage != null && c.TrimPackage.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (c.EngineCode != null && c.EngineCode.Contains(s, StringComparison.OrdinalIgnoreCase))
            );
        }

        if (!string.IsNullOrWhiteSpace(brand))
            query = query.Where(c => c.Brand != null && c.Brand.Contains(brand, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(model))
            query = query.Where(c => c.Model != null && c.Model.Contains(model, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(renk))
            query = query.Where(c => c.Color != null && c.Color.Contains(renk, StringComparison.OrdinalIgnoreCase));

        if (yakitTuru != null)
            query = query.Where(c => c.FuelType == yakitTuru);

        if (vites != null)
            query = query.Where(c => c.Transmission == vites);

        if (bodyType != null)
            query = query.Where(c => c.BodyType == bodyType);

        if (yil != null)
            query = query.Where(c => c.ModelYear == yil);

        return View(query.ToList());
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");

        var result = await _carInteractionService.GetCarDetailAsync(
            id,
            userId,
            isAdmin,
            cancellationToken);

        if (result == null) return NotFound();

        var model = new CarDetailsViewModel
        {
            Car = result.Car,
            Comments = result.Comments.Select(x => new CarComment
            {
                Id = x.Id,
                Content = $"{x.UserName}|{x.Content}",
                CreatedOn = x.CreatedOn

            }).ToList(),
            AverageRating = result.AverageRating,
            RatingCount = result.RatingCount,
            CurrentUserRating = result.CurrentUserRating,
            IsFavorite = result.IsFavorite
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ToggleFavorite(int carId, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) 
            return Challenge();
        await _carInteractionService.ToggledFavoriteAsync(carId, userId, cancellationToken);
        return RedirectToAction(nameof(Details), new {id = carId});
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Rate(int carId, int score, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            await _carInteractionService.RateAsync(carId, userId, score, cancellationToken);
        }
        catch(ArgumentException)
        {
            return RedirectToAction(nameof(Details), new {id =carId});
        }

        return RedirectToAction(nameof(Details), new { id = carId });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddComment(int carId, string content, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if(string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            await _carInteractionService.AddCommentAsync(carId, userId, content, cancellationToken);
        }
        catch (ArgumentException)
        {
            return RedirectToAction(nameof(Details), new {id = carId});
        }

        return RedirectToAction(nameof(Details), new {id = carId});
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var car = await _carsServices.GetByIdAsync(id, cancellationToken);
        if (car == null) return NotFound();
        return View(car);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(Car updateCar, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(updateCar);

        updateCar.ModifiedOn = DateTime.Now;
        await _carsServices.UpdateAsync(updateCar, cancellationToken);

        return RedirectToAction(nameof(List));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var car = await _carsServices.GetByIdAsync(id, cancellationToken);
        if (car == null) return NotFound();
        return View(car);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirm(int id, CancellationToken cancellationToken = default)
    {
        var success = await _carsServices.DeleteAsync(id, cancellationToken);
        if (!success) return NotFound();
        return RedirectToAction(nameof(List));
    }
}
