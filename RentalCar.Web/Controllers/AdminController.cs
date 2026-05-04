using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Domain.Entities;
using RentalCar.Infrastructure.Persistence.Context;
using RentalCar.ViewModels.Admin;

namespace RentalCar.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly RentalCarContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly ICarCatalogImageSyncService _carCatalogImageSyncService;

        public AdminController(
            RentalCarContext context,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            ICarCatalogImageSyncService carCatalogImageSyncService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _carCatalogImageSyncService = carCatalogImageSyncService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalCars = await _context.Cars.CountAsync(),
                PendingCars = await _context.Cars.CountAsync(c => !c.IsApproved),
                TotalComments = await _context.CarComments.CountAsync(),
                TotalRatings = await _context.CarRatings.CountAsync(),
                TotalFavorites = await _context.Favorites.CountAsync()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCarBrandModelsJson(CancellationToken cancellationToken)
        {
            var rows = await _carCatalogImageSyncService.GetBrandModelExportRowsAsync(cancellationToken);
            var json = JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
            return File(Encoding.UTF8.GetBytes(json), "application/json", "car-brand-models.json");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncCatalogCarImages(bool replaceRemoteUrls, CancellationToken cancellationToken)
        {
            var result = await _carCatalogImageSyncService.DownloadAndAttachToCarsAsync(replaceRemoteUrls, cancellationToken);
            TempData["AdminSuccess"] =
                $"Katalog gorselleri: islenen {result.CarsProcessed}, guncellenen {result.CarsUpdated}, atlanan {result.CarsSkipped}, indirilen dosya {result.FilesDownloaded}.";
            if (result.Errors.Count > 0)
                TempData["AdminError"] = string.Join(" | ", result.Errors.Take(8));
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var list = new List<AdminUserItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                list.Add(new AdminUserItemViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "User",
                    IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow
                });
            }

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction(nameof(Users));

            var allowedRoles = new[] { "Admin", "User" };
            if (!allowedRoles.Contains(role))
            {
                TempData["AdminError"] = "Gecersiz rol secimi.";
                return RedirectToAction(nameof(Users));
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new AppRole { Name = role });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            await _userManager.AddToRoleAsync(user, role);

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction(nameof(Users));

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["AdminError"] = "Kendi hesabinizi kilitleyemezsiniz.";
                return RedirectToAction(nameof(Users));
            }

            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            user.LockoutEnd = isLocked ? null : DateTimeOffset.UtcNow.AddYears(50);
            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction(nameof(Users));

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["AdminError"] = "Kendi hesabinizi silemezsiniz.";
                return RedirectToAction(nameof(Users));
            }

            await _userManager.DeleteAsync(user);
            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Cars()
        {
            var cars = await _context.Cars
                .OrderByDescending(c => c.CreatedOn)
                .Select(c => new AdminCarItemViewModel
                {
                    Id = c.Id,
                    Title = (c.CatalogBrand ?? c.Brand ?? "Arac") + " " + (c.CatalogModelName ?? c.Model ?? string.Empty),
                    Price = c.ListedPrice ?? c.DailyPrice,
                    Year = c.ModelYear,
                    City = c.City,
                    IsApproved = c.IsApproved
                })
                .ToListAsync();

            return View(cars);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleApprove(int id)
        {
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == id);
            if (car == null) return RedirectToAction(nameof(Cars));

            car.IsApproved = !car.IsApproved;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Cars));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCar(int id)
        {
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == id);
            if (car == null) return RedirectToAction(nameof(Cars));

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Cars));
        }

        public async Task<IActionResult> Comments()
        {
            var comments = await _context.CarComments
                .Include(c => c.Car)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedOn)
                .Select(c => new AdminCommentItemViewModel
                {
                    Id = c.Id,
                    CarId = c.CarId,
                    CarTitle = (c.Car.CatalogBrand ?? c.Car.Brand ?? "Arac") + " " + (c.Car.CatalogModelName ?? c.Car.Model ?? string.Empty),
                    UserName = c.User.UserName ?? "Kullanici",
                    Content = c.Content,
                    CreatedAt = c.CreatedOn
                })
                .ToListAsync();

            return View(comments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.CarComments.FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null) return RedirectToAction(nameof(Comments));

            _context.CarComments.Remove(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Comments));
        }

        public async Task<IActionResult> Ratings()
        {
            var ratings = await _context.CarRatings
                .Include(r => r.Car)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedOn)
                .Select(r => new AdminRatingItemViewModel
                {
                    Id = r.Id,
                    CarId = r.CarId,
                    CarTitle = (r.Car.CatalogBrand ?? r.Car.Brand ?? "Arac") + " " + (r.Car.CatalogModelName ?? r.Car.Model ?? string.Empty),
                    UserName = r.User.UserName ?? "Kullanici",
                    Score = r.Score,
                    CreatedAt = r.CreatedOn
                })
                .ToListAsync();

            return View(ratings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRating(int id)
        {
            var rating = await _context.CarRatings.FirstOrDefaultAsync(r => r.Id == id);
            if (rating == null) return RedirectToAction(nameof(Ratings));

            _context.CarRatings.Remove(rating);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Ratings));
        }

        public async Task<IActionResult> Favorites()
        {
            var favorites = await _context.Favorites
                .Include(f => f.Car)
                .Include(f => f.User)
                .OrderByDescending(f => f.CreatedOn)
                .Select(f => new AdminFavoriteItemViewModel
                {
                    Id = f.Id,
                    CarId = f.CarId,
                    CarTitle = (f.Car.CatalogBrand ?? f.Car.Brand ?? "Arac") + " " + (f.Car.CatalogModelName ?? f.Car.Model ?? string.Empty),
                    UserName = f.User.UserName ?? "Kullanici",
                    CreatedAt = f.CreatedOn
                })
                .ToListAsync();

            return View(favorites);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFavorite(int id)
        {
            var favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.Id == id);
            if (favorite == null) return RedirectToAction(nameof(Favorites));

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Favorites));
        }
    }
}
