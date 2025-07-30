using Microsoft.AspNetCore.Http;
using RentalCar.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Data.Models.Requests
{
    public class CreateCar
    {
        public int Id { get; set; }
        public string Vin { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public DateOnly Year { get; set; }
        public FuelType FuelType { get; set; }
        public Gear Gear { get; set; }  
        public BodyType BodyType { get; set; }  
        public string Colour { get; set; }    
        public string Plate { get; set; }   
        public Security Security { get; set; }
        public ExternalEquipment ExternalEquipment { get; set; }
        public InternalEquipment InternalEquipment { get; set; }

        public List<IFormFile> ImageUrls { get; set; }
    }
}
