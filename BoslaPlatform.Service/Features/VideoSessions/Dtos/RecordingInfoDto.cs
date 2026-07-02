namespace BoslaPlatform.Application.Features.VideoSessions.Dtos
{
    public sealed class RecordingInfoDto
    {
        public string? Status { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public bool IsRecording { get; set; }
        public bool CanStartRecording { get; set; }
        public bool CanStopRecording { get; set; }
        public Guid? CurrentRecordingId { get; set; }
        public string? Url { get; set; }
    }
}
