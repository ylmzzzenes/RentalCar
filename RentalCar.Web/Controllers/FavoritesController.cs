using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Infrastructure.Persistence.Context;
using System.Security.Claims;

namespace RentalCar.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly RentalCarContext _context;
        private readonly ICarInteractionService _carInteractionService;

        public FavoritesController(RentalCarContext context, ICarInteractionService carInteractionService)
        {
            _context = context;
            _carInteractionService = carInteractionService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var favorites = await _context.Favorites
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => x.Car)
                .ToListAsync(cancellationToken);

            var favoriteIds = favorites.Select(x => x.Id).ToHashSet();
            var ratings = await _context.CarRatings
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.ModifiedOn)
                .ToListAsync(cancellationToken);
            var ratedCars = await _context.Cars
                .Where(x => ratings.Select(r => r.CarId).Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            ViewBag.Ratings = ratings;
            ViewBag.RatedCars = ratedCars;

            if (favorites.Count > 0)
            {
                var preferredBrands = favorites
                    .Select(x => x.CatalogBrand ?? x.Brand)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
                var preferredFuel = favorites.Select(x => x.FuelType).Distinct();
                var preferredGear = favorites.Select(x => x.Transmission).Distinct();

                var candidates = await _context.Cars
                    .AsNoTracking()
                    .Where(x => x.IsApproved && !favoriteIds.Contains(x.Id))
                    .Where(x =>
                        preferredBrands.Contains(x.CatalogBrand ?? x.Brand ?? string.Empty) ||
                        preferredFuel.Contains(x.FuelType) ||
                        preferredGear.Contains(x.Transmission))
                    .Take(60)
                    .ToListAsync(cancellationToken);

                var criteriaSummary = $"Kullanici favorilerine benzer araclar: marka={string.Join(",", preferredBrands)}, yakit={string.Join(",", preferredFuel)}, vites={string.Join(",", preferredGear)}";
                var suggested = await _carInteractionService.RecommendByCriteriaAsync(candidates, criteriaSummary, 6, cancellationToken);
                ViewBag.RecommendedCars = suggested;
            }
            else
            {
                ViewBag.RecommendedCars = new List<RentalCar.Domain.Entities.Car>();
            }

            return View(favorites);
        }
    }
}
