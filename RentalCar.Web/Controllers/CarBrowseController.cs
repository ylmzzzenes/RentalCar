using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;
using RentalCar.Infrastructure.Services.Cars;
using RentalCar.ViewModels;
using System.Security.Claims;

namespace RentalCar.Web.Controllers
{
    public class CarBrowseController : Controller
    {
        private readonly CarServices _carsServices;
        private readonly ICarInteractionService _carInteractionService;
        private readonly ICarListingReliabilityService _listingReliabilityService;
        private readonly ICarListingInsightService _listingInsightService;

        public CarBrowseController(
            CarServices carsServices,
            ICarInteractionService carInteractionService,
            ICarListingReliabilityService listingReliabilityService,
            ICarListingInsightService listingInsightService)
        {
            _carsServices = carsServices;
            _carInteractionService = carInteractionService;
            _listingReliabilityService = listingReliabilityService;
            _listingInsightService = listingInsightService;
        }
    
        [HttpGet]
        public IActionResult Get(int id)
        {
            return RedirectToAction("Details", "CarBrowse", new { id });
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> List(
        string? brand,
        string? model,
        string? renk,
        string? city,
        FuelType? yakitTuru,
        Gear? vites,
        BodyType? bodyType,
        int? yil,
        int? minYil,
        int? maxYil,
        int? minKm,
        int? maxKm,
        decimal? minFiyat,
        decimal? maxFiyat,
        string? searchString,
        string? postedBy,
        CancellationToken cancellationToken = default)
        {
            var all = await _carsServices.GetAllAsync(cancellationToken);
            if (all == null)
                return Problem("Cars is null");

            var query = all.AsQueryable();
            if (!User.IsInRole("Admin"))
                query = query.Where(c => c.IsApproved);

            if (!string.IsNullOrWhiteSpace(postedBy))
                query = query.Where(c => c.PostedByUserId == postedBy);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();

                query = query.Where(c =>
                    (c.Brand != null && c.Brand.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.Model != null && c.Model.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.Color != null && c.Color.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.CatalogBrand != null && c.CatalogBrand.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.CatalogModelName != null && c.CatalogModelName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.TrimPackage != null && c.TrimPackage.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.EngineCode != null && c.EngineCode.Contains(s, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (!string.IsNullOrWhiteSpace(brand))
                query = query.Where(c => c.Brand != null && c.Brand.Contains(brand, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(model))
                query = query.Where(c => c.Model != null && c.Model.Contains(model, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(renk))
                query = query.Where(c => c.Color != null && c.Color.Contains(renk, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(city))
                query = query.Where(c => c.City != null && c.City.Contains(city, StringComparison.OrdinalIgnoreCase));

            if (yakitTuru != null)
                query = query.Where(c => c.FuelType == yakitTuru);

            if (vites != null)
                query = query.Where(c => c.Transmission == vites);

            if (bodyType != null)
                query = query.Where(c => c.BodyType == bodyType);

            if (yil != null)
                query = query.Where(c => c.ModelYear == yil);

            if (minYil.HasValue)
                query = query.Where(c => c.ModelYear.HasValue && c.ModelYear.Value >= minYil.Value);

            if (maxYil.HasValue)
                query = query.Where(c => c.ModelYear.HasValue && c.ModelYear.Value <= maxYil.Value);

            if (minKm.HasValue)
                query = query.Where(c => c.OdometerKm.HasValue && c.OdometerKm.Value >= minKm.Value);

            if (maxKm.HasValue)
                query = query.Where(c => c.OdometerKm.HasValue && c.OdometerKm.Value <= maxKm.Value);

            if (minFiyat.HasValue)
                query = query.Where(c => c.ListedPrice.HasValue && c.ListedPrice.Value >= minFiyat.Value);

            if (maxFiyat.HasValue)
                query = query.Where(c => c.ListedPrice.HasValue && c.ListedPrice.Value <= maxFiyat.Value);

            var list = query
                .OrderByDescending(c => c.CreatedOn)
                .ToList();
            var reliability = await _listingReliabilityService.CalculateBatchAsync(list, cancellationToken);

            var criteriaSummary =
                $"marka:{brand ?? "-"}, model:{model ?? "-"}, renk:{renk ?? "-"}, sehir:{city ?? "-"}, yakit:{yakitTuru?.ToString() ?? "-"}, vites:{vites?.ToString() ?? "-"}, kasa:{bodyType?.ToString() ?? "-"}, yil:{yil?.ToString() ?? "-"}, minYil:{minYil?.ToString() ?? "-"}, maxYil:{maxYil?.ToString() ?? "-"}, minKm:{minKm?.ToString() ?? "-"}, maxKm:{maxKm?.ToString() ?? "-"}, minFiyat:{minFiyat?.ToString() ?? "-"}, maxFiyat:{maxFiyat?.ToString() ?? "-"}, arama:{searchString ?? "-"}";
            var hasFilterCriteria =
                !string.IsNullOrWhiteSpace(searchString) ||
                !string.IsNullOrWhiteSpace(brand) ||
                !string.IsNullOrWhiteSpace(model) ||
                !string.IsNullOrWhiteSpace(renk) ||
                !string.IsNullOrWhiteSpace(city) ||
                yakitTuru.HasValue || vites.HasValue || bodyType.HasValue ||
                yil.HasValue || minYil.HasValue || maxYil.HasValue ||
                minKm.HasValue || maxKm.HasValue ||
                minFiyat.HasValue || maxFiyat.HasValue;

            var recommendationCandidates = all
                .Where(c => User.IsInRole("Admin") || c.IsApproved)
                .Where(c => list.All(x => x.Id != c.Id))
                .ToList();
            ViewBag.RecommendedCars = hasFilterCriteria
                ? await _carInteractionService.RecommendByCriteriaAsync(recommendationCandidates, criteriaSummary, maxCount: 6, cancellationToken)
                : new List<Car>();

            ViewBag.Brand = brand;
            ViewBag.Model = model;
            ViewBag.Renk = renk;
            ViewBag.City = city;
            ViewBag.YakitTuru = yakitTuru?.ToString();
            ViewBag.Vites = vites?.ToString();
            ViewBag.BodyType = bodyType?.ToString();
            ViewBag.Yil = yil;
            ViewBag.MinYil = minYil;
            ViewBag.MaxYil = maxYil;
            ViewBag.MinKm = minKm;
            ViewBag.MaxKm = maxKm;
            ViewBag.MinFiyat = minFiyat;
            ViewBag.MaxFiyat = maxFiyat;
            ViewBag.SearchString = searchString;
            ViewBag.PostedBy = postedBy;
            ViewBag.ReliabilityScores = reliability;
            return View(list);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var result = await _carInteractionService.GetCarDetailAsync(
                id,
                userId,
                isAdmin,
                cancellationToken);

            if (result == null) return NotFound();
            var reliability = await _listingReliabilityService.CalculateAsync(result.Car, cancellationToken);
            var insights = await _listingInsightService.GetInsightsAsync(id, userId, cancellationToken);

            var model = new CarDetailsViewModel
            {
                Car = result.Car,
                Seller = result.Seller,
                SimilarCars = result.SimilarCars,
                ListingInsights = insights,
                RecommendedCars = result.RecommendedCars,
                Comments = result.Comments.Select(x => new CarComment
                {
                    Id = x.Id,
                    Content = $"{x.UserName}|{x.Content}",
                    CreatedOn = x.CreatedOn

                }).ToList(),
                AverageRating = result.AverageRating,
                RatingCount = result.RatingCount,
                CurrentUserRating = result.CurrentUserRating,
                IsFavorite = result.IsFavorite,
                ReliabilityScore = reliability.Score,
                ReliabilityLabel = reliability.Label,
                ReliabilityTrustLevelTr = reliability.TrustLevelTr,
                ReliabilityExplanation = reliability.UserExplanation,
                ReliabilityFactors = isAdmin ? reliability.Factors : null
            };

            return View(model);
        }


    }
}
