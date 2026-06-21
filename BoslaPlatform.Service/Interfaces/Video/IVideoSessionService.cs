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
    }
}
