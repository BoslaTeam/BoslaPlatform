using BoslaPlatform.Shared;

namespace BoslaPlatform.Service.Features.AI.Requests;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessageDto> History { get; set; } = [];
}

public class ChatMessageDto
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}
