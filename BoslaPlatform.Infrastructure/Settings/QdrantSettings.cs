namespace BoslaPlatform.Infrastructure.Settings;

public class QdrantSettings
{
    public string BaseUrl { get; set; } = string.Empty; // e.g. http://localhost:6333
    public string ApiKey { get; set; } = string.Empty;
    public string CollectionName { get; set; } = "specialists";
    public int DefaultTopK { get; set; } = 10;
    // Vector size for the collection (must match embedding dimension returned by the embedding model)
    public int VectorSize { get; set; } = 3072;
}
