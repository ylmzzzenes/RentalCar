namespace RentalCar.ViewModels.Admin
{
    public class AdminCarItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public int? Year { get; set; }
        public string? City { get; set; }
        public bool IsApproved { get; set; }
    }
}
