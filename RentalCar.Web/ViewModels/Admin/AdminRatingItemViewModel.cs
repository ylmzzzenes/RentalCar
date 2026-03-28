namespace RentalCar.ViewModels.Admin
{
    public class AdminRatingItemViewModel
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public string CarTitle { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
