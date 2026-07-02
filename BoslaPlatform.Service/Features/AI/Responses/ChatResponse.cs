using BoslaPlatform.Application.Features.Specialists.DTOs;

namespace BoslaPlatform.Service.Features.AI.Responses;

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public List<SpecialistListItemResponse> Specialists { get; set; } = [];
}
