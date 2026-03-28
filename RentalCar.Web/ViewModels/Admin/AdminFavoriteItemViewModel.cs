namespace RentalCar.ViewModels.Admin
{
    public class AdminFavoriteItemViewModel
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public string CarTitle { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
