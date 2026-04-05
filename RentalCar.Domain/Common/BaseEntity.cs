namespace RentalCar.Domain.Common;

public abstract class BaseEntity : IAuditableEntity
{
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}

public interface IAuditableEntity
{
    DateTime CreatedOn { get; set; }
    DateTime ModifiedOn { get; set; }
}
