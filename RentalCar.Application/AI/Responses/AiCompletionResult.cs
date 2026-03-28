using RentalCar.Application.AI.Models;

namespace RentalCar.Application.AI.Responses
{
    public class AiCompletionResult
    {
        public string Message { get; set; } = string.Empty;
        public string Intent { get; set; } = "fallback";
        public List<ChatCarCard> Cars { get; set; } = new();
        public ChatFilterSuggestion? SuggestedFilters { get; set; }
    }
}
