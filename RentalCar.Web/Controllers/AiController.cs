using Microsoft.AspNetCore.Mvc;
using RentalCar.Application.Abstractions.AI;

namespace RentalCar.Web.Controllers;

[ApiController]
[Route("ai")]
public class AiController : ControllerBase
{
    private readonly IAIService _aiService;

    public AiController(IAIService aiService)
    {
        _aiService = aiService;
    }

    public sealed class AskRequest
    {
        public string Question { get; set; } = string.Empty;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "Soru bos olamaz." });

        var sessionId = HttpContext.Session.GetString("ai-ask-session") ?? Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString("ai-ask-session", sessionId);

        var response = await _aiService.ReplyAsync(request.Question.Trim(), sessionId, cancellationToken);
        return Ok(new
        {
            answer = response.Message,
            intent = response.Intent
        });
    }
}
