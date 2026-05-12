using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Application.Contracts.Cars;
using RentalCar.Application.Services.Cars;
using RentalCar.Domain.Entities;
using RentalCar.Infrastructure.Services.Cars;
using RentalCar.Web.Mappers;
using RentalCar.Web.Models.Requests;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;

namespace RentalCar.Web.Controllers
{
    public class CarManagementController : Controller
    {
        private readonly CarServices _carsServices;
        private readonly ICarAppService _carAppService;
        private readonly ICarListingReliabilityService _listingReliabilityService;
        private readonly ICarImageStorageService _carImageStorageService;
        private readonly ICreateCarCommandMapper _createCarCommandMapper;
        private readonly IWebHostEnvironment _environment;

        public CarManagementController(
            CarServices carsServices,
            ICarAppService carAppService,
            ICarListingReliabilityService listingReliabilityService,
            IWebHostEnvironment environment,
            ICarImageStorageService carImageStorageService,
            ICreateCarCommandMapper createCarCommandMapper)
        {
            _carsServices = carsServices;
            _carAppService = carAppService;
            _listingReliabilityService = listingReliabilityService;
            _environment = environment;
            _carImageStorageService = carImageStorageService;
            _createCarCommandMapper = createCarCommandMapper;
        }
        [Authorize]
        public IActionResult Index() => View(CreateCarFormDefaults.Create());


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromForm] CreateCar request,CancellationToken cancellationToken=default)
        {
            if (!ModelState.IsValid)
                return View("Index", request);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var fileNames = (await _carImageStorageService.SaveAsync(request.PhotoUploads, cancellationToken)).ToList();
           var command = _createCarCommandMapper.Map(request, userId, fileNames);
            var prepareResult = await _carAppService.PrepareCarForCreateAsync(command, cancellationToken);

            if (!prepareResult.Success)
            {
                var msg = prepareResult.Message ?? "Araç oluşturulamadı.";
                var chunks = msg.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var chunk in chunks)
                {
                    var t = chunk.Trim();
                    if (t.Length == 0) continue;
                    ModelState.AddModelError(string.Empty, t.EndsWith('.') ? t : t + ".");
                }

                if (ModelState.ErrorCount == 0)
                    ModelState.AddModelError(string.Empty, msg);

                return View("Index", request);
            }


            var newCar = prepareResult.Data;
            newCar.PostedByUserId = userId;
            await _carsServices.CreateAsync(newCar);
            var reliability = await _listingReliabilityService.CalculateAsync(newCar, HttpContext.RequestAborted);
            TempData["CreateSuccess"] = "Arac ilani olusturuldu.";
            TempData["CreateAiSummary"] =
                $"AI Fiyat: {newCar.PredictedPriceMid:N0} TL | Güven skoru: {reliability.Score}/100 — {reliability.TrustLevelTr}";
            return RedirectToAction("Details", "CarBrowse", new { id = newCar.Id });
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
        public async Task<IActionResult> Edit(Car posted, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return View(posted);

            var existing = await _carsServices.GetByIdAsync(posted.Id, cancellationToken);
            if (existing == null)
                return NotFound();

            existing.CatalogBrand = posted.CatalogBrand;
            existing.Brand = posted.Brand;
            existing.Series = posted.Series;
            existing.Model = posted.Model;
            existing.CatalogModelName = posted.Model;
            existing.ModelYear = posted.ModelYear;
            existing.OdometerKm = posted.OdometerKm;
            existing.ListedPrice = posted.ListedPrice;
            existing.Transmission = posted.Transmission;
            existing.FuelType = posted.FuelType;
            existing.EngineDisplacementLiters = posted.EngineDisplacementLiters;
            existing.EnginePowerHp = posted.EnginePowerHp;
            existing.Drivetrain = posted.Drivetrain;
            existing.FuelTankLiters = posted.FuelTankLiters;
            existing.BodyType = posted.BodyType;
            existing.Color = posted.Color;
            existing.VehicleCondition = posted.VehicleCondition;
            existing.BodyWorkNotes = posted.BodyWorkNotes;
            existing.TradeInAccepted = posted.TradeInAccepted;
            existing.SellerType = posted.SellerType;
            if (!string.IsNullOrWhiteSpace(posted.PostedByUserId))
                existing.PostedByUserId = posted.PostedByUserId;
            existing.Plate = posted.Plate;
            existing.ImageUrls = posted.ImageUrls;
            existing.ModifiedOn = DateTime.Now;

            await _carsServices.UpdateAsync(existing, cancellationToken);

            return RedirectToAction("List", "CarBrowse");
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
            return RedirectToAction("List", "CarBrowse");
        }

       
    }
}
