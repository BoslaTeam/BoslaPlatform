using BoslaPlatform.Application.Interfaces.Storage.Dtos;
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

        /// <summary>
        /// Opens a lazy read stream for <paramref name="objectKey"/> without buffering the entire
        /// object into memory.  The <see cref="DownloadObjectResponse.Content"/> stream is read
        /// directly from the storage provider over the network; the caller is responsible for
        /// disposing it once the HTTP response body has been fully written.
        /// </summary>
        Task<Result<DownloadObjectResponse>> OpenReadStreamAsync(
            string bucketName,
            string objectKey,
            CancellationToken ct = default);
    }
}