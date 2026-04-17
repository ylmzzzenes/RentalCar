using System.ComponentModel.DataAnnotations;

namespace RentalCar.Domain.Enums
{
    [Flags]
    public enum Gear
    {
        [Display(Name = "Seçiniz")]
        None = 0,

        [Display(Name = "Otomatik")]
        Otomatik = 1,

        [Display(Name = "Manuel")]
        Manuel = 2,

        [Display(Name = "Yarı otomatik")]
        YarıOtomatik = 4
    }
}
