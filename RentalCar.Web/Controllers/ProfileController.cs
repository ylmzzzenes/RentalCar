using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentalCar.Data.Models;
using RentalCar.ViewModels;
using System.Security.Claims;

namespace RentalCar.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public ProfileController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var model = new ProfileViewModel
            {
                FullName = user.FullName ?? string.Empty,
                UserName = user.UserName,
                Email = user.Email ?? string.Empty,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Update(ProfileViewModel model, IFormFile? profileImage, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid) return View("Index", model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.UserName ?? user.UserName;
            user.Bio = model.Bio;

            if (profileImage != null && profileImage.Length > 0)
            {
                var extension = Path.GetExtension(profileImage.FileName);
                var fileName = $"profile_{user.Id}_{Guid.NewGuid():N}{extension}";
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Profile");
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, fileName);
                await using var stream = new FileStream(path, FileMode.Create);
                await profileImage.CopyToAsync(stream, cancellationToken);
                user.ProfileImageUrl = fileName;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View("Index", model);
            }

            TempData["Message"] = "Profil bilgileri guncellendi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
