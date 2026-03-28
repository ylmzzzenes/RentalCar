using RentalCar.Application.AI.Models;
using System.Text.Json;
namespace RentalCar.Application.AI.Responses
{
    public class ChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Intent { get; set; } = "fallback";
        public List<ChatCarCard> Cars { get; set; } = new();
        public ChatFilterSuggestion? SuggestedFilters { get; set; }
        public bool CanAutoApplyFilters => SuggestedFilters is not null;
    }
}
