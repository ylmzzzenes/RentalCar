using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentalCar.Data.Models
{
    public abstract class EntityModel:IEntityModel
    {
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    public interface IEntityModel
    {
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }

    }
}
