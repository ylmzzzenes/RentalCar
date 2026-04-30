using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalCar.Application.Abstractions.Services.Purchases;
using System.Security.Claims;

namespace RentalCar.Controllers;

public class PurchaseController : Controller
{
    private readonly IPurchaseAppService _purchaseAppService;

    public PurchaseController(IPurchaseAppService purchaseAppService)
    {
        _purchaseAppService = purchaseAppService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Confirm(int carId, CancellationToken cancellationToken = default)
    {
        var page = await _purchaseAppService.GetPurchasePageAsync(carId, cancellationToken);
        if (page == null)
            return NotFound();

        return View(page);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int carId, decimal agreedPrice, string? buyerNote, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var result = await _purchaseAppService.CreatePurchaseAsync(carId, userId, agreedPrice, buyerNote, cancellationToken);
        if (!result.Success)
        {
            var page = await _purchaseAppService.GetPurchasePageAsync(carId, cancellationToken);
            if (page == null)
                return NotFound();
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "İşlem tamamlanamadı.");
            return View(page);
        }

        return RedirectToAction(nameof(Complete), new { id = result.PurchaseId });
    }

    [HttpGet]
    [Authorize]
    public IActionResult Complete(int id) => View(id);
}
