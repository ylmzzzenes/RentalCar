using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalCar.Infrastructure.Persistence.Context;
using System.Security.Claims;

namespace RentalCar.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly RentalCarContext _context;

        public FavoritesController(RentalCarContext context)
        {
            _context = context;
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

            return View(favorites);
        }
    }
}
