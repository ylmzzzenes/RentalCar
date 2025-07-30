using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalCar.Data.Enums;
using RentalCar.Data.Models;
using RentalCar.Data.Services;

namespace RentalCar.Controllers
{
   
    public class HomeController : Controller
    {
        private readonly CarServices _carsServices;

        public HomeController(CarServices carServices)
        {
            _carsServices = carServices;
        }
        public async Task<IActionResult> Index()
        {
            var vitrinArabalar= await _carsServices.GetRandomAsync(5);
            return View(vitrinArabalar);
        }

        public IActionResult About()
        {
            return View();  
        }

        public IActionResult Error()
        {
            return View();  
        }
        public async Task<IActionResult> List(string? brand, string? model, string? colour, FuelType? fuelType, Gear? gear, BodyType? bodyType, DateOnly? year, string ImageUrl)
        {
            var query = (await _carsServices.GetAllAsync()).AsQueryable();

            if (!string.IsNullOrWhiteSpace(brand))
                query = query.Where(c => c.Brand.Contains(brand, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(model))
                query = query.Where(c => c.Model.Contains(model, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(colour))
                query = query.Where(c => c.Colour.Contains(colour, StringComparison.OrdinalIgnoreCase));

            if (fuelType != null)
                query = query.Where(c => c.FuelType == fuelType);

            if (gear != null)
                query = query.Where(c => c.Gear == gear);

            if (bodyType != null)
                query = query.Where(c => c.BodyType == bodyType);

            if (year != null)
                query = query.Where(c => c.Year == year);
            if (ImageUrl != null)
                query = query.Where(c => c.ImageUrls == ImageUrl);

            return View(query.ToList());
        }
        
    }
}
