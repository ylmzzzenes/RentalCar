// ===============================
// 2) CarController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalCar.Application.Dtos.AI;
using RentalCar.Data.Enums;
using RentalCar.Data.Models;
using RentalCar.Data.Models.Requests;
using RentalCar.Infrastructure.AI.Services;
using RentalCar.Infrastructure.Persistence.Context;
using RentalCar.Infrastructure.Services;
using RentalCar.ViewModels;
using System.Security.Claims;
using DriveType = RentalCar.Data.Enums.DriveType;

namespace RentalCar.Controllers
{
    public class CarController : Controller
    {
        private readonly CarServices _carsServices;
        private readonly PricingApiClient _pricing;
        private readonly DescriptionService _descriptionService;
        private readonly RentalCarContext _context;
        private readonly UserManager<AppUser> _userManager;

        public CarController(
            CarServices carServices,
            PricingApiClient pricing,
            DescriptionService descriptionService,
            RentalCarContext context,
            UserManager<AppUser> userManager)
        {
            _carsServices = carServices;
            _pricing = pricing;
            _descriptionService = descriptionService;
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Index() { return View(); }
        [HttpGet] 
        public IActionResult Get(int id) 
        { 
            return RedirectToAction(nameof(Details), new { id }); 
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            CreateCar request,
            List<int> SelectedSecurity,
            List<int> SelectedInternal,
            List<int> SelectedExternal,
            List<int> SelectedBodyType,
            List<int> SelectedFuelType,
            List<int> SelectedGear,
            List<int> SelectedDriveType,
            CancellationToken cancellationToken = default)
        {
            Car newCar = new Car();
            var fileNames = new List<string>();

            if (request.ImageUrls != null && request.ImageUrls.Any())
            {
                foreach (var file in request.ImageUrls)
                {
                    var extension = Path.GetExtension(file.FileName);
                    var newFileName = Guid.NewGuid() + extension;
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images/Upload/", newFileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                    using var stream = new FileStream(path, FileMode.Create);
                    await file.CopyToAsync(stream);

                    fileNames.Add(newFileName);
                }
            }

            newCar.ImageUrls = string.Join(",", fileNames);

            var x = DateTime.Now;

            //newCar.Vin = request.Vin;
            //newCar.Brand = request.Brand;
            newCar.model = request.model;
            newCar.yil = request.yil;
            newCar.renk = request.renk;
            //newCar.Plate = request.Plate;

            newCar.modelraw = request.modelraw;
            newCar.marka = request.marka;
            newCar.model_adi = request.model_adi;
            newCar.paket = request.paket;
            newCar.motor_kodu = request.motor_kodu;
            newCar.sanziman_kodu = request.sanziman_kodu;

            newCar.kilometre = request.kilometre;
            newCar.vergi = request.vergi;
            newCar.lt_100km = request.lt_100km;
            newCar.motorHacmi = request.motorHacmi;
            newCar.fiyat = request.fiyat;
            newCar.sehir = request.sehir;
            newCar.donanimSeviyesi = request.donanimSeviyesi;
            newCar.hasarKaydi = request.hasarKaydi;
            newCar.servisGecmisi = request.servisGecmisi;
            newCar.motorGuc_hp = request.motorGuc_hp;
            newCar.tork_nm = request.tork_nm;
            newCar.sahipSayisi = request.sahipSayisi;
            newCar.modelraw = request.modelraw;



            newCar.Security = (SelectedSecurity != null && SelectedSecurity.Any())
                ? SelectedSecurity.Aggregate(Security.None, (acc, val) => acc | (Security)val)
                : Security.None;

            newCar.InternalEquipment = (SelectedInternal != null && SelectedInternal.Any())
                ? SelectedInternal.Aggregate(InternalEquipment.None, (acc, val) => acc | (InternalEquipment)val)
                : InternalEquipment.None;

            newCar.ExternalEquipment = (SelectedExternal != null && SelectedExternal.Any())
                ? SelectedExternal.Aggregate(ExternalEquipment.None, (acc, val) => acc | (ExternalEquipment)val)
                : ExternalEquipment.None;

            newCar.BodyType = (SelectedBodyType != null && SelectedBodyType.Any())
                ? SelectedBodyType.Aggregate(BodyType.None, (acc, val) => acc | (BodyType)val)
                : BodyType.None;

            newCar.yakitTuru = (SelectedFuelType != null && SelectedFuelType.Any())
                ? SelectedFuelType.Aggregate(FuelType.None, (acc, val) => acc | (FuelType)val)
                : FuelType.None;

            newCar.vites = (SelectedGear != null && SelectedGear.Any())
                ? SelectedGear.Aggregate(Gear.None, (acc, val) => acc | (Gear)val)
                : Gear.None;

            newCar.cekis = (SelectedDriveType != null && SelectedDriveType.Any())
                ? SelectedDriveType.Aggregate(DriveType.None, (acc, val) => acc | (DriveType)val)
                : DriveType.None;

            if (newCar.yakitTuru == FuelType.None && request.yakitTuru != FuelType.None)
                newCar.yakitTuru = request.yakitTuru;
            if (newCar.vites == Gear.None && request.vites != Gear.None)
                newCar.vites = request.vites;
            if (newCar.cekis == DriveType.None && request.cekis != DriveType.None)
                newCar.cekis = request.cekis;

            newCar.CreatedOn = x;
            newCar.ModifiedOn = x;

            try
            {
                var payload = MapCarToPredict(newCar);
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(payload));

                var pred = await _pricing.PredictAsync(payload, cancellationToken);

                var mid = pred.Mid ?? pred.Prediction;
                if (mid == null)
                    throw new Exception("Python response içinde mid/prediction yok.");

                var low = pred.Low ?? mid.Value;
                var high = pred.High ?? mid.Value;

                newCar.fiyat_tahmin = mid.Value;
                newCar.fiyat_min = low;
                newCar.fiyat_max = high;

                if (newCar.fiyat == 0)
                    newCar.fiyat = newCar.fiyat_tahmin;

                
                Dictionary<string, object?> dataDict;

               
                dataDict = new Dictionary<string, object?>
                {
                    ["model"] = newCar.model,
                    ["marka"] = newCar.marka,
                    ["model_adi"] = newCar.model_adi,
                    ["paket"] = newCar.paket,
                    ["motor_kodu"] = newCar.motor_kodu,
                    ["sanziman_kodu"] = newCar.sanziman_kodu,
                    ["yil"] = newCar.yil,
                    ["kilometre"] = newCar.kilometre,
                    ["yakitTuru"] = newCar.yakitTuru.ToString(),
                    ["vites"] = newCar.vites.ToString(),
                    ["cekis"] = newCar.cekis.ToString(),
                    ["vergi"] = newCar.vergi,
                    ["lt_100km"] = newCar.lt_100km,
                    ["motorHacmi"] = newCar.motorHacmi,
                    ["renk"] = newCar.renk,
                    ["sehir"] = newCar.sehir,
                    ["kasaTipi"] = newCar.kasaTipi,
                    ["donanimSeviyesi"] = newCar.donanimSeviyesi,
                    ["hasarKaydi"] = newCar.hasarKaydi,
                    ["degisenBoyanan"] = newCar.degisenBoyanan,
                    ["servisGecmisi"] = newCar.servisGecmisi,
                    ["motorGuc_hp"] = newCar.motorGuc_hp,
                    ["tork_nm"] = newCar.tork_nm,
                    ["sahipSayisi"] = newCar.sahipSayisi,
                };

                var descReq = new DescribeRequestDto
                {
                    data = dataDict,
                    predicted_mid = newCar.fiyat_tahmin,
                    predicted_low = newCar.fiyat_min,
                    predicted_high = newCar.fiyat_max
                };

                var desc = await _descriptionService.DescribeAsync(descReq, cancellationToken);

                newCar.aciklama_kisa = desc.@short;
                newCar.aciklama = desc.@long;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }


            await _carsServices.CreateAsync(newCar, cancellationToken);
            return RedirectToAction(nameof(Details), new { id = newCar.Id });
        }

