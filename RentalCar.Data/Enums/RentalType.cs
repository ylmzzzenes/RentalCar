using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Data.Enums
{
    [Flags]
    public enum RentalType
    {
        None=0,
        Daily = 1,
        Weekly = 2,
        Monthly = 4,
        LongTerm = 8
    }
}
