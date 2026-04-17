using System.ComponentModel.DataAnnotations;

namespace RentalCar.Domain.Enums;

/// <summary>İkinci el ilanlarında araç genel durumu.</summary>
public enum VehicleCondition
{
    [Display(Name = "Belirtilmedi")]
    Belirtilmedi = 0,

    [Display(Name = "Sıfır")]
    Sifir = 1,

    [Display(Name = "İkinci el")]
    IkinciEl = 2,

    [Display(Name = "Hasar kayıtlı")]
    HasarKayitli = 3
}
