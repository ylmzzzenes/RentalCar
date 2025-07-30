using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Data.Enums
{
    [Flags]
    public enum Security
    {
        None = 0,
        ABS = 1,
        AEB = 2,
        Immobilizier = 4,
        LaneKeeping = 8,
        NightVision = 16,
        AirBag = 32,
        Distronic = 64,
        CentralLocking = 128,
        Isofix = 256
    }
}
