namespace RentalCar.Core.Exceptions;

public class ValidationErrorDetail
{
    public string PropertyName { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;
}