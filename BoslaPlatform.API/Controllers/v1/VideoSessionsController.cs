using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.VideoSessions.Dtos;
using BoslaPlatform.Application.Features.VideoSessions.Interfaces;
using BoslaPlatform.Application.Features.VideoSessions.Requests;
using BoslaPlatform.Application.Features.VideoSessions.Responses;
using BoslaPlatform.Application.Interfaces.Video;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoslaPlatform.API.Controllers.v1
{
    /// <summary>
    /// Controller for managing video sessions and generating Agora tokens.
    /// Provides endpoints for video session operations within the Bosla Platform.
    /// </summary>
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/video-sessions")]
    [ApiController]
    [Authorize]
    public class VideoSessionsController : ControllerBase
    {
        private readonly IVideoSessionService _videoSessionService;

        /// <summary>
        /// Initializes a new instance of the VideoSessionsController.
        /// </summary>
        /// <param name="videoSessionService">The video session service.</param>
        /// <exception cref="ArgumentNullException">Thrown when videoSessionService is null.</exception>
        public VideoSessionsController(IVideoSessionService videoSessionService)
        {
            _videoSessionService = videoSessionService;
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
    //    [HttpPost("{id:guid}/join")]
    //    public async Task<IActionResult> Join(
    //Guid id,
    //CancellationToken ct)
    //    {
    //        var result = await _videoSessionService
    //            .JoinAsync(id, ct);

    //        return result.ToApiResponse();
    //    }

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
        /// <remarks>
        /// Ends the active video session and publishes a VideoSessionEndedEvent.
        /// </remarks>
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
    }
}
