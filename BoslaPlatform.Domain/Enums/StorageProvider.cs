using System.Text.Json.Serialization;

namespace BoslaPlatform.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum StorageProvider
    {
        CloudflareR2,
        AmazonS3,
        AzureBlob,
        GoogleCloudStorage,
        MinIO
    }
}