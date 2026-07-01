namespace BoslaPlatform.Application.Features.VideoSessions.Requests
{
    /// <summary>
    /// Request DTO for generating an Agora token for video session.
    /// </summary>
    public class GenerateAgoraTokenRequest
    {
        /// <summary>
        /// The ID of the appointment for which to generate the video session token.
        /// </summary>
        public Guid AppointmentId { get; set; }
    }
}
