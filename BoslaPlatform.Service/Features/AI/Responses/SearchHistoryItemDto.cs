namespace BoslaPlatform.Service.Features.AI.Responses;

public class SearchHistoryItemDto
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string RawQuery { get; set; } = string.Empty;
    public string? ResultSpecialistIds { get; set; }
    public Guid? ClickedSpecialistId { get; set; }
    public bool? WasHelpful { get; set; }
}
