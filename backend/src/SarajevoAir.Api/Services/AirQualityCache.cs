using System.Collections.Concurrent;
using SarajevoAir.Api.Dtos;

namespace SarajevoAir.Api.Services;

public class AirQualityCache
{
    private readonly ConcurrentDictionary<string, CacheEntry<LiveEntry>> _liveCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CacheEntry<ForecastEntry>> _forecastCache = new(StringComparer.OrdinalIgnoreCase);

    public bool TryGetLive(string city, TimeSpan ttl, out LiveEntry entry)
    {
        if (_liveCache.TryGetValue(city, out var cacheEntry))
        {
            if (DateTimeOffset.UtcNow - cacheEntry.StoredAt <= ttl)
            {
                entry = cacheEntry.Payload;
                return true;
            }

            _liveCache.TryRemove(city, out _);
        }

        entry = default!;
        return false;
    }

    public void SetLive(string city, LiveEntry entry)
    {
        _liveCache[city] = new CacheEntry<LiveEntry>(entry, DateTimeOffset.UtcNow);
    }

    public bool TryGetForecast(string city, TimeSpan ttl, out ForecastEntry entry)
    {
        if (_forecastCache.TryGetValue(city, out var cacheEntry))
        {
            if (DateTimeOffset.UtcNow - cacheEntry.StoredAt <= ttl)
            {
                entry = cacheEntry.Payload;
                return true;
            }

            _forecastCache.TryRemove(city, out _);
        }

        entry = default!;
        return false;
    }

    public void SetForecast(string city, ForecastEntry entry)
    {
        _forecastCache[city] = new CacheEntry<ForecastEntry>(entry, DateTimeOffset.UtcNow);
    }

    public record LiveEntry(LiveAqiResponse Response);

    public record ForecastEntry(ForecastResponse Response);

    private record CacheEntry<T>(T Payload, DateTimeOffset StoredAt);
}
