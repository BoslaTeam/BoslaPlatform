namespace BoslaPlatform.Application.Interfaces.Storage.Dtos;

public sealed record DownloadObjectResponse(
    Stream Content,
    string ContentType,
    long ContentLength);
