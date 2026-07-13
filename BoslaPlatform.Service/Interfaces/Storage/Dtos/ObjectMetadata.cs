namespace BoslaPlatform.Application.Interfaces.Storage.Dtos;

public sealed record ObjectMetadata(
    string BucketName,
    string ObjectKey,
    string ContentType,
    long ContentLength,
    DateTime? LastModified,
    IDictionary<string, string>? Metadata = null);
