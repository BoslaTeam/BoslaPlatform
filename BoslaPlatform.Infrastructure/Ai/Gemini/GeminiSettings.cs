namespace BoslaPlatform.Infrastructure.Settings;

public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    // Base URL should be set to: https://generativelanguage.googleapis.com
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    public string EmbeddingModel { get; set; } = "text-embedding-004";
    public string ChatModel { get; set; } = "gemini-2.5-flash";
    // Chat API endpoint
    public string ChatEndpoint { get; set; } = "/v1beta/chat/completions";
    // Embeddings endpoint template; '{model}' will be replaced with the model name
    public string EmbeddingEndpoint { get; set; } = "/v1beta/models/{model}:embedContent";
    // Concurrency limit for embedding requests (simple batching control)
    public int EmbeddingConcurrency { get; set; } = 4;
}
