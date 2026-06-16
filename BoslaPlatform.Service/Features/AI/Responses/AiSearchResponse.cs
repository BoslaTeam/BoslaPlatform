namespace BoslaPlatform.Service.Features.AI.Responses;

public class SearchResultDto
{
    public Guid SpecialistId { get; set; }
    public float Score { get; set; }
    public string Snippet { get; set; } = string.Empty;
}

public class AiSearchResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<SearchResultDto> Results { get; set; } = new List<SearchResultDto>();
}
