using System.ComponentModel.DataAnnotations;

namespace RentalCar.Domain.Enums
{
    [Flags]
    public enum DriveType
    {
        [Display(Name = "Seçiniz")]
        None = 0,

        [Display(Name = "Önden çekiş (FWD)")]
        FWD = 1,

        [Display(Name = "Arkadan çekiş (RWD)")]
        RWD = 2,

        [Display(Name = "Sürekli dört çeker (AWD)")]
        AWD = 4,

        [Display(Name = "Seçmeli dört çeker (4x4)")]
        FourByFour = 8
    }
}
