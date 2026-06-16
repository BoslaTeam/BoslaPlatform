using BoslaPlatform.Service.Features.AI.Requests;
using BoslaPlatform.Service.Features.AI.Responses;
using BoslaPlatform.Application.Interfaces.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BoslaPlatform.API.Controllers.V1;

[ApiController]
[Route("api/v1/ai")]
[Authorize]
/// <summary>
/// AI endpoints (Smart Search, RAG)
/// </summary>
public class AiController : ControllerBase
{
    private readonly IAiSearchService _ai;

    public AiController(IAiSearchService ai)
    {
        _ai = ai;
    }

    /// <summary>
    /// Run a smart search query (retrieval-augmented generation).
    /// </summary>
    /// <param name="req">Search request with query and topK</param>
    [HttpPost("search")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(BoslaPlatform.Service.Features.AI.Responses.AiSearchResponse), 200)]
    public async Task<IActionResult> Search([FromBody] BoslaPlatform.Service.Features.AI.Requests.SearchRequest req)
    {
        var res = await _ai.SearchAsync(req);
        return Ok(res);
    }

    [HttpGet("search/history")]
    [Produces("application/json")]
    public async Task<IActionResult> GetHistory()
    {
        var hist = await _ai.GetHistoryAsync();
        return Ok(hist);
    }

    [HttpPost("search/{id}/feedback")]
    [Produces("application/json")]
    public async Task<IActionResult> RecordFeedback([FromRoute] Guid id, [FromBody] BoslaPlatform.Service.Features.AI.Requests.FeedbackRequest req)
    {
        await _ai.RecordFeedbackAsync(id, req);
        return NoContent();
    }
}
