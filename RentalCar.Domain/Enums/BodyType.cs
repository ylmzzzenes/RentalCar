using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Data.Enums
{
    [Flags]
    public enum BodyType
    {
        None=0,
        Sedan=1,
        Coupe=2,
        SportsCar=4,
        StationWagon=8,
        Hatchback=16,
        Suv=32,
        Minivan=64,
        PıckUp=128
    }
}
