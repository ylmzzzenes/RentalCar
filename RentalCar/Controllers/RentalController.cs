using Microsoft.AspNetCore.Mvc;
using RentalCar.Data.Enums;
using RentalCar.Data.Models;
using RentalCar.Data.Services;

namespace RentalCar.Controllers
{
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

            var model = new Rental
            {
                CarId = car.Id,
                Car = car,
                RentalType = RentalType.Daily,
                Duration = 1
            };

            return View(model);
        }

      
        [HttpPost]
        public async Task<IActionResult> RentCar(
            int id,
            RentalType rentalType,
            decimal duration,
            CancellationToken cancellationToken = default)
        {
            var car = await _rentalServices.GetCarByIdAsync(id, cancellationToken);
            if (car == null) return NotFound();

         
            if (duration <= 0)
            {
                ModelState.AddModelError(nameof(duration), "Süre 0 veya negatif olamaz.");
                var backModel = new Rental { CarId = car.Id, Car = car, RentalType = rentalType, Duration = duration };
                return View(backModel);
            }

            var totalPrice = CalculateTotalPrice(car, rentalType, duration);

            var rental = new Rental
            {
                CarId = car.Id,
                Car = car,
                RentalType = rentalType,
                Duration = (rentalType == RentalType.LongTerm) ? 12 : duration,
                TotalPrice = totalPrice
            };

            await _rentalServices.CreateAsync(rental, cancellationToken);

            // Sonucu göster
            return View("RentalResult", rental);
        }

    
        [HttpGet]
        public async Task<IActionResult> RentalResult(int carId, CancellationToken cancellationToken = default)
        {
            var car = await _rentalServices.GetCarByIdAsync(carId, cancellationToken);
            if (car == null) return NotFound();

            
            var model = new Rental
            {
                CarId = car.Id,
                Car = car,
                RentalType = RentalType.Daily,
                Duration = 1,
                TotalPrice = 0
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
}
