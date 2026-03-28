namespace RentalCar.Application.AI.Models
{
    public class ToolExecutionResult
    {
        public string ToolName { get; set; } = string.Empty;
        public string ResultJson { get; set; } = "{}";
        public List<ChatCarCard> Cars { get; set; } = new();
        public ChatFilterSuggestion? SuggestedFilters { get; set; }
    }
}
