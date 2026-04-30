using RentalCar.Application.Abstractions.Services.Purchases;
using RentalCar.Application.Dtos.Purchases;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;

namespace RentalCar.Infrastructure.Services.Purchases;

public sealed class PurchaseAppService : IPurchaseAppService
{
    private readonly PurchaseServices _purchaseServices;

    public PurchaseAppService(PurchaseServices purchaseServices)
    {
        _purchaseServices = purchaseServices;
    }

    public async Task<PurchasePageDto?> GetPurchasePageAsync(int carId, CancellationToken cancellationToken = default)
    {
        var car = await _purchaseServices.GetCarByIdAsync(carId, cancellationToken);
        if (car == null || !car.IsApproved)
            return null;

        return new PurchasePageDto
        {
            CarId = car.Id,
            Brand = car.CatalogBrand ?? car.Brand ?? string.Empty,
            Model = car.Model ?? string.Empty,
            ModelYear = car.ModelYear,
            ListedPrice = car.ListedPrice,
            ImageUrls = car.ImageUrls?.ToList() ?? new List<string>()
        };
    }

    public async Task<PurchaseResultDto> CreatePurchaseAsync(int carId, string userId, decimal agreedPrice, string? buyerNote, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new PurchaseResultDto { Success = false, ErrorMessage = "Oturum gerekli." };

        if (agreedPrice <= 0)
            return new PurchaseResultDto { Success = false, ErrorMessage = "Geçerli bir fiyat giriniz." };

        var car = await _purchaseServices.GetCarByIdAsync(carId, cancellationToken);
        if (car == null || !car.IsApproved)
            return new PurchaseResultDto { Success = false, ErrorMessage = "Araç bulunamadı veya onaylı değil." };

        var now = DateTime.UtcNow;
        var purchase = new Purchase
        {
            UserId = userId,
            CarId = carId,
            AgreedPrice = Math.Round(agreedPrice, 2),
            Status = PurchaseStatus.Pending,
            BuyerNote = NormalizeNote(buyerNote),
            CreatedOn = now,
            ModifiedOn = now
        };

        await _purchaseServices.CreateAsync(purchase, cancellationToken);

        return new PurchaseResultDto { Success = true, PurchaseId = purchase.Id };
    }

    private static string? NormalizeNote(string? buyerNote)
    {
        if (string.IsNullOrWhiteSpace(buyerNote)) return null;
        var n = buyerNote.Trim();
        return n.Length <= 2000 ? n : n[..2000];
    }
}
