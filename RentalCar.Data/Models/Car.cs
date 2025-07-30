using RentalCar.Data.Enums;
using System.ComponentModel.DataAnnotations;


namespace RentalCar.Data.Models
{
    public class Car:EntityModel
    {
        [Key]
        public int Id { get;set; }
        
        [Required, StringLength(100)]
        public string Vin { get; set;}

        [Required, StringLength(100)]
        public string Brand { get; set; }
        
        [Required, StringLength(100)]
        public string Model { get; set; }
        public DateOnly Year { get; set; }
        public FuelType FuelType { get; set; }
        public Gear Gear { get; set; }
        public BodyType BodyType { get; set; }
       
        [Required, StringLength(100)]
        public string Colour { get; set; }
        public string Plate { get; set; }
        public Security Security { get; set; }

        public InternalEquipment InternalEquipment { get; set; }
        public ExternalEquipment ExternalEquipment { get; set; }

        public string ImageUrls { get; set; }
        public decimal DailyPrice { get; set; }
        public decimal WeeklyPrice { get; set; }
        public decimal MonthlyPrice { get; set; }

     

    }
}
