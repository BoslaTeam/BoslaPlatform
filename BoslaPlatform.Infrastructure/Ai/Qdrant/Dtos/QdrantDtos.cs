namespace BoslaPlatform.Infrastructure.AI.Qdrant.Dtos;

public class CreateCollectionRequest
{
    public string name { get; set; } = string.Empty;
    public object vectors { get; set; } = new { size = 1536, distance = "Cosine" };
}

public class UpsertPoint
{
    public string id { get; set; } = string.Empty;
    public object vector { get; set; } = new { };
    public object payload { get; set; } = new { };
}

public class UpsertRequest
{
    public UpsertPoint[] points { get; set; } = Array.Empty<UpsertPoint>();
}

public class SearchRequest
{
    public float[] vector { get; set; } = Array.Empty<float>();
    public int limit { get; set; }
}

public class SearchResultItem
{
    public string id { get; set; } = string.Empty;
    public float score { get; set; }
}

public class SearchResponse
{
    public SearchResultItem[] result { get; set; } = Array.Empty<SearchResultItem>();
}
