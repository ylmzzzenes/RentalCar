using System.ComponentModel.DataAnnotations;
using RentalCar.Domain.Common;
using RentalCar.Domain.Enums;

namespace RentalCar.Domain.Entities;

public class Purchase : BaseEntity
{
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public AppUser? User { get; set; }

    public int CarId { get; set; }
    public Car Car { get; set; } = null!;

    /// <summary>Teklif / kabul edilen satın alma bedeli (TL).</summary>
    public decimal AgreedPrice { get; set; }

    public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;

    [MaxLength(2000)]
    public string? BuyerNote { get; set; }
}
