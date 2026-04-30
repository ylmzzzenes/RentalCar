using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Application.Contracts.Cars;
using RentalCar.Domain.Entities;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.Infrastructure.Services.Cars;

public sealed class CarCatalogImageSyncService : ICarCatalogImageSyncService
{
    private readonly RentalCarContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CarCatalogImageSyncService> _logger;

    public CarCatalogImageSyncService(
        RentalCarContext context,
        IWebHostEnvironment environment,
        IHttpClientFactory httpClientFactory,
        ILogger<CarCatalogImageSyncService> logger)
    {
        _context = context;
        _environment = environment;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CarBrandModelExportRow>> GetBrandModelExportRowsAsync(CancellationToken cancellationToken = default)
    {
        var cars = await _context.Cars.AsNoTracking().ToListAsync(cancellationToken);
        return BuildExportRows(cars);
    }

    public async Task<CarCatalogImageSyncResult> DownloadAndAttachToCarsAsync(
        bool replaceRemoteUrls,
        CancellationToken cancellationToken = default)
    {
        var result = new CarCatalogImageSyncResult();
        var cars = await _context.Cars.ToListAsync(cancellationToken);
        result.CarsProcessed = cars.Count;

        var uploadDir = Path.Combine(_environment.WebRootPath, "Images", "Upload");
        Directory.CreateDirectory(uploadDir);

        var client = _httpClientFactory.CreateClient(nameof(CarCatalogImageSyncService));
        client.Timeout = TimeSpan.FromSeconds(90);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "RentalCarCatalogImageSync/1.0 (local dev; respectful delays between Commons requests)");

        var keyToLocalFiles = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var car in cars)
        {
            var key = CarCatalogImageLibrary.BuildLookupKey(car.CatalogBrand, car.Brand, car.CatalogModelName, car.Model);
            if (string.IsNullOrEmpty(key))
            {
                result.CarsSkipped++;
                continue;
            }

            var remote = CarCatalogImageLibrary.TryGetRemoteUrls(key);
            if (remote is null || remote.Count == 0)
            {
                result.CarsSkipped++;
                continue;
            }

            if (!replaceRemoteUrls && HasLocalImageReference(car.ImageUrls))
            {
                result.CarsSkipped++;
                continue;
            }

            if (!replaceRemoteUrls && car.ImageUrls is { Count: > 0 })
            {
                result.CarsSkipped++;
                continue;
            }

            try
            {
                if (!keyToLocalFiles.TryGetValue(key, out var localNames))
                {
                    localNames = new List<string>();
                    var slug = ShortHash(key);
                    for (var i = 0; i < remote.Count; i++)
                    {
                        var url = remote[i];
                        var ext = await ResolveExtensionAsync(client, url, cancellationToken);
                        var fileName = $"catalog_{slug}_{i}{ext}";
                        var fullPath = Path.Combine(uploadDir, fileName);

                        if (!File.Exists(fullPath))
                        {
                            await DownloadToFileAsync(client, url, fullPath, cancellationToken);
                            result.FilesDownloaded++;
                            await Task.Delay(TimeSpan.FromSeconds(2.5), cancellationToken);
                        }

                        localNames.Add(fileName);
                    }

                    keyToLocalFiles[key] = localNames;
                }

                car.ImageUrls = localNames.ToList();
                _context.Entry(car).Property(c => c.ImageUrls).IsModified = true;
                result.CarsUpdated++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Catalog image sync failed for car {CarId}, key {Key}", car.Id, key);
                result.Errors.Add($"Car {car.Id} ({key}): {ex.Message}");
                result.CarsSkipped++;
            }
        }

        if (result.CarsUpdated > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return result;
    }

    private static List<CarBrandModelExportRow> BuildExportRows(List<Car> cars)
    {
        var rows = cars
            .Select(c => new
            {
                Car = c,
                Key = CarCatalogImageLibrary.BuildLookupKey(c.CatalogBrand, c.Brand, c.CatalogModelName, c.Model)
            })
            .Where(x => !string.IsNullOrEmpty(x.Key))
            .GroupBy(x => x.Key!, StringComparer.Ordinal)
            .Select(g =>
            {
                var sample = g.First().Car;
                var remote = CarCatalogImageLibrary.TryGetRemoteUrls(g.Key);
                return new CarBrandModelExportRow
                {
                    NormalizedKey = g.Key,
                    CatalogBrand = sample.CatalogBrand,
                    Brand = sample.Brand,
                    CatalogModelName = sample.CatalogModelName,
                    Model = sample.Model,
                    CarCount = g.Count(),
                    HasCatalogImage = remote is not null && remote.Count > 0,
                    RemoteUrlCount = remote?.Count ?? 0
                };
            })
            .OrderBy(r => r.NormalizedKey)
            .ToList();

        return rows;
    }

    private static bool HasLocalImageReference(List<string>? urls)
    {
        if (urls is null || urls.Count == 0)
            return false;

        return urls.Any(u =>
            !string.IsNullOrWhiteSpace(u) &&
            !u.TrimStart().StartsWith("http", StringComparison.OrdinalIgnoreCase));
    }

    private static string ShortHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes.AsSpan(0, 6)).ToLowerInvariant();
    }

    private static async Task<string> ResolveExtensionAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        var path = new Uri(url).AbsolutePath;
        var ext = Path.GetExtension(path);
        if (ext is ".jpg" or ".jpeg" or ".png" or ".webp")
            return ext;

        if (url.Contains("wikimedia.org", StringComparison.OrdinalIgnoreCase))
            return ".jpg";

        using var head = new HttpRequestMessage(HttpMethod.Head, url);
        try
        {
            using var headResp = await client.SendAsync(head, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (headResp.Content.Headers.ContentType?.MediaType is { } mt)
            {
                if (mt.Contains("jpeg", StringComparison.OrdinalIgnoreCase)) return ".jpg";
                if (mt.Contains("png", StringComparison.OrdinalIgnoreCase)) return ".png";
                if (mt.Contains("webp", StringComparison.OrdinalIgnoreCase)) return ".webp";
            }
        }
        catch
        {
            // GET ile devam
        }

        return ".jpg";
    }

    private static async Task DownloadToFileAsync(HttpClient client, string url, string fullPath, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode == (HttpStatusCode)429 && attempt < 3)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(25 + attempt * 10);
                await Task.Delay(retryAfter, cancellationToken);
                continue;
            }

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(file, cancellationToken);
            return;
        }
    }
}
