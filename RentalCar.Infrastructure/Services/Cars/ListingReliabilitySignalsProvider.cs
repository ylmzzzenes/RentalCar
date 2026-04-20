using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Application.Contracts.Cars;
using RentalCar.Domain.Entities;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.Infrastructure.Services.Cars;

public sealed class ListingReliabilitySignalsProvider(
    RentalCarContext db,
    ILogger<ListingReliabilitySignalsProvider> logger) : IListingReliabilitySignalsProvider
{
    private sealed record CarSnap(
        int Id,
        string? CatalogBrand,
        string? Brand,
        string? CatalogModelName,
        string? Model,
        int? ModelYear,
        decimal? ListedPrice,
        string? PostedByUserId);

    public async Task<ListingReliabilitySignals> GetSignalsAsync(Car car, CancellationToken cancellationToken = default)
    {
        if (car.Id == 0)
            return await BuildForTransientCarAsync(car, cancellationToken);

        var map = await GetSignalsForCarsAsync(new[] { car }, cancellationToken);
        return map.TryGetValue(car.Id, out var s)
            ? s
            : new ListingReliabilitySignals { CarId = car.Id };
    }

    public async Task<IReadOnlyDictionary<int, ListingReliabilitySignals>> GetSignalsForCarsAsync(
        IReadOnlyList<Car> cars,
        CancellationToken cancellationToken = default)
    {
        if (cars.Count == 0)
            return new Dictionary<int, ListingReliabilitySignals>();

        var sellerIds = cars
            .Where(c => !string.IsNullOrWhiteSpace(c.PostedByUserId))
            .Select(c => c.PostedByUserId!)
            .Distinct()
            .ToList();

        var sellerCounts = sellerIds.Count == 0
            ? new Dictionary<string, int>()
            : await db.Cars.AsNoTracking()
                .Where(c => c.IsApproved && c.PostedByUserId != null && sellerIds.Contains(c.PostedByUserId))
                .GroupBy(c => c.PostedByUserId!)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        Dictionary<string, (double Avg, int N)> sellerRatings = new();
        if (sellerIds.Count > 0)
        {
            sellerRatings = await (
                from r in db.CarRatings.AsNoTracking()
                join c in db.Cars.AsNoTracking() on r.CarId equals c.Id
                where c.PostedByUserId != null && sellerIds.Contains(c.PostedByUserId)
                group r by c.PostedByUserId
                into g
                select new
                {
                    UserId = g.Key!,
                    Avg = g.Average(x => (double)x.Score),
                    N = g.Count()
                }).ToDictionaryAsync(x => x.UserId, x => (x.Avg, x.N), cancellationToken);
        }

        var snapshot = await db.Cars.AsNoTracking()
            .Where(c => c.IsApproved)
            .Select(c => new CarSnap(
                c.Id,
                c.CatalogBrand,
                c.Brand,
                c.CatalogModelName,
                c.Model,
                c.ModelYear,
                c.ListedPrice,
                c.PostedByUserId))
            .ToListAsync(cancellationToken);

        logger.LogDebug("ListingReliabilitySignals snapshot rows={Count}", snapshot.Count);

        var result = new Dictionary<int, ListingReliabilitySignals>();
        foreach (var car in cars)
        {
            if (car.Id == 0)
            {
                result[0] = await BuildForTransientCarAsync(car, cancellationToken);
                continue;
            }

            var sim = CountSimilar(snapshot, car);
            var sellerId = car.PostedByUserId;
            var sellerN = 0;
            if (!string.IsNullOrEmpty(sellerId))
            {
                if (!sellerCounts.TryGetValue(sellerId, out sellerN))
                    sellerN = 1;
            }

            sellerRatings.TryGetValue(sellerId ?? "", out var rt);

            var peer = PeerAverageExcludingCar(car, snapshot);

            result[car.Id] = new ListingReliabilitySignals
            {
                CarId = car.Id,
                SimilarListingCount = sim,
                SellerListingCount = sellerN,
                SellerAverageCarRating = rt.N > 0 ? rt.Avg : null,
                SellerRatingSampleSize = rt.N,
                PeerAverageListedPrice = peer.Avg,
                PeerPriceSampleSize = peer.N
            };
        }

        return result;
    }

    private async Task<ListingReliabilitySignals> BuildForTransientCarAsync(Car car, CancellationToken cancellationToken)
    {
        var sellerId = car.PostedByUserId;
        var sellerN = 0;
        double? avg = null;
        var nRt = 0;

        if (!string.IsNullOrEmpty(sellerId))
        {
            sellerN = await db.Cars.AsNoTracking()
                .CountAsync(c => c.IsApproved && c.PostedByUserId == sellerId, cancellationToken);
            if (sellerN == 0)
                sellerN = 1;

            var carIds = await db.Cars.AsNoTracking()
                .Where(c => c.PostedByUserId == sellerId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            if (carIds.Count > 0)
            {
                nRt = await db.CarRatings.AsNoTracking()
                    .CountAsync(r => carIds.Contains(r.CarId), cancellationToken);
                if (nRt > 0)
                    avg = await db.CarRatings.AsNoTracking()
                        .Where(r => carIds.Contains(r.CarId))
                        .AverageAsync(r => (double)r.Score, cancellationToken);
            }
        }

        return new ListingReliabilitySignals
        {
            CarId = 0,
            SimilarListingCount = 0,
            SellerListingCount = sellerN,
            SellerAverageCarRating = avg,
            SellerRatingSampleSize = nRt,
            PeerAverageListedPrice = null,
            PeerPriceSampleSize = 0
        };
    }

    private static (decimal? Avg, int N) PeerAverageExcludingCar(Car car, List<CarSnap> snapshot)
    {
        var bk = BKey(car);
        var mk = MKey(car);
        if (string.IsNullOrEmpty(bk) || string.IsNullOrEmpty(mk) || !car.ModelYear.HasValue || car.ListedPrice is not > 0)
            return (null, 0);

        var peers = snapshot
            .Where(s => s.Id != car.Id)
            .Where(s => BKey(s) == bk && MKey(s) == mk && s.ModelYear == car.ModelYear)
            .Where(s => s.ListedPrice is > 0)
            .Select(s => s.ListedPrice!.Value)
            .ToList();

        if (peers.Count == 0)
            return (null, 0);

        return (peers.Average(x => x), peers.Count);
    }

    private static int CountSimilar(List<CarSnap> all, Car car)
    {
        var id = car.Id;
        var bk = BKey(car);
        var mk = MKey(car);
        if (string.IsNullOrEmpty(bk) || string.IsNullOrEmpty(mk) || !car.ModelYear.HasValue || car.ListedPrice is not > 0)
            return 0;

        var y = car.ModelYear.Value;
        var p = car.ListedPrice.Value;
        var n = 0;

        foreach (var s in all)
        {
            if (s.Id == id) continue;
            if (BKey(s) != bk || MKey(s) != mk || s.ModelYear != y) continue;
            if (s.ListedPrice is not > 0) continue;
            var sp = s.ListedPrice.Value;
            if (p == 0) continue;
            var diff = Math.Abs(sp - p) / p;
            if (diff <= 0.12m)
                n++;
        }

        return n;
    }

    private static string BKey(Car c) => (c.CatalogBrand ?? c.Brand ?? "").Trim().ToLowerInvariant();

    private static string MKey(Car c) => (c.CatalogModelName ?? c.Model ?? "").Trim().ToLowerInvariant();

    private static string BKey(CarSnap s) => (s.CatalogBrand ?? s.Brand ?? "").Trim().ToLowerInvariant();

    private static string MKey(CarSnap s) => (s.CatalogModelName ?? s.Model ?? "").Trim().ToLowerInvariant();
}
