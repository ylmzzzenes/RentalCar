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
        Diesel=1,
        Electric=2,
        Hybrid=4,
        LPG=8,
        CNG=16
    }
}
