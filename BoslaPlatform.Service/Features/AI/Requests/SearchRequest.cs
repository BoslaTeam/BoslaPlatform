namespace BoslaPlatform.Service.Features.AI.Requests;

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 5;
}
