using Microsoft.Extensions.Caching.Memory;

namespace HttpListenerExample;

public static class MemoryCache
{
    private static Lazy<IMemoryCache> _cache => new (
        () => 
            new Microsoft.Extensions.Caching.Memory.MemoryCache(new MemoryCacheOptions())
        );

    public static IMemoryCache Cache => _cache.Value;
}