using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Data.Enums
{
    [Flags]
    public enum InternalEquipment
    {
        None=0,
        StartStop=1,
        Climate=2,
        BackCamera=4
    }
}
