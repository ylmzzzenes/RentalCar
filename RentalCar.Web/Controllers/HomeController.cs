using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalCar.Domain.Enums;
using RentalCar.Infrastructure.Services;

namespace RentalCar.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly CarServices _carsServices;

    public HomeController(CarServices carServices)
    {
        _carsServices = carServices;
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
    public IActionResult Error()
    {
        return View();
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

        return View("~/Views/Car/List.cshtml", query.ToList());
    }
}
