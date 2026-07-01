using System.Text.Json.Serialization;

namespace BoslaPlatform.Infrastructure.AI.Gemini;

// Interactions request
public class GeminiInteractionsRequest
{
    public GeminiInteractionsRequest(string model, object input)
    {
        Model = model;
        Input = input;
    }

    [JsonPropertyName("model")] public string Model { get; set; }
    [JsonPropertyName("input")] public object Input { get; set; }
}

// Interactions response shapes (simplified)
public class GeminiInteractionsResponse
{
    [JsonPropertyName("output_text")] public string? OutputText { get; set; }
    [JsonPropertyName("output")] public GeminiOutput[]? Output { get; set; }
    [JsonPropertyName("candidates")] public GeminiCandidate[]? Candidates { get; set; }
}

public class GeminiOutput
{
    [JsonPropertyName("output_text")] public string? OutputText { get; set; }
    [JsonPropertyName("content")] public GeminiContent[]? Content { get; set; }
    [JsonPropertyName("candidates")] public GeminiCandidate[]? Candidates { get; set; }
}

public class GeminiCandidate
{
    [JsonPropertyName("output_text")] public string? OutputText { get; set; }
    [JsonPropertyName("content")] public GeminiContent[]? Content { get; set; }
}

public class GeminiContent
{
    [JsonPropertyName("text")] public string? Text { get; set; }
}

// Embedding request/response
public record GeminiEmbeddingRequest(string Input)
{
    [JsonPropertyName("input")] public string input { get; init; } = Input;
}

public class GeminiEmbeddingResponse
{
    [JsonPropertyName("embedding")] public float[]? Embedding { get; set; }
    [JsonPropertyName("embeddings")] public GeminiEmbeddingData[]? Embeddings { get; set; }
    [JsonPropertyName("embedding_value")] public float[]? Embedding_value { get; set; }
    [JsonPropertyName("results")] public GeminiEmbeddingResult[]? Results { get; set; }
    [JsonPropertyName("data")] public GeminiEmbeddingData[]? Data { get; set; }
}

public class GeminiEmbeddingResult
{
    [JsonPropertyName("embedding")] public float[]? Embedding { get; set; }
}

public class GeminiEmbeddingData
{
    [JsonPropertyName("embedding")] public float[]? Embedding { get; set; }
}
