using System.Text.Json;

namespace BoslaPlatform.Infrastructure.AI.VectorStore;

public static class VectorParser
{
    public static bool TryParse(string? vectorJson, out float[] vector)
    {
        vector = Array.Empty<float>();
        if (string.IsNullOrWhiteSpace(vectorJson))
            return false;

        try
        {
            vector = JsonSerializer.Deserialize<float[]>(vectorJson) ?? Array.Empty<float>();
            return vector.Length > 0;
        }
        catch
        {
            try
            {
                var parts = vectorJson.Trim('[', ']', ' ').Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                vector = parts.Select(p => float.Parse(p)).ToArray();
                return vector.Length > 0;
            }
            catch
            {
                vector = Array.Empty<float>();
                return false;
            }
        }
    }
}
