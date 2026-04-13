namespace RentalCar.Web.ViewModels.Error;

public class ErrorViewModel
{
    public string Message { get; set; } = "Bir hata oluştu.";

    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
}