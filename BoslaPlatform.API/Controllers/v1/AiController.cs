using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Specialists.DTOs;
using BoslaPlatform.Service.Features.AI.Requests;
using BoslaPlatform.Service.Features.AI.Responses;
using BoslaPlatform.Application.Interfaces.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BoslaPlatform.API.Controllers.V1;

[ApiController]
[Route("api/v1/ai")]
public class AiController : ControllerBase
{
    private readonly IAiSearchService _ai;
    private readonly IAiRecommendationService _recommendations;
    private readonly ISummaryService _summaryService;
    private readonly IChatBotService _chatBot;

    public AiController(IAiSearchService ai, IAiRecommendationService recommendations, ISummaryService summaryService, IChatBotService chatBot)
    {
        _ai = ai;
        _recommendations = recommendations;
        _summaryService = summaryService;
        _chatBot = chatBot;
    }

    [HttpPost("search")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<AiSearchResponse>), 200)]
    public async Task<IActionResult> Search([FromBody] SearchRequest req)
    {
        var res = await _ai.SearchAsync(req);
        return Ok(ApiResponse<AiSearchResponse>.SuccessResponse(res));
    }

    [HttpGet("search/history")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<List<SearchHistoryItemDto>>), 200)]
    public async Task<IActionResult> GetHistory()
    {
        var hist = await _ai.GetHistoryAsync();
        return Ok(ApiResponse<List<SearchHistoryItemDto>>.SuccessResponse(hist));
    }

    [HttpPost("search/{id}/feedback")]
    [Produces("application/json")]
    public async Task<IActionResult> RecordFeedback([FromRoute] Guid id, [FromBody] FeedbackRequest req)
    {
        await _ai.RecordFeedbackAsync(id, req);
        return NoContent();
    }

    [HttpGet("recommendations")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<List<SpecialistListItemResponse>>), 200)]
    public async Task<IActionResult> GetRecommendations([FromQuery] int topK = 6, CancellationToken ct = default)
    {
        var result = await _recommendations.GetRecommendationsAsync(topK, ct);
        return Ok(ApiResponse<List<SpecialistListItemResponse>>.SuccessResponse(result));
    }

    [HttpPost("summaries/{id:guid}/regenerate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegenerateSummary([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _summaryService.RegenerateAsync(id, ct);
        if (result.IsError) return Problem(statusCode: 500, title: "Failed to regenerate summary");
        return NoContent();
    }

    [HttpPost("chat")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<ChatResponse>), 200)]
    public async Task<IActionResult> Chat([FromBody] ChatRequest req, CancellationToken ct)
    {
        var reply = await _chatBot.ChatAsync(req, ct);
        return Ok(ApiResponse<ChatResponse>.SuccessResponse(reply));
    }

    [HttpPost("smart-replies")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<SmartRepliesResponse>), 200)]
    public async Task<IActionResult> GetSmartReplies([FromBody] SmartRepliesRequest req, CancellationToken ct)
    {
        var replies = await _chatBot.GetSmartRepliesAsync(req.ConversationId, ct);
        return Ok(ApiResponse<SmartRepliesResponse>.SuccessResponse(replies));
    }
}
