using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Data.Enums
{
    [Flags]
    public enum ExternalEquipment
    {
        None=0,
        Sunroof=1,
        PanaromicGlassRoof=2,
        ParkSensor=4
    }
}
