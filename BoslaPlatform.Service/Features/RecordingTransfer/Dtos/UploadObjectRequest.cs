namespace BoslaPlatform.Application.Features.RecordingTransfer.Dtos;

public sealed record UploadObjectRequest(
    string BucketName,
    string ObjectKey,
    Stream Content,
    string ContentType,
    long ContentLength);