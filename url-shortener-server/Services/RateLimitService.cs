using Microsoft.Extensions.Caching.Memory;

namespace UrlShortener.Services;

public class RateLimitService
{
    private readonly IMemoryCache _cache;
    private const int RATE_LIMIT = 3; // (per minute)
    private const int TIME_WINDOW_SECONDS = 60; // one minute

    public RateLimitService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool IsRateLimited(string key)
    {
        if (!_cache.TryGetValue(key, out int requestCount))
        {
            requestCount = 0;
        }

        if (requestCount >= RATE_LIMIT)
        {
            return true;
        }

        _cache.Set(key, requestCount + 1, TimeSpan.FromSeconds(TIME_WINDOW_SECONDS));
        return false;
    }
}