        private static string MapGear(Gear g) => g switch
        {
            Gear.Manuel => "Manuel",
            Gear.Otomatik => "Otomatik",
            Gear.YarıOtomatik => "YarıOtomatik",
            _ => "Manuel"
        };

        private static string MapFuelType(FuelType f) => f switch 
        {
            FuelType.Benzin => "Benzin",
            FuelType.Dizel => "Dizel",
            FuelType.Elektrik => "Elektrik",
            FuelType.Hibrit => "Hibrit",
            _ => "Benzin"
        };

        private static string MapDriveType(DriveType d) => d switch 
        {
            DriveType.FWD => "FWD",
            DriveType.RWD => "RWD",
            DriveType.AWD => "AWD",
            DriveType.FourByFour => "4x4",
            _ => "FWD"
        };
        private static PredictRequestDto MapCarToPredict(Car car)
        {
           
            var yilInt = car.yil?.Year ?? 0;

            return new PredictRequestDto
            {
                marka = car.marka ?? car.Brand ?? "Bilinmiyor",
                model_adi = car.model_adi ?? car.model ?? "Bilinmiyor",
                paket = car.paket ?? "Standard",
                motor_kodu = car.motor_kodu ?? "Bilinmiyor",
                cekis = car.cekis.ToString(),
                sanziman_kodu = car.sanziman_kodu ?? "Bilinmiyor",
                vites = car.vites.ToString(),
                yakitTuru = car.yakitTuru.ToString(),
                renk = car.renk ?? "Bilinmiyor",

                
                sehir = car.sehir,                    
                kasaTipi = car.kasaTipi,                
                donanimSeviyesi = car.donanimSeviyesi,  
                hasarKaydi = car.hasarKaydi,           
                degisenBoyanan = car.degisenBoyanan,    
                servisGecmisi = car.servisGecmisi,      

                motorGuc_hp = car.motorGuc_hp,          
                tork_nm = car.tork_nm,                 
                sahipSayisi = car.sahipSayisi,          

                yil = yilInt,
                kilometre = car.kilometre ?? 0,
                vergi = car.vergi ?? 0,
                lt_100km = car.lt_100km ?? 0.0,
                motorHacmi = car.motorHacmi ?? 0.0,
            };
        }





        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> List(
            string? brand,
            string? model,
            string? renk,
            FuelType? yakitTuru,
            Gear? vites,
            BodyType? bodyType,
            int? yil,
            string? searchString,
            CancellationToken cancellationToken = default)
        {
            var all = await _carsServices.GetAllAsync(cancellationToken);
            if (all == null)
                return Problem("Cars is null");

            var query = all.AsQueryable();
            if (!User.IsInRole("Admin"))
                query = query.Where(c => c.IsApproved);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();

                query = query.Where(c =>
                    (c.Brand != null && c.Brand.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.model != null && c.model.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.renk != null && c.renk.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.marka != null && c.marka.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.model_adi != null && c.model_adi.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.paket != null && c.paket.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.motor_kodu != null && c.motor_kodu.Contains(s, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (!string.IsNullOrWhiteSpace(brand))
                query = query.Where(c => c.Brand != null && c.Brand.Contains(brand, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(model))
                query = query.Where(c => c.model != null && c.model.Contains(model, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(renk))
                query = query.Where(c => c.renk != null && c.renk.Contains(renk, StringComparison.OrdinalIgnoreCase));

            if (yakitTuru != null)
                query = query.Where(c => c.yakitTuru == yakitTuru);

            if (vites != null)
                query = query.Where(c => c.vites == vites);

            if (bodyType != null)
                query = query.Where(c => c.BodyType == bodyType);

            if (yil != null)
                query = query.Where(c => c.yil.HasValue && c.yil.Value.Year == yil.Value);

            return View(query.ToList());
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
        {
            var car = await _carsServices.GetByIdAsync(id, cancellationToken);
            if (car == null) return NotFound();
            if (!car.IsApproved && !User.IsInRole("Admin")) return NotFound();

            var comments = await _context.CarComments
                .AsNoTracking()
                .Where(x => x.CarId == id)
                .OrderByDescending(x => x.CreatedOn)
                .Take(50)
                .ToListAsync(cancellationToken);

            var commentUserIds = comments.Select(x => x.UserId).Distinct().ToList();
            var commentUserMap = await _context.Users
                .Where(x => commentUserIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.UserName ?? "Kullanici", cancellationToken);

            foreach (var c in comments)
            {
                if (commentUserMap.TryGetValue(c.UserId, out var userName))
                {
                    c.Content = $"{userName}|{c.Content}";
                }
            }

            var ratings = await _context.CarRatings
                .AsNoTracking()
                .Where(x => x.CarId == id)
                .ToListAsync(cancellationToken);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentRating = userId is null ? null : ratings.FirstOrDefault(x => x.UserId == userId)?.Score;
            var isFavorite = userId is not null &&
                             await _context.Favorites.AnyAsync(x => x.CarId == id && x.UserId == userId, cancellationToken);

            var model = new CarDetailsViewModel
            {
                Car = car,
                Comments = comments,
                AverageRating = ratings.Count == 0 ? 0 : ratings.Average(x => x.Score),
                RatingCount = ratings.Count,
                CurrentUserRating = currentRating,
                IsFavorite = isFavorite
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(int carId, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var favorite = await _context.Favorites.FirstOrDefaultAsync(x => x.CarId == carId && x.UserId == userId, cancellationToken);
            if (favorite == null)
            {
                _context.Favorites.Add(new Favorite
                {
                    CarId = carId,
                    UserId = userId,
                    CreatedOn = DateTime.Now,
                    ModifiedOn = DateTime.Now
                });
            }
            else
            {
                _context.Favorites.Remove(favorite);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return RedirectToAction(nameof(Details), new { id = carId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Rate(int carId, int score, CancellationToken cancellationToken = default)
        {
            if (score < 1 || score > 5)
                return RedirectToAction(nameof(Details), new { id = carId });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var rating = await _context.CarRatings.FirstOrDefaultAsync(x => x.CarId == carId && x.UserId == userId, cancellationToken);
            if (rating == null)
            {
                _context.CarRatings.Add(new CarRating
                {
                    CarId = carId,
                    UserId = userId,
                    Score = score,
                    CreatedOn = DateTime.Now,
                    ModifiedOn = DateTime.Now
                });
            }
            else
            {
                rating.Score = score;
                rating.ModifiedOn = DateTime.Now;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return RedirectToAction(nameof(Details), new { id = carId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(int carId, string content, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction(nameof(Details), new { id = carId });

            _context.CarComments.Add(new CarComment
            {
                CarId = carId,
                UserId = userId,
                Content = content.Trim(),
                CreatedOn = DateTime.Now,
                ModifiedOn = DateTime.Now
            });
            await _context.SaveChangesAsync(cancellationToken);
            return RedirectToAction(nameof(Details), new { id = carId });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var car = await _carsServices.GetByIdAsync(id, cancellationToken);
            if (car == null) return NotFound();
            return View(car);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Car updateCar, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return View(updateCar);

            updateCar.ModifiedOn = DateTime.Now;
            await _carsServices.UpdateAsync(updateCar, cancellationToken);

            return RedirectToAction(nameof(List));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            var car = await _carsServices.GetByIdAsync(id, cancellationToken);
            if (car == null) return NotFound();
            return View(car);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirm(int id, CancellationToken cancellationToken = default)
        {
            var success = await _carsServices.DeleteAsync(id, cancellationToken);
            if (!success) return NotFound();
            return RedirectToAction(nameof(List));
        }
    }
}
