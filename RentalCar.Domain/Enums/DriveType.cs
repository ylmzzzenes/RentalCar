using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Data.Enums
{
    [Flags]
    public enum DriveType
    {
        None = 0,
        FWD = 1,
        RWD = 2,
        AWD = 4,
        FourByFour = 8
    }
}
