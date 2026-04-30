using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;
using RentalCar.ViewModels;
using System.Security.Claims;
using RentalCar.Infrastructure.Services.Cars;
using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Application.Contracts.Cars;
using RentalCar.Web.Models.Requests;
using Microsoft.AspNetCore.Hosting;

namespace RentalCar.Controllers;

public class CarController : Controller
{
    private readonly CarServices _carsServices;
    public readonly ICarInteractionService _carInteractionService;
    private readonly ICarAppService _carAppService;
    private readonly ICarListingReliabilityService _listingReliabilityService;
    private readonly ICarListingInsightService _listingInsightService;
    private readonly IWebHostEnvironment _environment;

    public CarController(
        CarServices carServices,
        ICarInteractionService carInteractionService,
        ICarAppService carAppService,
        ICarListingReliabilityService listingReliabilityService,
        ICarListingInsightService listingInsightService,
        IWebHostEnvironment environment)
    {
        _carsServices = carServices;
        _carInteractionService = carInteractionService;
        _carAppService = carAppService;
        _listingReliabilityService = listingReliabilityService;
        _listingInsightService = listingInsightService;
        _environment = environment;
    }

    [Authorize]
    public IActionResult Index() => View(CreateCarFormDefaults.Create());

    [HttpGet]
    public IActionResult Get(int id)
    {
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromForm] CreateCar request)
    {
        if (!ModelState.IsValid)
            return View("Index", request);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var fileNames = await SaveCarImagesAsync(request.PhotoUploads, HttpContext.RequestAborted);
        var prepareResult = await _carAppService.PrepareCarForCreateAsync(new CreateCarCommand
        {
            CatalogBrand = request.CatalogBrand,
            Series = request.Series,
            Model = request.Model,
            ModelYear = request.ModelYear,
            OdometerKm = request.OdometerKm,
            ListedPrice = request.ListedPrice ?? 0,
            FuelType = request.FuelType,
            Transmission = request.Transmission,
            EngineDisplacementLiters = request.EngineDisplacementLiters,
            EnginePowerHp = request.EnginePowerHp,
            Drivetrain = request.Drivetrain,
            FuelTankLiters = request.FuelTankLiters,
            ListingBodyType = request.ListingBodyType,
            Color = request.Color,
            VehicleCondition = request.VehicleCondition,
            BodyWorkNotes = request.BodyWorkNotes,
            TradeInAccepted = request.TradeInAccepted,
            SellerType = request.SellerType,
            ImageUrls = fileNames
        }, HttpContext.RequestAborted);

        if (!prepareResult.Success)
        {
            var msg = prepareResult.Message ?? "Araç oluşturulamadı.";
            var chunks = msg.Split(new[] { ". " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var chunk in chunks)
            {
                var t = chunk.Trim();
                if (t.Length == 0) continue;
                ModelState.AddModelError(string.Empty, t.EndsWith('.') ? t : t + ".");
            }

            if (ModelState.ErrorCount == 0)
                ModelState.AddModelError(string.Empty, msg);

            return View("Index", request);
        }

        var newCar = prepareResult.Data;
        newCar.PostedByUserId = userId;
        await _carsServices.CreateAsync(newCar);
        var reliability = await _listingReliabilityService.CalculateAsync(newCar, HttpContext.RequestAborted);
        TempData["CreateSuccess"] = "Arac ilani olusturuldu.";
        TempData["CreateAiSummary"] =
            $"AI Fiyat: {newCar.PredictedPriceMid:N0} TL | Güven skoru: {reliability.Score}/100 — {reliability.TrustLevelTr}";
        return RedirectToAction(nameof(Details), new { id = newCar.Id });
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(
        string? brand,
        string? model,
        string? renk,
        string? city,
        FuelType? yakitTuru,
        Gear? vites,
        BodyType? bodyType,
        int? yil,
        int? minYil,
        int? maxYil,
        int? minKm,
        int? maxKm,
        decimal? minFiyat,
        decimal? maxFiyat,
        string? searchString,
        string? postedBy,
        CancellationToken cancellationToken = default)
    {
        var all = await _carsServices.GetAllAsync(cancellationToken);
        if (all == null)
            return Problem("Cars is null");

        var query = all.AsQueryable();
        if (!User.IsInRole("Admin"))
            query = query.Where(c => c.IsApproved);

        if (!string.IsNullOrWhiteSpace(postedBy))
            query = query.Where(c => c.PostedByUserId == postedBy);

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

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(c => c.City != null && c.City.Contains(city, StringComparison.OrdinalIgnoreCase));

        if (yakitTuru != null)
            query = query.Where(c => c.FuelType == yakitTuru);

        if (vites != null)
            query = query.Where(c => c.Transmission == vites);

        if (bodyType != null)
            query = query.Where(c => c.BodyType == bodyType);

        if (yil != null)
            query = query.Where(c => c.ModelYear == yil);

        if (minYil.HasValue)
            query = query.Where(c => c.ModelYear.HasValue && c.ModelYear.Value >= minYil.Value);

        if (maxYil.HasValue)
            query = query.Where(c => c.ModelYear.HasValue && c.ModelYear.Value <= maxYil.Value);

        if (minKm.HasValue)
            query = query.Where(c => c.OdometerKm.HasValue && c.OdometerKm.Value >= minKm.Value);

        if (maxKm.HasValue)
            query = query.Where(c => c.OdometerKm.HasValue && c.OdometerKm.Value <= maxKm.Value);

        if (minFiyat.HasValue)
            query = query.Where(c => c.ListedPrice.HasValue && c.ListedPrice.Value >= minFiyat.Value);

        if (maxFiyat.HasValue)
            query = query.Where(c => c.ListedPrice.HasValue && c.ListedPrice.Value <= maxFiyat.Value);

        var list = query
            .OrderByDescending(c => c.CreatedOn)
            .ToList();
        var reliability = await _listingReliabilityService.CalculateBatchAsync(list, cancellationToken);

        var criteriaSummary =
            $"marka:{brand ?? "-"}, model:{model ?? "-"}, renk:{renk ?? "-"}, sehir:{city ?? "-"}, yakit:{yakitTuru?.ToString() ?? "-"}, vites:{vites?.ToString() ?? "-"}, kasa:{bodyType?.ToString() ?? "-"}, yil:{yil?.ToString() ?? "-"}, minYil:{minYil?.ToString() ?? "-"}, maxYil:{maxYil?.ToString() ?? "-"}, minKm:{minKm?.ToString() ?? "-"}, maxKm:{maxKm?.ToString() ?? "-"}, minFiyat:{minFiyat?.ToString() ?? "-"}, maxFiyat:{maxFiyat?.ToString() ?? "-"}, arama:{searchString ?? "-"}";
        var hasFilterCriteria =
            !string.IsNullOrWhiteSpace(searchString) ||
            !string.IsNullOrWhiteSpace(brand) ||
            !string.IsNullOrWhiteSpace(model) ||
            !string.IsNullOrWhiteSpace(renk) ||
            !string.IsNullOrWhiteSpace(city) ||
            yakitTuru.HasValue || vites.HasValue || bodyType.HasValue ||
            yil.HasValue || minYil.HasValue || maxYil.HasValue ||
            minKm.HasValue || maxKm.HasValue ||
            minFiyat.HasValue || maxFiyat.HasValue;

        var recommendationCandidates = all
            .Where(c => User.IsInRole("Admin") || c.IsApproved)
            .Where(c => list.All(x => x.Id != c.Id))
            .ToList();
        ViewBag.RecommendedCars = hasFilterCriteria
            ? await _carInteractionService.RecommendByCriteriaAsync(recommendationCandidates, criteriaSummary, maxCount: 6, cancellationToken)
            : new List<Car>();

        ViewBag.Brand = brand;
        ViewBag.Model = model;
        ViewBag.Renk = renk;
        ViewBag.City = city;
        ViewBag.YakitTuru = yakitTuru?.ToString();
        ViewBag.Vites = vites?.ToString();
        ViewBag.BodyType = bodyType?.ToString();
        ViewBag.Yil = yil;
        ViewBag.MinYil = minYil;
        ViewBag.MaxYil = maxYil;
        ViewBag.MinKm = minKm;
        ViewBag.MaxKm = maxKm;
        ViewBag.MinFiyat = minFiyat;
        ViewBag.MaxFiyat = maxFiyat;
        ViewBag.SearchString = searchString;
        ViewBag.PostedBy = postedBy;
        ViewBag.ReliabilityScores = reliability;
        return View(list);
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
        var reliability = await _listingReliabilityService.CalculateAsync(result.Car, cancellationToken);
        var insights = await _listingInsightService.GetInsightsAsync(id, userId, cancellationToken);

        var model = new CarDetailsViewModel
        {
            Car = result.Car,
            Seller = result.Seller,
            SimilarCars = result.SimilarCars,
            ListingInsights = insights,
            RecommendedCars = result.RecommendedCars,
            Comments = result.Comments.Select(x => new CarComment
            {
                Id = x.Id,
                Content = $"{x.UserName}|{x.Content}",
                CreatedOn = x.CreatedOn

            }).ToList(),
            AverageRating = result.AverageRating,
            RatingCount = result.RatingCount,
            CurrentUserRating = result.CurrentUserRating,
            IsFavorite = result.IsFavorite,
            ReliabilityScore = reliability.Score,
            ReliabilityLabel = reliability.Label,
            ReliabilityTrustLevelTr = reliability.TrustLevelTr,
            ReliabilityExplanation = reliability.UserExplanation,
            ReliabilityFactors = isAdmin ? reliability.Factors : null
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
    public async Task<IActionResult> Edit(Car posted, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(posted);

        var existing = await _carsServices.GetByIdAsync(posted.Id, cancellationToken);
        if (existing == null)
            return NotFound();

        existing.CatalogBrand = posted.CatalogBrand;
        existing.Brand = posted.Brand;
        existing.Series = posted.Series;
        existing.Model = posted.Model;
        existing.CatalogModelName = posted.Model;
        existing.ModelYear = posted.ModelYear;
        existing.OdometerKm = posted.OdometerKm;
        existing.ListedPrice = posted.ListedPrice;
        existing.Transmission = posted.Transmission;
        existing.FuelType = posted.FuelType;
        existing.EngineDisplacementLiters = posted.EngineDisplacementLiters;
        existing.EnginePowerHp = posted.EnginePowerHp;
        existing.Drivetrain = posted.Drivetrain;
        existing.FuelTankLiters = posted.FuelTankLiters;
        existing.BodyType = posted.BodyType;
        existing.Color = posted.Color;
        existing.VehicleCondition = posted.VehicleCondition;
        existing.BodyWorkNotes = posted.BodyWorkNotes;
        existing.TradeInAccepted = posted.TradeInAccepted;
        existing.SellerType = posted.SellerType;
        if (!string.IsNullOrWhiteSpace(posted.PostedByUserId))
            existing.PostedByUserId = posted.PostedByUserId;
        existing.Plate = posted.Plate;
        existing.ImageUrls = posted.ImageUrls;
        existing.ModifiedOn = DateTime.Now;

        await _carsServices.UpdateAsync(existing, cancellationToken);

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

    private async Task<List<string>> SaveCarImagesAsync(IEnumerable<IFormFile>? files, CancellationToken cancellationToken)
    {
        var stored = new List<string>();
        if (files is null)
            return stored;

        var targetFolder = Path.Combine(_environment.WebRootPath, "Images", "Upload");
        Directory.CreateDirectory(targetFolder);

        foreach (var file in files.Where(f => f is not null && f.Length > 0))
        {
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                continue;

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(targetFolder, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream, cancellationToken);
            stored.Add(fileName);
        }

        return stored;
    }
}
