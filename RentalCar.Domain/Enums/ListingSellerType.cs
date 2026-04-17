using System.ComponentModel.DataAnnotations;

namespace RentalCar.Domain.Enums;

/// <summary>İlanı veren taraf (sahibinden / galeri vb.).</summary>
public enum ListingSellerType
{
    [Display(Name = "Belirtilmedi")]
    Belirtilmedi = 0,

    [Display(Name = "Sahibinden")]
    Sahibinden = 1,

    [Display(Name = "Galeri")]
    Galeri = 2,

    [Display(Name = "Yetkili bayi")]
    YetkiliBayi = 3
}
