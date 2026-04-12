namespace RentalCar.Core.Exceptions;

public class ExceptionResponseModel
{
    public bool Success { get; set; } = false;

    public string Message { get; set; } = "Bir hata oluştu.";

    public List<string> Errors { get; set; } = new();

    public List<ValidationErrorDetail>? ValidationErrors { get; set; }
}