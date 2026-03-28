using RentalCar.Application.AI.Models;

namespace RentalCar.Application.Abstractions.AI
{
    public interface IToolLayerService
    {
        string BuildToolDefinitionJson();
        Task<ToolExecutionResult> ExecuteAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default);
    }
}
