using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalCar.Data.Enums;
using RentalCar.Data.Services;

namespace RentalCar.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly CarServices _carsServices;

        public HomeController(CarServices carServices)
        {
            _carsServices = carServices;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var vitrinArabalar = await _carsServices.GetRandomAsync(5);
            return View(vitrinArabalar);
        }

        [HttpGet]
        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Error()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> List(
            string? brand,
            string? model,
            string? renk,
            FuelType? yakitTuru,
            Gear? vites,
            BodyType? bodyType,
            DateTime? yil,
            string? imageUrl,
            string? searchString,
            CancellationToken cancellationToken = default)
        {
            var all = await _carsServices.GetAllAsync();
            if (all == null)
                return Problem("Cars is null");

            var query = all.AsQueryable();

            // Genel arama (opsiyonel)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                query = query.Where(c =>
                    (c.Brand != null && c.Brand.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.model != null && c.model.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.renk != null && c.renk.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.marka != null && c.marka.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.model_adi != null && c.model_adi.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.paket != null && c.paket.Contains(s, StringComparison.OrdinalIgnoreCase))
                );
            }

            // Filtreler
            if (!string.IsNullOrWhiteSpace(brand))
                query = query.Where(c => c.Brand != null && c.Brand.Contains(brand, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(model))
                query = query.Where(c => c.model != null && c.model.Contains(model, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(renk))
                query = query.Where(c => c.renk != null && c.renk.Contains(renk, StringComparison.OrdinalIgnoreCase));

            if (yakitTuru.HasValue)
                query = query.Where(c => c.yakitTuru == yakitTuru.Value);

            if (vites.HasValue)
                query = query.Where(c => c.vites == vites.Value);

            if (bodyType.HasValue)
                query = query.Where(c => c.BodyType == bodyType.Value);

            if (yil.HasValue)
                query = query.Where(c => c.yil == yil.Value);

            if (!string.IsNullOrWhiteSpace(imageUrl))
                query = query.Where(c => c.ImageUrls != null && c.ImageUrls.Contains(imageUrl, StringComparison.OrdinalIgnoreCase));

            return View(query.ToList());
        }
    }
}
