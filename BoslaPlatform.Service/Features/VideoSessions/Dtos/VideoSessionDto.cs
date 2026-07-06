namespace BoslaPlatform.Application.Features.VideoSessions.Dtos
{
    /// <summary>
    /// Data transfer object representing a video session with its full details.
    /// Includes session metadata, status, timing information, and participant list.
    /// </summary>
    public sealed class VideoSessionDto
    {
        /// <summary>
        /// The unique identifier of the video session.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The unique identifier of the associated appointment.
        /// </summary>
        public Guid AppointmentId { get; set; }

        /// <summary>
        /// The Agora channel name mapped from the session's AgoraChannelName.
        /// </summary>
        public string ChannelName { get; set; } = string.Empty;

        /// <summary>
        /// The current status of the video session (Waiting, Active, or Ended).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// The UTC timestamp when the session was started.
        /// Null if the session has not started yet.
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// The UTC timestamp when the session ended.
        /// Null if the session has not ended yet.
        /// </summary>
        public DateTime? EndedAt { get; set; }

        /// <summary>
        /// The list of participants in the video session.
        /// </summary>
        public IReadOnlyList<VideoSessionParticipantDto> Participants { get; set; }
            = Array.Empty<VideoSessionParticipantDto>();

        /// <summary>
        /// Recording state information for the current session.
        /// </summary>
        public RecordingInfoDto? Recording { get; set; }

        /// <summary>
        /// The UTC timestamp when the appointment ends.
        /// </summary>
        public DateTime? AppointmentEndTime { get; set; }
    }
}
