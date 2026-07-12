using Microsoft.Extensions.Caching.Memory;

namespace BoslaPlatform.Application.Features.RecordingAccess.Services;

public sealed class PresignedUrlCache
{
    private readonly MemoryCache _cache;

    public PresignedUrlCache()
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 500
        });
    }

    public CachedUrlEntry? Get(string key)
    {
        if (_cache.TryGetValue(key, out CachedUrlEntry? entry) && entry is not null)
        {
            if (entry.ExpiresAt > DateTime.UtcNow.AddSeconds(30))
            {
                return entry;
            }
        }

        return null;
    }

    public void Set(string key, string url, DateTime expiresAt)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expiresAt,
            Size = 1,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(key, new CachedUrlEntry(url, expiresAt), cacheEntryOptions);
    }

    public sealed record CachedUrlEntry(string Url, DateTime ExpiresAt);
}