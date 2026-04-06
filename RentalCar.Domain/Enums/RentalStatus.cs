namespace RentalCar.Domain.Enums;

/// <summary>Kiralama kaydının iş akışı durumu.</summary>
public enum RentalStatus
{
    Draft = 0,
    PendingConfirmation = 1,
    Confirmed = 2,
    Active = 3,
    Completed = 4,
    Cancelled = 5
}
