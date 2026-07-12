namespace BoslaPlatform.Application.Features.VideoSessions.Dtos;

public sealed class RecordingFileDto
{
    public string FileName { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? Duration { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string MimeType { get; set; } = string.Empty;
}
