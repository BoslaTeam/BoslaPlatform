using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.RecordingAccess.Dtos;
using BoslaPlatform.Application.Features.VideoSessions.Dtos;
using BoslaPlatform.Application.Features.VideoSessions.Interfaces;
using BoslaPlatform.Application.Features.VideoSessions.Requests;
using BoslaPlatform.Application.Features.VideoSessions.Responses;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Application.Interfaces.Video;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoslaPlatform.API.Controllers.v1
{
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/video-sessions")]
    [ApiController]
    [Authorize]
    public class VideoSessionsController : ControllerBase
    {
        private readonly IVideoSessionService _videoSessionService;
        private readonly IRecordingProvider _recordingProvider;
        private readonly IRecordingAccessService _recordingAccessService;

        public VideoSessionsController(
            IVideoSessionService videoSessionService,
            IRecordingProvider recordingProvider,
            IRecordingAccessService recordingAccessService)
        {
            _videoSessionService = videoSessionService;
            _recordingProvider = recordingProvider;
            _recordingAccessService = recordingAccessService;
        }

        /// <summary>
        /// Retrieves a video session by its unique identifier.
        /// </summary>
        /// <remarks>
        /// Returns the full video session details including participants and their user information.
        /// The authenticated user must be either the client or the specialist associated
        /// with the session's appointment to access the session.
        ///
        /// Business flow:
        /// 1. Validates user authentication.
        /// 2. Retrieves the video session with participants.
        /// 3. Validates that the session exists.
        /// 4. Validates that the current user belongs to the appointment.
        /// 5. Returns the mapped session details.
        /// </remarks>
        /// <param name="id">The unique identifier of the video session.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>
        /// An ApiResponse containing the VideoSessionDto with session details and participants.
        /// </returns>
        /// <response code="200">Video session retrieved successfully.</response>
        /// <response code="401">User is not authenticated.</response>
        /// <response code="403">User is not authorized to access this video session.</response>
        /// <response code="404">Video session not found.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<VideoSessionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [EndpointName("GetVideoSessionById")]
        [EndpointSummary("Retrieves a video session by its unique identifier.")]
        [EndpointDescription("Returns full video session details including participants for authorized appointment members.")]
        [Tags("Communication")]
        public async Task<IResult> GetById(
            Guid id,
            CancellationToken ct)
        {
            var result = await _videoSessionService
                .GetByIdAsync(id, ct);

            return result.Match(
               value =>
               {
                   var response = ApiResponse<VideoSessionDto>
                       .SuccessResponse(
                           value,
                           "Video session retrieved successfully.");

                   return Results.Ok(response);
               },

               errors => errors.ToProblem());
        }

        /// <summary>
        /// Generates an Agora RTC token for a video session.
        /// </summary>
        /// <remarks>
        /// Generates an Agora RTC token that allows the current user to join a video channel
        /// associated with their appointment. The user must be either the client or specialist
        /// in the appointment.
        /// 
        /// The token is valid for a configured duration (typically 24 hours) and expires after that time.
        /// </remarks>
        /// <param name="request">The generate token request containing the appointment ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>
        /// An ApiResponse containing the AgoraTokenResponse with the generated token and session details.
        /// </returns>
        /// <response code="200">Token generated successfully.</response>
        /// <response code="400">Invalid request or validation failed.</response>
        /// <response code="401">User is not authenticated.</response>
        /// <response code="403">User is not authorized to access this appointment.</response>
        /// <response code="404">Appointment not found.</response>
        [HttpPost("generate-token")]
        [ProducesResponseType(typeof(ApiResponse<AgoraTokenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [EndpointName("GenerateAgoraToken")]
        [EndpointSummary("Generates an Agora RTC token for video session access.")]
        [EndpointDescription("Generates a token that allows the authenticated user to join a video channel for their appointment.")]
        [Tags("Communication")]
        public async Task<IResult> GenerateToken(
            GenerateAgoraTokenRequest request,
            CancellationToken ct)
        {
            var result = await _videoSessionService.GenerateTokenAsync(
                request.AppointmentId,
                ct);

            return result.Match(
                value =>
                {
                    var response = ApiResponse<AgoraTokenResponse>
                        .SuccessResponse(
                            value,
                            "Agora token generated successfully.");

                    return Results.Ok(response);
                },

                errors => errors.ToProblem());
        }
        [HttpPost("{id:guid}/join")]
        [ProducesResponseType(typeof(ApiResponse<JoinVideoSessionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [EndpointName("JoinVideoSession")]
        [EndpointSummary("Joins a video session.")]
        [EndpointDescription("Allows an authorized participant to join the video session. Validates time window and session state.")]
        [Tags("Communication")]
        public async Task<IResult> Join(
            Guid id,
            CancellationToken ct)
        {
            var result = await _videoSessionService
                .JoinAsync(id, ct);

            return result.Match(
                value =>
                {
                    var response = ApiResponse<JoinVideoSessionResponse>
                        .SuccessResponse(value, "Joined video session successfully.");
                    return Results.Ok(response);
                },
                errors => errors.ToProblem());
        }

        [HttpPost("{id:guid}/leave")]
        [ProducesResponseType(typeof(ApiResponse<LeaveVideoSessionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [EndpointName("LeaveVideoSession")]
        [EndpointSummary("Leaves a video session.")]
        [EndpointDescription("Allows a participant to leave the session without ending it. The session remains active.")]
        [Tags("Communication")]
        public async Task<IResult> Leave(
            Guid id,
            CancellationToken ct)
        {
            var result = await _videoSessionService
                .LeaveAsync(id, ct);

            return result.Match(
                value =>
                {
                    var response = ApiResponse<LeaveVideoSessionResponse>
                        .SuccessResponse(value, "Left video session successfully.");
                    return Results.Ok(response);
                },
                errors => errors.ToProblem());
        }

        [HttpPost("{id:guid}/finish-consultation")]
        [ProducesResponseType(typeof(ApiResponse<FinishConsultationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [EndpointName("FinishConsultation")]
        [EndpointSummary("Finishes the consultation.")]
        [EndpointDescription("Allows the assigned specialist to complete the consultation. Stops any active recording and marks the session as completed.")]
        [Tags("Communication")]
        public async Task<IResult> FinishConsultation(
            Guid id,
            CancellationToken ct)
        {
            var result = await _videoSessionService
                .FinishConsultationAsync(id, ct);

            return result.Match(
                value =>
                {
                    var response = ApiResponse<FinishConsultationResponse>
                        .SuccessResponse(value, "Consultation finished successfully.");
                    return Results.Ok(response);
                },
                errors => errors.ToProblem());
        }

        /// <summary>
        /// Starts a video session.
        /// </summary>
        /// <remarks>
        /// Only the assigned specialist can start the session.
        /// </remarks>
        /// <response code="200">Session started successfully.</response>
        /// <response code="401">User is not authenticated.</response>
        /// <response code="403">Only specialist can start the session.</response>
        /// <response code="404">Video session not found.</response>
        [HttpPost("{id:guid}/start")]
        [ProducesResponseType(typeof(ApiResponse<StartVideoSessionResponse>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound)]
        [EndpointSummary("Starts a video session.")]
        [EndpointDescription("Allows the assigned specialist to start the video consultation session.")]
        [Tags("Communication")]
        public async Task<IResult> Start(
            Guid id,
            CancellationToken ct)
        {
            var result = await _videoSessionService
                .StartAsync(id, ct);


            return result.Match(
               value =>
               {
                   var response = ApiResponse<StartVideoSessionResponse>
                       .SuccessResponse(
                           value,
                           "Video Session has been started successfully.");

                   return Results.Ok(response);
               },

               errors => errors.ToProblem());
        }

        /// <summary>
        /// Ends a video session.
        /// </summary>
        [HttpPost("{id:guid}/end")]
        [ProducesResponseType(typeof(ApiResponse<EndVideoSessionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType( typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType( typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType( typeof(ProblemDetails),   StatusCodes.Status404NotFound)]
        [EndpointName("EndVideoSession")]
        [EndpointSummary("Ends a video consultation session.")]
        [EndpointDescription("Allows the assigned specialist to end the video consultation session.")]
        [Tags("Communication")]
        public async Task<IResult> End(
            Guid id,
            CancellationToken ct)
        {
            var result = await _videoSessionService
                .EndAsync(id, ct);

            return result.Match(
               value =>
               {
                   var response = ApiResponse<EndVideoSessionResponse>
                       .SuccessResponse(
                           value,
                           "Video Session has been ended successfully.");

                   return Results.Ok(response);
               },

               errors => errors.ToProblem());
        }

        [HttpPost("{id:guid}/recording/start")]
        [ProducesResponseType(typeof(ApiResponse<StartRecordingResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [EndpointName("StartRecording")]
        [EndpointSummary("Starts recording the video session.")]
        [EndpointDescription("Allows the assigned specialist to start cloud recording for an active video session.")]
        [Tags("Communication")]
        public async Task<IResult> StartRecording(
            Guid id,
            CancellationToken ct)
        {
            var result = await _videoSessionService
                .StartRecordingAsync(id, ct);

            return result.Match(
                value =>
                {
                    var response = ApiResponse<StartRecordingResponse>
                        .SuccessResponse(value, "Recording started successfully.");
                    return Results.Ok(response);
                },
                errors => errors.ToProblem());
        }

        [HttpPost("{id:guid}/recording/stop")]
        [ProducesResponseType(typeof(ApiResponse<StopRecordingResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [EndpointName("StopRecording")]
        [EndpointSummary("Stops recording the video session.")]
        [EndpointDescription("Allows the assigned specialist to stop cloud recording.")]
        [Tags("Communication")]
        public async Task<IResult> StopRecording(
            Guid id,
            CancellationToken ct)
        {
            var result = await _videoSessionService
                .StopRecordingAsync(id, ct);

            return result.Match(
                value =>
                {
                    var response = ApiResponse<StopRecordingResponse>
                        .SuccessResponse(value, "Recording stopped successfully.");
                    return Results.Ok(response);
                },
                errors => errors.ToProblem());
        }

        [HttpGet("{id:guid}/recording/watch")]
        [ProducesResponseType(typeof(ApiResponse<RecordingWatchResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [EndpointName("WatchRecording")]
        [EndpointSummary("Gets a secure temporary URL to watch the recording.")]
        [EndpointDescription("Returns a time-limited presigned URL for authorized users. Only the appointment owner, assigned specialist, or admin can access.")]
        [Tags("Communication")]
        public async Task<IResult> WatchRecording(
            Guid id,
            [FromQuery] int? expirationMinutes,
            CancellationToken ct)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;

            var expiration = expirationMinutes.HasValue
                ? TimeSpan.FromMinutes(Math.Clamp(expirationMinutes.Value, 1, 60))
                : TimeSpan.FromMinutes(15);

            var result = await _recordingAccessService.GetWatchUrlAsync(
                id, userId, role, expiration, ct);

            return result.Match(
                value =>
                {
                    var response = ApiResponse<RecordingWatchResponse>
                        .SuccessResponse(value, "Recording watch URL generated.");
                    return Results.Ok(response);
                },
                errors =>
                {
                    if (errors.Any(e => e.Code == "Recording.AccessDenied"))
                    {
                        return Results.Forbid();
                    }
                    if (errors.Any(e => e.Code == "Recording.NotFound"))
                    {
                        return Results.NotFound(new ProblemDetails
                        {
                            Status = StatusCodes.Status404NotFound,
                            Title = "Recording not found."
                        });
                    }
                    return errors.ToProblem();
                });
        }

        /// <summary>
        /// Downloads the recording file for a video session directly from storage.
        /// </summary>
        /// <remarks>
        /// Streams the recording file directly from Cloudflare R2 without loading it into
        /// server memory.  Authorisation follows the same rules as the watch endpoint:
        /// the appointment owner, assigned specialist, or an Admin may download.
        ///
        /// The response carries <c>Content-Disposition: attachment</c> so browsers prompt a
        /// save-as dialog rather than attempting inline playback.
        ///
        /// Returns <c>404</c> for both "not found" and "upload not yet complete" cases to
        /// avoid revealing whether a recording exists to unauthorised callers.
        /// </remarks>
        /// <param name="id">The unique identifier of the video session.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A streaming file download of the recording.</returns>
        /// <response code="200">Recording file streamed successfully.</response>
        /// <response code="401">User is not authenticated.</response>
        /// <response code="403">User is not authorized to download this recording.</response>
        /// <response code="404">Recording not found or upload not yet complete.</response>
        [HttpGet("{id:guid}/recording/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [EndpointName("DownloadRecording")]
        [EndpointSummary("Downloads the recording file directly from storage.")]
        [EndpointDescription("Streams the recording from Cloudflare R2 to the client without loading it into server memory. Requires owner, specialist, or Admin role.")]
        [Tags("Communication")]
        public async Task<IResult> DownloadRecording(
            Guid id,
            CancellationToken ct)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;

            var result = await _recordingAccessService.GetDownloadStreamAsync(id, userId, role, ct);

            return result.Match<IResult>(
                value =>
                {
                    // Results.Stream disposes the stream after the body is written — no memory copy.
                    return Results.Stream(
                        value.Content,
                        value.ContentType,
                        fileDownloadName: value.FileName,
                        enableRangeProcessing: false);
                },
                errors =>
                {
                    if (errors.Any(e => e.Code == "Recording.AccessDenied"))
                    {
                        return Results.Forbid();
                    }

                    if (errors.Any(e => e.Code == "Recording.NotFound"))
                    {
                        return Results.NotFound(new ProblemDetails
                        {
                            Status = StatusCodes.Status404NotFound,
                            Title = "Recording not found."
                        });
                    }

                    return errors.ToProblem();
                });
        }

        [HttpGet("{id:guid}/recording")]
        [ProducesResponseType(typeof(ApiResponse<RecordingInfoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [EndpointName("GetRecording")]
        [EndpointSummary("Gets recording information for the session.")]
        [EndpointDescription("Returns current recording state and metadata for both participants and specialists.")]
        [Tags("Communication")]
        public async Task<IResult> GetRecording(
            Guid id,
            CancellationToken ct)
        {
            var result = await _videoSessionService
                .GetRecordingAsync(id, ct);

            return result.Match(
                value =>
                {
                    var response = ApiResponse<RecordingInfoDto>
                        .SuccessResponse(value, "Recording info retrieved successfully.");
                    return Results.Ok(response);
                },
                errors => errors.ToProblem());
        }

        [HttpGet("{id:guid}/recording/agora-status")]
        [ProducesResponseType(typeof(ApiResponse<QueryResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [EndpointName("QueryAgoraRecordingStatus")]
        [EndpointSummary("Queries Agora Cloud Recording status for the session.")]
        [EndpointDescription("Calls the Agora Query API to get the current cloud recording status for this session's active recording.")]
        [Tags("Communication")]
        public async Task<IResult> QueryRecordingStatus(
            Guid id,
            CancellationToken ct)
        {
            var sessionResult = await _videoSessionService
                .GetByIdAsync(id, ct);

            return await sessionResult.Match(
                async session =>
                {
                    if (string.IsNullOrWhiteSpace(session.AgoraRecordingId) ||
                        string.IsNullOrWhiteSpace(session.AgoraRecordingSid))
                    {
                        var error = Shared.Error.NotFound(
                            "Recording.NotStarted",
                            "No active recording found for this session. Start a recording first.");
                        return new[] { error }.ToProblem();
                    }

                    var queryResult = await _recordingProvider.QueryAsync(
                        session.AgoraRecordingId, session.AgoraRecordingSid, ct);

                    return queryResult.Match(
                        value =>
                        {
                            var response = ApiResponse<QueryResult>
                                .SuccessResponse(value, "Recording status retrieved from Agora.");
                            return Results.Ok(response);
                        },
                        errors => errors.ToProblem());
                },
                errors => Task.FromResult(errors.ToProblem()));
        }
    }
}
