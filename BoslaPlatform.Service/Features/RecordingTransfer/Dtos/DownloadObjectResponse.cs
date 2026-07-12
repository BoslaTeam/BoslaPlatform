namespace BoslaPlatform.Application.Features.RecordingTransfer.Dtos;

public sealed record DownloadObjectResponse(
    Stream Content,
    string ContentType,
    long ContentLength);