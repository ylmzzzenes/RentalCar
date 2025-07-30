using RentalCar.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace RentalCar.Data.Models
{
    public class Rental
    {
        public int Id { get; set; }

        public int CarId { get; set; }
        public Car Car { get; set; }

        public RentalType RentalType { get; set; }

      
        public decimal Duration { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime StartDate { get; set; }


        public DateTime EndDate => StartDate.AddDays((double)CalculateTotalDays);

        private decimal  CalculateTotalDays
        {
            get
            {
                switch (RentalType)
                {
                    case RentalType.Daily:
                        return Duration;
                    case RentalType.Weekly:
                        return Duration * 7;
                    case RentalType.Monthly:
                        return Duration * 30;
                    case RentalType.LongTerm:
                        return Duration * 365;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(RentalType), "Invalid rental type");
                }
            }
        }
    }
}
