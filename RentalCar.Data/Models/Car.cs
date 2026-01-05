using RentalCar.Data.Enums;
using System.ComponentModel.DataAnnotations;
using DriveType = RentalCar.Data.Enums.DriveType;


namespace RentalCar.Data.Models
{
    public class Car:EntityModel
    {
        [Key]
        public int Id { get;set; }
    
        public string? Vin { get; set;}

        public string? Brand { get; set; }
        
        public string? model { get; set; }
        public DateTime? yil { get; set; }
        public FuelType yakitTuru { get; set; }
        public Gear vites { get; set; }
        public BodyType BodyType { get; set; }
       
        
        public string? renk { get; set; }
        public string? Plate { get; set; }
        public Security Security { get; set; }

        public InternalEquipment InternalEquipment { get; set; }
        public ExternalEquipment ExternalEquipment { get; set; }

        public string? ImageUrls { get; set; }
        public decimal DailyPrice { get; set; }
        public decimal WeeklyPrice { get; set; }
        public decimal MonthlyPrice { get; set; }
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
        public int? hasarKaydi { get; set; }          // 0/1
        public string? degisenBoyanan { get; set; }
        public int? servisGecmisi { get; set; }       // 0/1

        public int? motorGuc_hp { get; set; }
        public int? tork_nm { get; set; }
        public int? sahipSayisi { get; set; }

        public decimal? fiyat_tahmin { get; set; }  // mid
        public decimal? fiyat_min { get; set; }     // low
        public decimal? fiyat_max { get; set; }     // high

        public string? aciklama_kisa { get; set; }
        public string? aciklama { get; set; }






    }
}
