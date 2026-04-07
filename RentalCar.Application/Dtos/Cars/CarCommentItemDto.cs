namespace RentalCar.Application.Dtos.Cars
{
    public sealed class  CarCommentItemDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
    }
}
