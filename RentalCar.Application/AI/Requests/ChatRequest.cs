namespace RentalCar.Application.AI.Requests
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }
}
