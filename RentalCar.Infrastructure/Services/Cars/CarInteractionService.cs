using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Application.Dtos.Cars;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;
using RentalCar.Domain.Extensions;
using RentalCar.AI.Configuration;
using RentalCar.Infrastructure.Persistence.Context;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RentalCar.Infrastructure.Services.Cars
{
    public sealed class CarInteractionService : ICarInteractionService
    {
        private readonly RentalCarContext _context;
        private readonly CarServices _carServices;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OpenAiOptions _openAiOptions;
        private readonly ILogger<CarInteractionService> _logger;

        public CarInteractionService(
            RentalCarContext context,
            CarServices carServices,
            IHttpClientFactory httpClientFactory,
            IOptions<OpenAiOptions> openAiOptions,
            ILogger<CarInteractionService> logger)
        {
            _context = context;
            _carServices = carServices;
            _httpClientFactory = httpClientFactory;
            _openAiOptions = openAiOptions.Value;
            _logger = logger;
        }
        public async Task AddCommentAsync(int carId, string userId, string content, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Yorum içeriği boş olamaz", nameof(content));

            _context.CarComments.Add(new CarComment
            {
                CarId = carId,
                UserId = userId,
                Content = content.Trim(),
                CreatedOn = DateTime.Now,
                ModifiedOn = DateTime.Now
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<CarDetailsDto?> GetCarDetailAsync(int CarId, string? userId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var car = await _carServices.GetByIdAsync(CarId, cancellationToken);
            if (car == null) return null;

            if (!car.IsApproved && !isAdmin) return null;

            var comments = await _context.CarComments
                                 .AsNoTracking()
                                 .Where(x => x.CarId == CarId)
                                 .OrderByDescending(x => x.CreatedOn)
                                 .Take(50)
                                 .ToListAsync(cancellationToken);

            var commentUserIds = comments
                                .Select(x => x.UserId)
                                .Distinct()
                                .ToList();

            var commentUserMap = await _context.Users
                                       .Where(x => commentUserIds.Contains(x.Id))
                                       .ToDictionaryAsync(
                                        x => x.Id,
                                        x => x.UserName ?? "Kullanici",
                                        cancellationToken
                                        );

            var commentItems = comments.Select(c => new CarCommentItemDto
            {
                Id = c.Id,
                UserName = commentUserMap.TryGetValue(c.UserId, out var userName) ? userName : "Kullanici",
                Content = c.Content,
                CreatedOn = c.CreatedOn
            }).ToList();

            var ratings = await _context.CarRatings
                                .AsNoTracking()
                                .Where(x => x.CarId == CarId)
                                .ToListAsync(cancellationToken);

            var currentUserRating = userId is null
                                    ? null : ratings.FirstOrDefault(x => x.UserId == userId)?.Score;

            var isFavorite = userId is not null &&
                             await _context.Favorites.AnyAsync(
                                 x => x.CarId == CarId && x.UserId == userId,
                                 cancellationToken);

            var recommendedCars = new List<Car>();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var favoriteIds = await _context.Favorites
                    .AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .Select(x => x.CarId)
                    .ToListAsync(cancellationToken);

                if (favoriteIds.Count > 0)
                {
                    var favoriteCars = await _context.Cars
                        .AsNoTracking()
                        .Where(x => favoriteIds.Contains(x.Id))
                        .ToListAsync(cancellationToken);

                    var preferredBrands = favoriteCars
                        .Select(x => x.CatalogBrand ?? x.Brand)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var preferredFuel = favoriteCars.Select(x => x.FuelType).Distinct().ToList();
                    var preferredGear = favoriteCars.Select(x => x.Transmission).Distinct().ToList();
                    var preferredCity = favoriteCars.Select(x => x.City).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                    var preferredBody = favoriteCars.Select(x => x.BodyType).Distinct().ToList();
                    var avgPrice = favoriteCars.Where(x => x.ListedPrice.HasValue && x.ListedPrice.Value > 0).Select(x => x.ListedPrice!.Value).DefaultIfEmpty(0).Average();
                    var minPrice = avgPrice > 0 ? avgPrice * 0.75m : 0;
                    var maxPrice = avgPrice > 0 ? avgPrice * 1.25m : decimal.MaxValue;

                    var candidateCars = await _context.Cars
                        .AsNoTracking()
                        .Where(x => x.Id != CarId && x.IsApproved)
                        .Where(x =>
                            preferredBrands.Contains(x.CatalogBrand ?? x.Brand ?? string.Empty) ||
                            preferredFuel.Contains(x.FuelType) ||
                            preferredGear.Contains(x.Transmission) ||
                            preferredBody.Contains(x.BodyType) ||
                            preferredCity.Contains(x.City ?? string.Empty))
                        .Where(x => !x.ListedPrice.HasValue || x.ListedPrice.Value >= minPrice && x.ListedPrice.Value <= maxPrice)
                        .Take(20)
                        .ToListAsync(cancellationToken);

                    recommendedCars = await RankWithLlmByFavoritesAsync(favoriteCars, candidateCars, cancellationToken);
                }
            }

            var similarCars = await LoadSimilarCarsAsync(car, 8, cancellationToken);
            var seller = await BuildSellerSummaryAsync(car, cancellationToken);

            return new CarDetailsDto
            {
                Car = car,
                Comments = commentItems,
                RecommendedCars = recommendedCars,
                SimilarCars = similarCars,
                Seller = seller,
                AverageRating = ratings.Count == 0 ? 0 : ratings.Average(x => x.Score),
                RatingCount = ratings.Count,
                CurrentUserRating = currentUserRating,
                IsFavorite = isFavorite
            };


        }

        private async Task<List<Car>> LoadSimilarCarsAsync(Car car, int take, CancellationToken ct)
        {
            var brand = (car.CatalogBrand ?? car.Brand ?? "").Trim();
            var brandKey = brand.ToLowerInvariant();
            var modelToken = (car.CatalogModelName ?? car.Model ?? "").Trim();
            var modelKey = modelToken.ToLowerInvariant();
            var listed = car.ListedPrice ?? 0;
            var minP = listed > 0 ? listed * 0.72m : 0m;
            var maxP = listed > 0 ? listed * 1.28m : 0m;

            var baseQuery = _context.Cars.AsNoTracking()
                .Where(c => c.Id != car.Id && c.IsApproved);

            var candidates = await baseQuery
                .Where(c =>
                    (brandKey.Length > 0 &&
                     ((c.CatalogBrand != null && (c.CatalogBrand.ToLower() == brandKey || c.CatalogBrand.ToLower().Contains(brandKey))) ||
                      (c.Brand != null && (c.Brand.ToLower() == brandKey || c.Brand.ToLower().Contains(brandKey))))) ||
                    (listed > 0 && c.ListedPrice.HasValue && c.ListedPrice.Value > 0m &&
                     c.ListedPrice.Value >= minP && c.ListedPrice.Value <= maxP))
                .Take(100)
                .ToListAsync(ct);

            if (candidates.Count == 0)
            {
                return await baseQuery
                    .OrderByDescending(c => c.CreatedOn)
                    .Take(take)
                    .ToListAsync(ct);
            }

            int Score(Car c)
            {
                var cm = (c.CatalogModelName ?? c.Model ?? "").ToLowerInvariant();
                var s = 0;
                if (brandKey.Length > 0)
                {
                    var cb = (c.CatalogBrand ?? c.Brand ?? "").ToLowerInvariant();
                    if (cb.Contains(brandKey, StringComparison.Ordinal) || brandKey.Contains(cb, StringComparison.Ordinal))
                        s += 3;
                }

                if (modelKey.Length >= 2 && (cm.Contains(modelKey, StringComparison.Ordinal) || modelKey.Contains(cm, StringComparison.Ordinal)))
                    s += 5;
                return s;
            }

            decimal PriceDist(Car c)
            {
                if (listed <= 0) return 0;
                var p = c.ListedPrice ?? 0;
                if (p <= 0) return decimal.MaxValue;
                return Math.Abs(p - listed);
            }

            return candidates
                .OrderByDescending(Score)
                .ThenBy(PriceDist)
                .ThenByDescending(c => c.CreatedOn)
                .Take(take)
                .ToList();
        }

        private async Task<CarSellerSummaryDto> BuildSellerSummaryAsync(Car car, CancellationToken ct)
        {
            var profileLabel = car.SellerType.GetDisplayName();

            if (string.IsNullOrWhiteSpace(car.PostedByUserId))
            {
                var fallbackName = car.SellerType switch
                {
                    ListingSellerType.Galeri => "Galeri ilanı",
                    ListingSellerType.Sahibinden => "Sahibinden ilanı",
                    ListingSellerType.YetkiliBayi => "Yetkili bayi",
                    _ => "İlan sahibi"
                };
                return new CarSellerSummaryDto
                {
                    DisplayName = fallbackName,
                    ProfileTypeLabel = profileLabel,
                    HasAccount = false
                };
            }

            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == car.PostedByUserId, ct);

            if (user == null)
            {
                return new CarSellerSummaryDto
                {
                    DisplayName = "İlan sahibi",
                    ProfileTypeLabel = profileLabel,
                    HasAccount = false
                };
            }

            var other = await _context.Cars.CountAsync(
                c => c.PostedByUserId == car.PostedByUserId && c.Id != car.Id && c.IsApproved,
                ct);

            var display = string.IsNullOrWhiteSpace(user.FullName)
                ? (user.UserName ?? user.Email ?? "Üye")
                : user.FullName.Trim();

            return new CarSellerSummaryDto
            {
                DisplayName = display,
                UserId = user.Id,
                ProfileTypeLabel = profileLabel,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                OtherListingsCount = other,
                HasAccount = true
            };
        }

        public async Task RateAsync(int carId, string userId, int score, CancellationToken cancellationToken = default)
        {
            if (score < 1 || score > 5)
                throw new ArgumentException(nameof(score), "Skor 1 ile 5 arasında olmalı.");

            var rating = await _context.CarRatings
                        .FirstOrDefaultAsync(
                         x => x.CarId == carId && x.UserId == userId,
                         cancellationToken);

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
        }

        public async Task ToggledFavoriteAsync(int carId, string userId, CancellationToken cancellationToken = default)
        {
            var favorite = await _context.Favorites
                           .FirstOrDefaultAsync(
                            x => x.CarId == carId && x.UserId == userId,
                            cancellationToken);

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
                {
                    _context.Favorites.Remove(favorite);
                }
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<Car>> RecommendByCriteriaAsync(
            List<Car> candidates,
            string criteriaSummary,
            int maxCount = 6,
            CancellationToken cancellationToken = default)
        {
            if (candidates.Count == 0)
                return candidates;

            var safeMax = Math.Clamp(maxCount, 1, 12);
            var approved = candidates.Where(x => x.IsApproved).ToList();
            if (approved.Count == 0)
                approved = candidates;

            if (string.IsNullOrWhiteSpace(_openAiOptions.ApiKey))
                return approved.Take(safeMax).ToList();

            try
            {
                var candidateRows = string.Join("\n", approved.Select(x =>
                    $"{x.Id}: {(x.CatalogBrand ?? x.Brand)} {x.Model}, yil:{x.ModelYear}, yakit:{x.FuelType}, vites:{x.Transmission}, kasa:{x.BodyType}, km:{x.OdometerKm ?? 0}, sehir:{x.City}, fiyat:{x.ListedPrice ?? 0}"));

                var prompt =
                    $@"Kriterlere gore en uygun araclari sec.
Kriterler:
{criteriaSummary}

Adaylar:
{candidateRows}

Sadece JSON don:
{{""recommended_ids"":[id1,id2,id3]}}
Kurallar:
- Sadece aday id'leri kullan
- En fazla {safeMax} adet dondur
- Oncelik: marka-model uyumu, butce/fiyat uyumu, yakit-vites, yil ve km dengesi";

                var payload = new
                {
                    model = _openAiOptions.Model,
                    temperature = 0.15,
                    messages = new object[]
                    {
                        new { role = "system", content = "Sen arac secim asistanisin. Yalnizca gecerli JSON don." },
                        new { role = "user", content = prompt }
                    }
                };

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(Math.Max(5, _openAiOptions.TimeoutSeconds));
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_openAiOptions.BaseUrl.TrimEnd('/')}/chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiOptions.ApiKey);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                using var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return approved.Take(safeMax).ToList();

                var raw = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(raw);
                var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                if (string.IsNullOrWhiteSpace(content))
                    return approved.Take(safeMax).ToList();

                var jsonText = content.Trim();
                var start = jsonText.IndexOf('{');
                var end = jsonText.LastIndexOf('}');
                if (start < 0 || end <= start)
                    return approved.Take(safeMax).ToList();

                jsonText = jsonText[start..(end + 1)];
                using var jsonDoc = JsonDocument.Parse(jsonText);
                if (!jsonDoc.RootElement.TryGetProperty("recommended_ids", out var idsNode) || idsNode.ValueKind != JsonValueKind.Array)
                    return approved.Take(safeMax).ToList();

                var candidateMap = approved.ToDictionary(x => x.Id);
                var ordered = new List<Car>();
                foreach (var idNode in idsNode.EnumerateArray())
                {
                    if (!idNode.TryGetInt32(out var id))
                        continue;
                    if (candidateMap.TryGetValue(id, out var car))
                        ordered.Add(car);
                }

                if (ordered.Count < safeMax)
                {
                    var remaining = approved.Where(x => ordered.All(o => o.Id != x.Id)).Take(safeMax - ordered.Count);
                    ordered.AddRange(remaining);
                }

                return ordered.Take(safeMax).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM kriter bazli oneriler basarisiz. Fallback uygulanacak.");
                return approved.Take(safeMax).ToList();
            }
        }

        private async Task<List<Car>> RankWithLlmByFavoritesAsync(List<Car> favorites, List<Car> candidates, CancellationToken cancellationToken)
        {
            if (candidates.Count == 0)
                return candidates;

            if (string.IsNullOrWhiteSpace(_openAiOptions.ApiKey))
                return candidates.Take(6).ToList();

            try
            {
                var favoritesText = string.Join("\n", favorites.Select(x =>
                    $"{x.Id}: {(x.CatalogBrand ?? x.Brand)} {x.Model}, {x.FuelType}, {x.Transmission}, {x.BodyType}, {x.City}, {x.ListedPrice ?? 0}"));
                var candidatesText = string.Join("\n", candidates.Select(x =>
                    $"{x.Id}: {(x.CatalogBrand ?? x.Brand)} {x.Model}, {x.FuelType}, {x.Transmission}, {x.BodyType}, {x.City}, {x.ListedPrice ?? 0}"));

                var prompt =
                    $@"Kullanici favorilerine gore aday araclari sirala.
Favoriler:
{favoritesText}

Adaylar:
{candidatesText}

Sadece JSON don:
{{""recommended_ids"":[id1,id2,id3,id4,id5,id6]}}
Kurallar:
- Sadece aday listesinde olan idleri kullan
- En fazla 6 id don
- Oncelik: marka, fiyat bandi, yakit, vites, sehir, kasa tipi benzerligi";

                var payload = new
                {
                    model = _openAiOptions.Model,
                    temperature = 0.1,
                    messages = new object[]
                    {
                        new { role = "system", content = "Sen arac onerme siralama motorusun. Sadece gecerli JSON don." },
                        new { role = "user", content = prompt }
                    }
                };

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(Math.Max(5, _openAiOptions.TimeoutSeconds));
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_openAiOptions.BaseUrl.TrimEnd('/')}/chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiOptions.ApiKey);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                using var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return candidates.Take(6).ToList();

                var raw = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(raw);
                var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                if (string.IsNullOrWhiteSpace(content))
                    return candidates.Take(6).ToList();

                var jsonText = content.Trim();
                var start = jsonText.IndexOf('{');
                var end = jsonText.LastIndexOf('}');
                if (start < 0 || end <= start)
                    return candidates.Take(6).ToList();

                jsonText = jsonText[start..(end + 1)];
                using var jsonDoc = JsonDocument.Parse(jsonText);
                if (!jsonDoc.RootElement.TryGetProperty("recommended_ids", out var idsNode) || idsNode.ValueKind != JsonValueKind.Array)
                    return candidates.Take(6).ToList();

                var candidateMap = candidates.ToDictionary(x => x.Id);
                var ordered = new List<Car>();
                foreach (var idNode in idsNode.EnumerateArray())
                {
                    if (!idNode.TryGetInt32(out var id))
                        continue;
                    if (candidateMap.TryGetValue(id, out var car))
                        ordered.Add(car);
                }

                if (ordered.Count == 0)
                    return candidates.Take(6).ToList();

                var extra = candidates.Where(x => ordered.All(o => o.Id != x.Id)).Take(6 - ordered.Count);
                ordered.AddRange(extra);
                return ordered.Take(6).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM favori öneri sıralaması başarısız. Kural tabanlı fallback kullanılıyor.");
                return candidates.Take(6).ToList();
            }
        }
    }
}
