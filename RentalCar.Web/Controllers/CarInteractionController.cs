using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalCar.Application.Abstractions.Services.Cars;
using System.Security.Claims;

namespace RentalCar.Web.Controllers
{
    public class CarInteractionController : Controller
    {

        private readonly ICarInteractionService _carInteractionService;

        public CarInteractionController(ICarInteractionService carInteractionService)
        {
            _carInteractionService = carInteractionService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(int carId, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();
            await _carInteractionService.ToggledFavoriteAsync(carId, userId, cancellationToken);
            return RedirectToAction("Details", "CarBrowse", new { id = carId });

        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Rate(int carId, int score, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            try
            {
                await _carInteractionService.RateAsync(carId, userId, score, cancellationToken);
            }
            catch (ArgumentException)
            {
                return RedirectToAction("Details","CarBrowse", new { id = carId });
            }

            return RedirectToAction("Details", "CarBrowse", new { id = carId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(int carId, string content, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            try
            {
                await _carInteractionService.AddCommentAsync(carId, userId, content, cancellationToken);
            }
            catch (ArgumentException)
            {
                return RedirectToAction("Details", "CarBrowse", new { id = carId });
            }

            return RedirectToAction("Details", "CarBrowse", new { id = carId });
        }
    }
}
