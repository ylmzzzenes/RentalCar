using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalCar.Application.Abstractions.Services.Rentals;
using RentalCar.Web.ViewModels.Rentals;
using RentalPageDto = RentalCar.Application.Dtos.Rentals.RentalPageDto;

namespace RentalCar.Controllers;

public class RentalController : Controller
{
    private readonly IRentalAppService _rentalAppService;

    public RentalController(IRentalAppService rentalAppService)
    {
        _rentalAppService = rentalAppService;
    }

    [HttpGet]
    public async Task<IActionResult> RentCar(int id, CancellationToken cancellationToken = default)
    {
        var page = await _rentalAppService.GetRentCarPageAsync(id, cancellationToken);
        if (page == null)
            return NotFound();

        var model = MapToPageViewModel(page);
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RentCar(
        RentCarPageViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var page = await _rentalAppService.GetRentCarPageAsync(model.Form.CarId, cancellationToken);
            if (page == null)
                return NotFound();

            model.Car = MapCarSummary(page);
            return View(model);
        }

        var result = await _rentalAppService.CreateRentalAsync(
            model.Form.CarId,
            model.Form.RentalType,
            model.Form.Duration,
            cancellationToken);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Kiralama işlemi başarısız.");

            var page = await _rentalAppService.GetRentCarPageAsync(model.Form.CarId, cancellationToken);
            if (page == null)
                return NotFound();

            model.Car = MapCarSummary(page);
            return View(model);
        }

        return RedirectToAction(nameof(RentalResult), new { id = result.Rental!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> RentalResult(int id, CancellationToken cancellationToken = default)
    {
        var result = await _rentalAppService.GetRentalresultAsync(id, cancellationToken);
        if (result == null || result.Rental == null)
            return NotFound();

        return View(result.Rental);
    }

    private static RentCarPageViewModel MapToPageViewModel(RentalPageDto page)
    {
        return new RentCarPageViewModel
        {
            Car = MapCarSummary(page),
            Form = new RentalFormViewModel
            {
                CarId = page.CarId,
                RentalType = page.RentalType,
                Duration = page.Duration
            }
        };
    }

    private static CarRentalSummaryViewModel MapCarSummary(RentalPageDto page)
    {
        return new CarRentalSummaryViewModel
        {
            CarId = page.CarId,
            Brand = page.Brand,
            Model = page.Model,
            Plate = page.Plate,
            ModelYear = page.ModelYear,
            FuelType = page.FuelType,
            Transmission = page.Transmission,
            BodyType = page.BodyType,
            Color = page.Color,
            Security = page.Security,
            InternalEquipment = page.InternalEquipment,
            ExternalEquipment = page.ExternalEquipment,
            ImageUrls = page.ImageUrls ?? new List<string>(),
            DailyPrice = page.DailyPrice,
            WeeklyPrice = page.WeeklyPrice,
            MonthlyPrice = page.MonthlyPrice
        };
    }
}