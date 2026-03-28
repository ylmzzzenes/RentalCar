using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Data.Enums
{
    [Flags]
    public enum FuelType
    {
        None=0,
        Dizel=1,
        Elektrik=2,
        Hibrit=4,
        Benzin=8,
        
    }
}
