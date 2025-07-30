using System.ComponentModel.DataAnnotations;

namespace RentalCar.Data.Models.Requests
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta giriniz.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre giriniz.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }
    }
}
