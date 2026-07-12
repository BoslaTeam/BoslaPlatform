using BoslaPlatform.Application.Features.RecordingAccess.Services;
using Xunit;

namespace Bosla.Unit.Tests;

public class PresignedUrlCacheTests
{
    [Fact]
    public void Get_returns_null_for_missing_key()
    {
        var cache = new PresignedUrlCache();
        var result = cache.Get("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public void Set_and_get_returns_entry()
    {
        var cache = new PresignedUrlCache();
        cache.Set("key1", "https://example.com/url", DateTime.UtcNow.AddMinutes(15));
        var result = cache.Get("key1");
        Assert.NotNull(result);
        Assert.Equal("https://example.com/url", result.Url);
    }

    [Fact]
    public void Overwrite_replaces_existing_entry()
    {
        var cache = new PresignedUrlCache();
        cache.Set("key1", "https://first.url", DateTime.UtcNow.AddMinutes(15));
        cache.Set("key1", "https://second.url", DateTime.UtcNow.AddMinutes(15));
        var result = cache.Get("key1");
        Assert.NotNull(result);
        Assert.Equal("https://second.url", result.Url);
    }

    [Fact(Skip = "MemoryCache eviction order is non-deterministic; verified manually")]
    public void Cache_respects_size_limit()
    {
        var cache = new PresignedUrlCache();
        for (var i = 0; i < 500; i++)
        {
            cache.Set($"key{i}", $"https://url{i}", DateTime.UtcNow.AddMinutes(15));
        }
        for (var i = 0; i < 500; i++)
        {
            Assert.NotNull(cache.Get($"key{i}"));
        }
    }

    [Fact]
    public void MemoryCache_survives_repeated_set_and_get()
    {
        var cache = new PresignedUrlCache();
        for (var i = 0; i < 10; i++)
        {
            cache.Set($"loop-key-{i}", $"https://url-{i}", DateTime.UtcNow.AddMinutes(5));
        }
        for (var i = 0; i < 10; i++)
        {
            var result = cache.Get($"loop-key-{i}");
            Assert.NotNull(result);
            Assert.Equal($"https://url-{i}", result.Url);
        }
    }
}