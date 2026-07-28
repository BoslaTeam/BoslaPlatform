using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Observability;
using BoslaPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoslaPlatform.API.Controllers.v1
{
    /// <summary>
    /// Read-only diagnostics for the recording pipeline. Reconstructs a full
    /// lifecycle timeline for a recording from the persisted event store, so an
    /// operator can answer "where did this recording stop?" without grepping logs
    /// or standing up a trace backend.
    /// </summary>
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/diagnostics/recordings")]
    [ApiController]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public class RecordingDiagnosticsController : ControllerBase
    {
        private readonly IRecordingDiagnosticsService _diagnostics;

        public RecordingDiagnosticsController(IRecordingDiagnosticsService diagnostics)
        {
            _diagnostics = diagnostics;
        }

        /// <summary>
        /// Returns the ordered lifecycle timeline for a recording correlation id.
        /// Accepts the canonical id ("rec-{recordingId}"), a bare recording or
        /// session guid, or a channel name.
        /// </summary>
        [HttpGet("{correlationId}/timeline")]
        [ProducesResponseType(typeof(ApiResponse<RecordingTimeline>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [EndpointName("GetRecordingTimeline")]
        [EndpointSummary("Reconstructs a recording's lifecycle timeline.")]
        [EndpointDescription("Returns every pipeline stage (Acquire → Start → Webhook → Stop → Metadata → Playback) for a recording, with a verdict indicating the furthest stage reached or the stage that failed.")]
        [Tags("Diagnostics")]
        public async Task<IResult> GetTimeline(string correlationId, CancellationToken ct)
        {
            var result = await _diagnostics.GetTimelineAsync(correlationId, ct);

            return result.Match(
                timeline => Results.Ok(
                    ApiResponse<RecordingTimeline>.SuccessResponse(
                        timeline, "Recording timeline reconstructed.")),
                errors => errors.ToProblem());
        }
    }
}
