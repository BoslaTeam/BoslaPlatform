using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BoslaPlatform.Infrastructure.AI.Gemini;

public static class TelemetryExtensions
{
    public static async Task<T> TrackRequestAsync<T>(this ILogger logger, string name, Func<Task<T>> func)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await func();
            sw.Stop();
            logger.LogInformation("Telemetry: {Name} succeeded in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Telemetry: {Name} failed in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
