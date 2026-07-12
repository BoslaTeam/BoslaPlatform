namespace BoslaPlatform.Application.Features.RecordingTransfer.Dtos;

public sealed record UploadObjectResponse(
    string BucketName,
    string ObjectKey,
    long ContentLength,
    DateTime UploadedAtUtc,
    string? ETag = null,
    string? VersionId = null,
    string? ChecksumSha256 = null);