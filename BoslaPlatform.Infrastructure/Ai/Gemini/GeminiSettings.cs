namespace BoslaPlatform.Infrastructure.Settings;

public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    // Base URL should be set to: https://generativelanguage.googleapis.com
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    public string EmbeddingModel { get; set; } = "gemini-3.5-flash";
    public string ChatModel { get; set; } = "gemini-3.5-flash";
    // Interactions API (recommended)
    public string ChatEndpoint { get; set; } = "/v1beta/interactions";
    // Embeddings endpoint template; '{model}' will be replaced with the model name
    public string EmbeddingEndpoint { get; set; } = "/v1beta/models/{model}:embedContent";
}
