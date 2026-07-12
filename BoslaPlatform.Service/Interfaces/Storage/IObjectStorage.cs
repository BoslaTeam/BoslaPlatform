using BoslaPlatform.Application.Features.RecordingTransfer.Dtos;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Storage
{
    public interface IObjectStorage
    {
        Task<Result<UploadObjectResponse>> UploadAsync(
            UploadObjectRequest request,
            CancellationToken ct = default);

        Task<Result<DownloadObjectResponse>> DownloadAsync(
            string bucketName,
            string objectKey,
            CancellationToken ct = default);

        Task<Result> DeleteAsync(
            string bucketName,
            string objectKey,
            CancellationToken ct = default);

        Task<Result<bool>> ExistsAsync(
            string bucketName,
            string objectKey,
            CancellationToken ct = default);

        Task<Result<ObjectMetadata>> GetMetadataAsync(
            string bucketName,
            string objectKey,
            CancellationToken ct = default);

        Task<Result<string>> GeneratePresignedUrlAsync(
            string bucketName,
            string objectKey,
            TimeSpan? expiration = null,
            CancellationToken ct = default);
    }
}