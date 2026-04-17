using System.ComponentModel.DataAnnotations.Schema;

namespace RentalCar.Domain.Entities;

/// <summary>İlanı oluşturan kullanıcı (opsiyonel; toplu içe aktarımda boş olabilir).</summary>
public partial class Car
{
    public string? PostedByUserId { get; set; }

    [ForeignKey(nameof(PostedByUserId))]
    public AppUser? PostedBy { get; set; }
}
