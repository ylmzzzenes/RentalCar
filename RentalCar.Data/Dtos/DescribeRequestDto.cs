using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Data.Dtos
{
    public class DescribeRequestDto
    {

        public Dictionary<string, object?> data { get; set; } = new();
        public decimal? predicted_mid { get; set; }
        public decimal? predicted_low { get; set; }
        public decimal? predicted_high { get; set; }
    }
}
