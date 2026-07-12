namespace BoslaPlatform.Application.Features.RecordingAccess.Dtos;

/// <summary>
/// Carries the raw stream and metadata needed to serve a recording download response.
/// The caller (typically the controller) is responsible for disposing <see cref="Content"/>
/// after the HTTP response body has been fully written.
/// </summary>
public sealed record RecordingDownloadContext(
    Stream Content,
    string ContentType,
    long ContentLength,
    string FileName);
