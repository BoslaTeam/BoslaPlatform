using BoslaPlatform.Application.Features.VideoSessions.Responses;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Video
{
    /// <summary>
    /// Service interface for generating Agora RTC tokens.
    /// </summary>
    public interface IAgoraTokenService
    {
        /// <summary>
        /// Generates an Agora RTC token for accessing a video session channel.
        /// </summary>
        /// <param name="appointmentId">The ID of the appointment.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A Result containing the AgoraTokenResponse with token and session details.</returns>
        Task<Result<AgoraTokenResponse>> GenerateTokenAsync(
            Guid appointmentId,
            CancellationToken ct = default);
    }
}
