using System.ComponentModel.DataAnnotations;
using RentalCar.Domain.Common;
using RentalCar.Domain.Enums;
using RentalCar.Domain.Rules;

namespace RentalCar.Domain.Entities;

public class Rental : BaseEntity
{
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public AppUser? User { get; set; }

    public int CarId { get; set; }
    public Car Car { get; set; } = null!;

    public RentalType RentalType { get; set; }

    public decimal Duration { get; set; }

    public decimal TotalPrice { get; set; }

    /// <summary>Kiralama başlangıcı (UTC, genelde gün başı).</summary>
    public DateTime StartDate { get; set; }

    public RentalStatus Status { get; set; } = RentalStatus.PendingConfirmation;

    /// <summary>Süre ve kiralama tipine göre hesaplanan bitiş (UTC).</summary>
    public DateTime EndDate => RentalDateRules.ComputeEndUtc(StartDate, RentalType, Duration);
}
