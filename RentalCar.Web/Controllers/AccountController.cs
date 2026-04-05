using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RentalCar.Application.Abstractions.Services;
using RentalCar.Domain.Entities;
using RentalCar.ViewModels;
using System.Net.Mail;

namespace RentalCar.Controllers
{
    public class AccountController : Controller
    {
        private RoleManager<AppRole> _roleManager;
        private UserManager<AppUser> _userManager;
        private SignInManager<AppUser> _signInManager;
        private IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;


        public AccountController(
            RoleManager<AppRole> roleManager,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
        }
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]

        public async Task<IActionResult> Create(CreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new AppUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    FullName = model.FullName
                };

                IdentityResult result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationUrl = Url.Action("ConfirmEmail", "Account", new { user.Id, token }, Request.Scheme);

                    try
                    {
                        await _emailSender.SendEmailAsync(
                            user.Email!,
                            "Hesap Onayi",
                            $"Lutfen email hesabinizi onaylamak icin linke <a href='{confirmationUrl}'>tiklayiniz.</a>");

                        TempData["Message"] = "Onay e-postasi gonderildi. Lutfen gelen kutunuzu kontrol edin.";
                    }
                    catch (SmtpException ex)
                    {
                        _logger.LogError(ex, "Kayit sonrasi e-posta gonderilemedi. UserId: {UserId}", user.Id);
                        TempData["Message"] = "Hesap olusturuldu ancak e-posta gonderilemedi. SMTP ayarlarinizi kontrol edin.";
                    }

                    return RedirectToAction("Login", "Account");
                }

                foreach (IdentityError err in result.Errors)
                {
                    ModelState.AddModelError("", err.Description);
                }
            }
            return View(model);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    await _signInManager.SignOutAsync();
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, true);
                    if (result.Succeeded)
                    {
                        await _userManager.ResetAccessFailedCountAsync(user);
                        await _userManager.SetLockoutEndDateAsync(user, null);
                        return RedirectToAction("Index", "Home");
                    }
                    else if (result.IsLockedOut)
                    {
                        var lockoutDate = await _userManager.GetLockoutEndDateAsync(user);
                        var timeLeft = lockoutDate.Value - DateTime.UtcNow;
                        ModelState.AddModelError("", $"Hesabınız kitlendi,lütfen {timeLeft.Minutes} dakika sonra açılacaktır.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Parolanız hatalı");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Bu Email adresiyle bir hesap bulunamadı");
                }
            }
            else
            {
                ModelState.AddModelError("", "Lutfen form alanlarini kontrol edin.");
            }
            return View(model);
        }

        public async Task<IActionResult> ConfirmEmail(string id, string token)
        {
            if (id == null || token == null)
            {
                TempData["Message"] = "Geçersiz Token Bilgisi";
                return View();
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    TempData["Message"] = "Hesabınız başarıyla onaylandı";
                    return View();
                }
            }
            TempData["Message"] = "Hesabınız onaylanmadı";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            TempData["Message"] = "Bu sayfaya erisim yetkiniz yok.";
            return RedirectToAction(nameof(Login));
        }

        public async Task<IActionResult> ForgotPassword(string Email)
        {
            if (string.IsNullOrEmpty(Email))
            {
                TempData["Message"] = "E-posta adresinizi giriniz";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                TempData["Message"] = "E-posta adresinizle eşleşen kayıt yok";
                return View();
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = Url.Action("ResetPassword", "Account", new { user.Id, token }, Request.Scheme);

            try
            {
                await _emailSender.SendEmailAsync(
                    Email,
                    "Parola sifirlama",
                    $"Parolanizi sifirlamak icin linke <a href='{resetUrl}'>tiklayiniz.</a>");

                TempData["Message"] = "Parola sifirlama e-postasi gonderildi.";
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "Sifre sifirlama e-postasi gonderilemedi. Email: {Email}", Email);
                TempData["Message"] = "E-posta gonderilemedi. SMTP ayarlarini kontrol edin.";
            }

            return View();
        }

        public  IActionResult ResetPassword(string Id, string token)
        {
            if (Id == null || token == null)
            {
                return RedirectToAction("login");
            }
            var model = new ResetPasswordModel { Token = token };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    TempData["Message"] = "E-posta adresinizle eşleşen kayıt bulunamadı";
                    return RedirectToAction("Login");
                }
                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
                if (result.Succeeded)
                {
                    TempData["Message"] = "Parolanız başarıyla sıfırlandı";
                    return RedirectToAction("Login");
                }

                foreach (IdentityError err in result.Errors)
                {
                    ModelState.AddModelError("", err.Description);
                }

            }
            return View(model);


        }
    }
}
