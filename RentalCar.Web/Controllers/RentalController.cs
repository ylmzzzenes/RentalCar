using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;
using RentalCar.Domain.Rules;
using RentalCar.Infrastructure.Services;

namespace RentalCar.Controllers;

public class RentalController : Controller
{
    private readonly RentalServices _rentalServices;

    public RentalController(RentalServices rentalServices)
    {
        _rentalServices = rentalServices;
    }

    [HttpGet]
    public async Task<IActionResult> RentCar(int id, CancellationToken cancellationToken = default)
    {
        var car = await _rentalServices.GetCarByIdAsync(id, cancellationToken);
        if (car == null) return NotFound();

        var startUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var model = new Rental
        {
            CarId = car.Id,
            Car = car,
            RentalType = RentalType.Daily,
            Duration = 1,
            StartDate = startUtc
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> RentCar(
        int id,
        RentalType rentalType,
        decimal duration,
        CancellationToken cancellationToken = default)
    {
        var car = await _rentalServices.GetCarByIdAsync(id, cancellationToken);
        if (car == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Challenge();

        if (duration <= 0)
        {
            ModelState.AddModelError(nameof(duration), "Süre 0 veya negatif olamaz.");
            var backModel = new Rental
            {
                CarId = car.Id,
                Car = car,
                RentalType = rentalType,
                Duration = duration,
                StartDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc)
            };
            return View(backModel);
        }

        var effectiveDuration = rentalType == RentalType.LongTerm ? 12 : duration;
        var startUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        try
        {
            RentalDateRules.ValidateSchedule(startUtc, rentalType, effectiveDuration);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var backModel = new Rental
            {
                CarId = car.Id,
                Car = car,
                RentalType = rentalType,
                Duration = duration,
                StartDate = startUtc
            };
            return View(backModel);
        }

        var totalPrice = CalculateTotalPrice(car, rentalType, duration);
        var now = DateTime.UtcNow;

        var rental = new Rental
        {
            CarId = car.Id,
            UserId = userId,
            RentalType = rentalType,
            Duration = effectiveDuration,
            TotalPrice = totalPrice,
            StartDate = startUtc,
            Status = RentalStatus.Confirmed,
            CreatedOn = now,
            ModifiedOn = now
        };

        await _rentalServices.CreateAsync(rental, cancellationToken);

        var created = await _rentalServices.GetRentalByIdWithCarAsync(rental.Id, cancellationToken);
        if (created == null)
            return NotFound();

        return View("RentalResult", created);
    }

    [HttpGet]
    public async Task<IActionResult> RentalResult(int carId, CancellationToken cancellationToken = default)
    {
        var car = await _rentalServices.GetCarByIdAsync(carId, cancellationToken);
        if (car == null) return NotFound();

        var startUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var model = new Rental
        {
            CarId = car.Id,
            Car = car,
            RentalType = RentalType.Daily,
            Duration = 1,
            TotalPrice = 0,
            StartDate = startUtc
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> RentalResult(Rental rental, CancellationToken cancellationToken = default)
    {
        var car = await _rentalServices.GetCarByIdAsync(rental.CarId, cancellationToken);
        if (car == null) return NotFound();

        if (rental.Duration <= 0)
        {
            ModelState.AddModelError(nameof(rental.Duration), "Süre 0 veya negatif olamaz.");
            rental.Car = car;
            return View(rental);
        }

        rental.Car = car;

        if (rental.StartDate == default || rental.StartDate.Kind != DateTimeKind.Utc)
            rental.StartDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        var totalPrice = CalculateTotalPrice(car, rental.RentalType, rental.Duration);

        ViewBag.TotalPrice = totalPrice;

        rental.TotalPrice = totalPrice;

        return View("RentalSummary", rental);
    }

    private static decimal CalculateTotalPrice(Car car, RentalType rentalType, decimal duration)
    {
        return rentalType switch
        {
            RentalType.Daily => car.DailyPrice * duration,
            RentalType.Weekly => car.WeeklyPrice * duration,
            RentalType.Monthly => car.MonthlyPrice * duration,
            RentalType.LongTerm => car.MonthlyPrice * 12,
            _ => 0m
        };
    }
}
