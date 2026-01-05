// ===============================
// 2) CarController.cs
// ===============================
using Microsoft.AspNetCore.Mvc;
using RentalCar.Data.Dtos;
using RentalCar.Data.Enums;
using RentalCar.Data.Models;
using RentalCar.Data.Models.Requests;
using RentalCar.Data.Services;
using DriveType = RentalCar.Data.Enums.DriveType;

namespace RentalCar.Controllers
{
    public class CarController : Controller
    {
        private readonly CarServices _carsServices;
        private readonly PricingApiClient _pricing;
        private readonly DescriptionService _descriptionService;

        public CarController(CarServices carServices, PricingApiClient pricing, DescriptionService descriptionService)
        {
            _carsServices = carServices;
            _pricing = pricing;
            _descriptionService = descriptionService;
        }

        public IActionResult Index() { return View(); }
        [HttpGet] 
        public IActionResult Get(int id) 
        { 
            return View(); 
        }


        [HttpPost]
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



            //newCar.Security = (SelectedSecurity != null && SelectedSecurity.Any())
            //    ? SelectedSecurity.Aggregate(Security.None, (acc, val) => acc | (Security)val)
            //    : Security.None;

            //newCar.InternalEquipment = (SelectedInternal != null && SelectedInternal.Any())
            //    ? SelectedInternal.Aggregate(InternalEquipment.None, (acc, val) => acc | (InternalEquipment)val)
            //    : InternalEquipment.None;

            //newCar.ExternalEquipment = (SelectedExternal != null && SelectedExternal.Any())
            //    ? SelectedExternal.Aggregate(ExternalEquipment.None, (acc, val) => acc | (ExternalEquipment)val)
            //    : ExternalEquipment.None;

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
        public async Task<IActionResult> List(
            string? brand,
            string? model,
            string? renk,
            FuelType? yakitTuru,
            Gear? vites,
            BodyType? bodyType,
            DateTime? yil,
            string? searchString,
            CancellationToken cancellationToken = default)
        {
            var all = await _carsServices.GetAllAsync();
            if (all == null)
                return Problem("Cars is null");

            var query = all.AsQueryable();

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
                query = query.Where(c => c.yil == yil);

            return View(query.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
        {
            var car = await _carsServices.GetByIdAsync(id, cancellationToken);
            if (car == null) return NotFound();
            return View(car);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var car = await _carsServices.GetByIdAsync(id, cancellationToken);
            if (car == null) return NotFound();
            return View(car);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Car updateCar, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return View(updateCar);

            updateCar.ModifiedOn = DateTime.Now;
            await _carsServices.UpdateAsync(updateCar, cancellationToken);

            return RedirectToAction(nameof(List));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            var car = await _carsServices.GetByIdAsync(id, cancellationToken);
            if (car == null) return NotFound();
            return View(car);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirm(int id, CancellationToken cancellationToken = default)
        {
            var success = await _carsServices.DeleteAsync(id, cancellationToken);
            if (!success) return NotFound();
            return RedirectToAction(nameof(List));
        }
    }
}
