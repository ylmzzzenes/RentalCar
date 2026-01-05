using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Data.Enums
{
    [Flags]
    public enum Gear
    {
        None=0,
        Otomatik=1,
        Manuel=2,
        YarıOtomatik = 4
    }
}
