using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Interfaces.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoslaPlatform.API.Controllers.V1;

[ApiController]
[Route("api/v1/ai/specialist")]
[Authorize(Roles = "Specialist")]
public class AiSpecialistController : ControllerBase
{
    private readonly ISpecialistAiService _specialistAi;

    public AiSpecialistController(ISpecialistAiService specialistAi)
    {
        _specialistAi = specialistAi;
    }

    [HttpPost("smart-replies")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<SmartRepliesResponse>), 200)]
    public async Task<IActionResult> GetSmartReplies([FromBody] SmartRepliesRequest req, CancellationToken ct)
    {
        var result = await _specialistAi.GetSmartRepliesAsync(req.ConversationId, ct);
        if (result.IsError)
            return Problem(statusCode: 400, title: result.Errors[0].Description);
        return Ok(ApiResponse<SmartRepliesResponse>.SuccessResponse(result.Value));
    }

    [HttpGet("session-prep/{appointmentId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<SessionPrepDto>), 200)]
    public async Task<IActionResult> GetSessionPrep([FromRoute] Guid appointmentId, CancellationToken ct)
    {
        var result = await _specialistAi.GetSessionPrepAsync(appointmentId, ct);
        if (result.IsError)
            return Problem(statusCode: 400, title: result.Errors[0].Description);
        return Ok(ApiResponse<SessionPrepDto>.SuccessResponse(result.Value));
    }

    [HttpGet("dashboard-insights")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<DashboardInsightsDto>), 200)]
    public async Task<IActionResult> GetDashboardInsights(CancellationToken ct)
    {
        var result = await _specialistAi.GetDashboardInsightsAsync(ct);
        if (result.IsError)
            return Problem(statusCode: 400, title: result.Errors[0].Description);
        return Ok(ApiResponse<DashboardInsightsDto>.SuccessResponse(result.Value));
    }
}

public class SmartRepliesRequest
{
    public Guid ConversationId { get; set; }
}
