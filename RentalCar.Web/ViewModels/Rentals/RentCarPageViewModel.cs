namespace RentalCar.Web.ViewModels.Rentals
{
    public class RentCarPageViewModel
    {
        public CarRentalSummaryViewModel Car { get; set; } = new();
        public RentalFormViewModel Form { get; set; } = new();
    }
}
