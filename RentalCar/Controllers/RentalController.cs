using Microsoft.AspNetCore.Mvc;
using RentalCar.Data.Dbcontexts;
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
        public async Task<IActionResult> RentCar(int id)
        {
            var car = await _rentalServices.GetCarByIdAsync(id);
            if (car == null) return NotFound();

            var model = new Rental
            {
                CarId = car.Id,
                Car = car
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> RentCar(int id, RentalType RentalType, decimal duration, CancellationToken cancellationToken = default)
        {
            var car = await _rentalServices.GetCarByIdAsync(id, cancellationToken);
            if (car == null)
                return NotFound();

            decimal totalPrice = RentalType switch
            {
                RentalType.Daily => car.DailyPrice * duration,
                RentalType.Weekly => car.WeeklyPrice * duration,
                RentalType.Monthly => car.MonthlyPrice * duration,
                RentalType.LongTerm => car.MonthlyPrice * 12,
                _ => 0
            };

            var rental = new Rental
            {
                CarId = car.Id,
                Car = car,
                RentalType = RentalType,
                Duration = duration,
                TotalPrice = totalPrice
            };
            await _rentalServices.CreateAsync(rental);


            return View("RentalResult", rental);
        }



        [HttpGet]
        public async Task<IActionResult> RentalResult(int id)
        {
            var car = await _rentalServices.GetCarByIdAsync(id);
            if (car == null) return NotFound();

            return View(car); 
        }



        [HttpPost]
        public async Task<IActionResult> RentalResult(Rental rental)
        {
            var car = await _rentalServices.GetCarByIdAsync(rental.CarId);
            if (car == null) return NotFound();

            rental.Car = car;

            decimal totalPrice = rental.RentalType switch
            {
                RentalType.Daily => car.DailyPrice * rental.Duration,
                RentalType.Weekly => car.WeeklyPrice * rental.Duration,
                RentalType.Monthly => car.MonthlyPrice * rental.Duration,
                RentalType.LongTerm => car.MonthlyPrice * 12,
                _ => 0
            };


            ViewBag.TotalPrice = totalPrice;
            return View("RentalSummary", rental);

        }
    }
}