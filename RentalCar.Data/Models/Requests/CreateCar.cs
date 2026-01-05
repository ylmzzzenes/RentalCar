using Microsoft.AspNetCore.Http;
using RentalCar.Data.Enums;
using DriveType = RentalCar.Data.Enums.DriveType;
namespace RentalCar.Data.Models.Requests
{
    public class CreateCar
    {
        public int Id { get; set; }
        public string? Vin { get; set; }

        public string? Brand { get; set; }

        public string? Plate { get; set; }
        public string? model { get; set; }
        public DateTime? yil { get; set; }
        public FuelType yakitTuru { get; set; }
        public Gear vites { get; set; }
        public string? renk { get; set; }
        public string? modelraw { get; set; }
        public string? marka { get; set; }
        public string? model_adi { get; set; }
        public string? paket { get; set; }
        public string? motor_kodu { get; set; }
        public DriveType cekis { get; set; }
        public string? sanziman_kodu { get; set; }
        public int? kilometre { get; set; }
        public int? vergi { get; set; }
        public double? lt_100km { get; set; }
        public double? motorHacmi { get; set; }
        public decimal? fiyat { get; set; }

        public string? sehir { get; set; }
        public string? kasaTipi { get; set; }
        public string? donanimSeviyesi { get; set; }
        public int? hasarKaydi { get; set; }       
        public string? degisenBoyanan { get; set; }
        public int? servisGecmisi { get; set; }      

        public int? motorGuc_hp { get; set; }
        public int? tork_nm { get; set; }
        public int? sahipSayisi { get; set; }

        public List<IFormFile>? ImageUrls { get; set; }
    }
}
