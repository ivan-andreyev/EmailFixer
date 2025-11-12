namespace EmailFixer.Client.Services;

/// <summary>
/// Service for debouncing expensive operations
/// Prevents UI lag from rapid input events
/// </summary>
public class DebounceService
{
    private readonly Dictionary<string, Timer> _timers = new();

    /// <summary>
    /// Debounce an action - only execute after delay without new calls
    /// </summary>
    /// <param name="key">Unique key for this debounce operation</param>
    /// <param name="action">Action to execute</param>
    /// <param name="delayMs">Delay in milliseconds</param>
    public void Debounce(string key, Action action, int delayMs = 300)
    {
        // Cancel previous timer if exists
        if (_timers.ContainsKey(key))
        {
            _timers[key].Dispose();
        }

        // Create new timer
        var timer = new Timer(_ =>
        {
            action?.Invoke();
            if (_timers.ContainsKey(key))
            {
                _timers.Remove(key);
            }
        }, null, delayMs, Timeout.Infinite);

        _timers[key] = timer;
    }

    /// <summary>
    /// Debounce an async action
    /// </summary>
    public void Debounce(string key, Func<Task> action, int delayMs = 300)
    {
        if (_timers.ContainsKey(key))
        {
            _timers[key].Dispose();
        }

        var timer = new Timer(async _ =>
        {
            await action?.Invoke()!;
            if (_timers.ContainsKey(key))
            {
                _timers.Remove(key);
            }
        }, null, delayMs, Timeout.Infinite);

        _timers[key] = timer;
    }

    /// <summary>
    /// Cancel a debounced operation
    /// </summary>
    public void Cancel(string key)
    {
        if (_timers.ContainsKey(key))
        {
            _timers[key].Dispose();
            _timers.Remove(key);
        }
    }

    /// <summary>
    /// Clear all timers
    /// </summary>
    public void ClearAll()
    {
        foreach (var timer in _timers.Values)
        {
            timer.Dispose();
        }
        _timers.Clear();
    }
}
