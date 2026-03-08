using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace SmartTravelPlanner.Infrastructure.Caching;

/// <summary>
/// Wraps IMemoryCache with configurable TTLs from appsettings.
/// </summary>
public class InMemoryCacheService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _placesTtl;
    private readonly TimeSpan _weatherTtl;
    private readonly TimeSpan _forexTtl;

    public InMemoryCacheService(IMemoryCache cache, IConfiguration config)
    {
        _cache = cache;
        _placesTtl = TimeSpan.FromDays(int.Parse(config["Caching:PlaceCacheExpirationDays"] ?? "7"));
        _weatherTtl = TimeSpan.FromHours(int.Parse(config["Caching:WeatherCacheExpirationHours"] ?? "6"));
        _forexTtl = TimeSpan.FromHours(int.Parse(config["Caching:ExchangeRateCacheExpirationHours"] ?? "24"));
    }

    public async Task<T?> GetOrSetAsync<T>(string key, CacheCategory category, Func<Task<T>> factory)
    {
        if (_cache.TryGetValue(key, out T? cached))
            return cached;

        var value = await factory();
        if (value is not null)
        {
            var ttl = category switch
            {
                CacheCategory.Places => _placesTtl,
                CacheCategory.Weather => _weatherTtl,
                CacheCategory.ExchangeRate => _forexTtl,
                _ => TimeSpan.FromHours(1)
            };

            _cache.Set(key, value, ttl);
        }

        return value;
    }

    public void Remove(string key) => _cache.Remove(key);
}

public enum CacheCategory
{
    Places,
    Weather,
    ExchangeRate
}
