using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalCar.Infrastructure.Persistence.Context;
using System.Security.Claims;

namespace RentalCar.Controllers
{
    [Authorize]
    public class RatingsController : Controller
    {
        private readonly RentalCarContext _context;

        public RatingsController(RentalCarContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var ratings = await _context.CarRatings
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.ModifiedOn)
                .ToListAsync(cancellationToken);

            var carIds = ratings.Select(x => x.CarId).Distinct().ToList();
            var cars = await _context.Cars
                .Where(x => carIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            ViewBag.Cars = cars;
            return View(ratings);
        }
    }
}
