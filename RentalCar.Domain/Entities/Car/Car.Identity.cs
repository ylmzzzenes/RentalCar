using System.ComponentModel.DataAnnotations;
using RentalCar.Domain.Common;

namespace RentalCar.Domain.Entities;

public partial class Car : BaseEntity
{
    [Key]
    public int Id { get; set; }
}
