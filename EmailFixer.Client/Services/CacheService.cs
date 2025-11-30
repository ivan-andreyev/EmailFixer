namespace EmailFixer.Client.Services;

/// <summary>
/// Simple in-memory cache with expiration
/// Prevents multiple API calls for the same data
/// </summary>
public class CacheService
{
    private class CacheEntry
    {
        public object? Value { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly Dictionary<string, Timer> _timers = new();

    /// <summary>
    /// Try to get value from cache
    /// </summary>
    public bool TryGetValue<T>(string key, out T? value)
    {
        value = default;

        if (!_cache.ContainsKey(key))
            return false;

        var entry = _cache[key];

        // Check if expired
        if (DateTime.UtcNow > entry.ExpiresAt)
        {
            _cache.Remove(key);
            return false;
        }

        value = (T?)entry.Value;
        return true;
    }

    /// <summary>
    /// Set value in cache with expiration
    /// </summary>
    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        // Default 10 minutes
        var exp = expiration ?? TimeSpan.FromMinutes(10);

        var entry = new CacheEntry
        {
            Value = value,
            ExpiresAt = DateTime.UtcNow.Add(exp)
        };

        _cache[key] = entry;

        // Set expiration timer
        if (_timers.ContainsKey(key))
        {
            _timers[key].Dispose();
        }

        var timer = new Timer(_ =>
        {
            _cache.Remove(key);
            if (_timers.ContainsKey(key))
            {
                _timers.Remove(key);
            }
        }, null, (int)exp.TotalMilliseconds, Timeout.Infinite);

        _timers[key] = timer;
    }

    /// <summary>
    /// Remove specific key from cache
    /// </summary>
    public void Remove(string key)
    {
        _cache.Remove(key);

        if (_timers.ContainsKey(key))
        {
            _timers[key].Dispose();
            _timers.Remove(key);
        }
    }

    /// <summary>
    /// Clear entire cache
    /// </summary>
    public void Clear()
    {
        foreach (var timer in _timers.Values)
        {
            timer.Dispose();
        }

        _cache.Clear();
        _timers.Clear();
    }
}
