using System.ComponentModel.DataAnnotations;

namespace RentalCar.Data.Models.Requests
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Adınızı giriniz.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyadınızı giriniz.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "E-posta giriniz.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre giriniz.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Şifre tekrarı giriniz.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifreler uyuşmuyor.")]
        public string ConfirmPassword { get; set; }

        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public string? PhoneNumber2 { get; set; }
    }
}
