using BoslaPlatform.Application.Features.VideoSessions.Dtos;
using BoslaPlatform.Application.Features.VideoSessions.Responses;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Features.VideoSessions.Interfaces
{
    /// <summary>
    /// Service interface for managing video session operations and Agora token generation.
    /// </summary>
    public interface IVideoSessionService
    {
        /// <summary>
        /// Retrieves a video session by its unique identifier.
        /// </summary>
        /// <remarks>
        /// Business flow:
        /// 1. Validates that the current user is authenticated.
        /// 2. Retrieves the video session with participants and their user information.
        /// 3. Validates that the session exists.
        /// 4. Validates that the current user belongs to the associated appointment
        ///    (either as the client or the specialist).
        /// 5. Maps and returns the session details.
        /// </remarks>
        /// <param name="sessionId">The unique identifier of the video session.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>
        /// A Result containing the VideoSessionDto with session details and participants,
        /// or an error if the session is not found or the user is not authorized.
        /// </returns>
        Task<Result<VideoSessionDto>> GetByIdAsync(
            Guid sessionId,
            CancellationToken ct = default);

        /// <summary>
        /// Generates an Agora RTC token for a video session associated with an appointment.
        /// </summary>
        /// <param name="appointmentId">The ID of the appointment.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>
        /// A Result containing the AgoraTokenResponse with the generated token and session details,
        /// or an error if validation fails or token generation encounters an issue.
        /// </returns>
        Task<Result<AgoraTokenResponse>> GenerateTokenAsync(
            Guid appointmentId,
            CancellationToken ct = default);

        /// <summary>
        /// Starts a video session.
        /// </summary>
        /// <param name="videoSessionId">Video session identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Started session details.</returns>
        Task<Result<StartVideoSessionResponse>> StartAsync(
            Guid videoSessionId,
            CancellationToken ct = default);

        /// <summary>
        /// Ends a video session.
        /// </summary>
        /// <param name="videoSessionId">Video session identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Ended session details.</returns>
        Task<Result<EndVideoSessionResponse>> EndAsync(
            Guid videoSessionId,
            CancellationToken ct = default);
    }
}
