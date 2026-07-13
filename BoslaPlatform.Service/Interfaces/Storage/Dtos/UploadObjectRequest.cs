namespace BoslaPlatform.Application.Interfaces.Storage.Dtos;

public sealed record UploadObjectRequest(
    string BucketName,
    string ObjectKey,
    Stream Content,
    string ContentType,
    long ContentLength);
