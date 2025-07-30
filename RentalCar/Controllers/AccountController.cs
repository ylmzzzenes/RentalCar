using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentalCar.Data.Models;
using RentalCar.Data.Models.Requests;

namespace RentalCar.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    BirthDate = model.BirthDate,
                    Address = model.Address,
                    City = model.City,
                    Country = model.Country,
                    PostalCode = model.PostalCode,
                    PhoneNumber2 = model.PhoneNumber2,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

            }
            return View(model);



        }

        public IActionResult Login()
            {
                return View();
            }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> Login(LoginViewModel Model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(Model.Email, Model.Password, Model.RememberMe, false);
                if (result.Succeeded)
                
                    return RedirectToAction("Index", "Home");
                ModelState.AddModelError(string.Empty,"Geçersiz giriş denemesi.");
            }
            return View(Model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

    }
}

