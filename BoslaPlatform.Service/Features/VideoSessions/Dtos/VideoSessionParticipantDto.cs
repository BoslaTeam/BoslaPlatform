namespace BoslaPlatform.Application.Features.VideoSessions.Dtos
{
    /// <summary>
    /// Data transfer object representing a participant in a video session.
    /// Contains user identity, role, and participation timestamps.
    /// </summary>
    public sealed class VideoSessionParticipantDto
    {
        /// <summary>
        /// The unique identifier of the participant user.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The display name of the participant.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// The role of the participant in the video session (e.g., Host, Participant).
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// The UTC timestamp when the participant joined the session.
        /// </summary>
        public DateTime? JoinedAt { get; set; }

        /// <summary>
        /// The UTC timestamp when the participant left the session.
        /// Null if the participant is still active.
        /// </summary>
        public DateTime? LeftAt { get; set; }
    }
}
