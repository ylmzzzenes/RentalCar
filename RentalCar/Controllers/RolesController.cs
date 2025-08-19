using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentalCar.Data.Models;

namespace RentalCar.Controllers
{
    public class RolesController : Controller
    {
        private readonly RoleManager<AppRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;

        public RolesController(RoleManager<AppRole> roleManager, UserManager<AppUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View(_roleManager.Roles);
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(AppRole model)
        {
            if (ModelState.IsValid)
            {
                var result =await _roleManager.CreateAsync(model);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                }
                foreach(var err in result.Errors)
                {
                    ModelState.AddModelError("", err.Description);
                }
            }
            return View(model);
        }
    }
}
