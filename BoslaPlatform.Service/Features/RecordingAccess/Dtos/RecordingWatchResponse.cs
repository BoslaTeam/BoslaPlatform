namespace BoslaPlatform.Application.Features.RecordingAccess.Dtos;

public sealed record RecordingWatchResponse(
    string PresignedUrl,
    DateTime ExpiresAtUtc,
    string ContentType,
    long? ContentLength,
    string? FileName,
    int? DurationSeconds);
