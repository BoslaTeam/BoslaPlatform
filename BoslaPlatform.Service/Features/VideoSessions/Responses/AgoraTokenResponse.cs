namespace BoslaPlatform.Application.Features.VideoSessions.Responses
{
    /// <summary>
    /// Response DTO containing Agora token and session information for video sessions.
    /// </summary>
    public class AgoraTokenResponse
    {
        /// <summary>
        /// The Agora application ID.
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// The channel name for the video session.
        /// </summary>
        public string ChannelName { get; set; } = string.Empty;

        /// <summary>
        /// The generated RTC token for accessing the Agora channel.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// The expiration time of the token in UTC.
        /// </summary>
        public DateTimeOffset ExpiresAt { get; set; }

        public uint Uid { get; set; }

        public Guid SessionId { get; set; }
    }
}
