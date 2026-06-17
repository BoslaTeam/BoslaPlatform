namespace BoslaPlatform.Infrastructure.AI.OpenAi.Dtos;

public class EmbeddingRequest
{
    public string input { get; set; } = string.Empty;
    public string model { get; set; } = string.Empty;
}

public class EmbeddingResponseData
{
    public float[] embedding { get; set; } = Array.Empty<float>();
}

public class EmbeddingResponse
{
    public EmbeddingResponseData[] data { get; set; } = Array.Empty<EmbeddingResponseData>();
}

public class ChatMessage
{
    public string role { get; set; } = string.Empty;
    public string content { get; set; } = string.Empty;
}

public class ChatRequest
{
    public string model { get; set; } = string.Empty;
    public ChatMessage[] messages { get; set; } = Array.Empty<ChatMessage>();
}

public class ChatChoice
{
    public ChatMessage message { get; set; } = new ChatMessage();
}

public class ChatResponse
{
    public ChatChoice[] choices { get; set; } = Array.Empty<ChatChoice>();
}
