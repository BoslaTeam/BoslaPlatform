using System.Collections.Concurrent;
using System.Reflection;

namespace BoslaPlatform.Infrastructure.Data.Outbox;

/// <summary>
/// Thread-safe, cached implementation of <see cref="IEventTypeResolver"/>.
///
/// Resolved types are stored in a <see cref="ConcurrentDictionary{TKey, TValue}"/> keyed by
/// <c>"AssemblyName|EventType"</c>. Subsequent resolutions for the same pair avoid
/// the <c>Assembly.Load</c> call entirely.
///
/// Registered as Singleton because the cache is process-lifetime and thread-safe.
/// </summary>
public sealed class CachedEventTypeResolver : IEventTypeResolver
{
    private static readonly ConcurrentDictionary<string, Type> _cache = new(StringComparer.Ordinal);

    public Type Resolve(string assemblyName, string eventType)
    {
        var key = $"{assemblyName}|{eventType}";

        return _cache.GetOrAdd(key, _ =>
        {
            var assembly = Assembly.Load(assemblyName);
            return assembly.GetType(eventType, throwOnError: false)
                ?? throw new InvalidOperationException(
                    $"Type '{eventType}' not found in assembly '{assemblyName}'.");
        });
    }
}
