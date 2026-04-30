using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using RentalCar.Domain.Enums;
using RentalCar.Infrastructure.Services.Cars;
using RentalCar.Models;
using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Application.Contracts.Cars;

namespace RentalCar.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly CarServices _carsServices;
    private readonly ICarAppService _carAppService;
    private readonly ICarListingReliabilityService _listingReliabilityService;

    public HomeController(
        CarServices carServices,
        ICarAppService carAppService,
        ICarListingReliabilityService listingReliabilityService)
    {
        _carsServices = carServices;
        _carAppService = carAppService;
        _listingReliabilityService = listingReliabilityService;
    }
    [AllowAnonymous]
    [HttpGet]
    public IActionResult ValidationAspectSuccessTest()
    {
        try
        {
            var request = new CreateCarRequest
            {
                Brand = "Toyota",
                Model = "Corolla",
                PricePerDay = 2500,
                Year = 2022,
                FuelType = "Benzin",
                Transmission = "Otomatik"
            };

            var result = _carAppService.Create(request);

            return Content($"Success: {result.Success}, Message: {result.Message}");
        }
        catch (Exception ex)
        {
            return Content($"HATA: {ex.GetType().Name} - {ex.Message}");
        }
    }
    [AllowAnonymous]
    [HttpGet]
    public IActionResult ValidationAspectBusinessRuleTest()
    {
        var request = new CreateCarRequest
        {
            Brand = "Admin",
            Model = "Corolla",
            PricePerDay = 2500,
            Year = 2022,
            FuelType = "Benzin",
            Transmission = "Otomatik"
        };

        var result = _carAppService.Create(request);

        return Content($"Success: {result.Success}, Message: {result.Message}");
    }
    [AllowAnonymous]
    [HttpGet]
    public IActionResult ValidationAspectValidationTest()
    {
        var request = new CreateCarRequest
        {
            Brand = "",
            Model = "",
            PricePerDay = 0,
            Year = 1900,
            FuelType = "",
            Transmission = ""
        };

        var result = _carAppService.Create(request);

        return Content($"Success: {result.Success}, Message: {result.Message}");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var vitrinArabalar = await _carsServices.GetRandomAsync(5, cancellationToken);
        return View(vitrinArabalar);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult About()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("Home/Error")]
    public IActionResult Error()
    {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();

        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Message = HttpContext.Items["ExceptionMessage"] as string
                      ?? exceptionFeature?.Error?.Message
                        ?? "Beklenmeyen bir hata olustu."
        };
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(
        string? brand,
        string? model,
        string? renk,
        FuelType? yakitTuru,
        Gear? vites,
        BodyType? bodyType,
        int? yil,
        string? imageUrl,
        string? searchString,
        CancellationToken cancellationToken = default)
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
                (c.TrimPackage != null && c.TrimPackage.Contains(s, StringComparison.OrdinalIgnoreCase))
            );
        }

        if (!string.IsNullOrWhiteSpace(brand))
            query = query.Where(c => c.Brand != null && c.Brand.Contains(brand, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(model))
            query = query.Where(c => c.Model != null && c.Model.Contains(model, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(renk))
            query = query.Where(c => c.Color != null && c.Color.Contains(renk, StringComparison.OrdinalIgnoreCase));

        if (yakitTuru.HasValue)
            query = query.Where(c => c.FuelType == yakitTuru.Value);

        if (vites.HasValue)
            query = query.Where(c => c.Transmission == vites.Value);

        if (bodyType.HasValue)
            query = query.Where(c => c.BodyType == bodyType.Value);

        if (yil.HasValue)
            query = query.Where(c => c.ModelYear == yil.Value);

        if (!string.IsNullOrWhiteSpace(imageUrl))
            query = query.Where(c => c.ImageUrls.Any(img => img.Contains(imageUrl, StringComparison.OrdinalIgnoreCase)));

        var list = query.ToList();
        ViewBag.ReliabilityScores = await _listingReliabilityService.CalculateBatchAsync(list, cancellationToken);
        return View("~/Views/Car/List.cshtml", list);
    }
}
