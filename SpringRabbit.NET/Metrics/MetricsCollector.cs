using System.Collections.Concurrent;

namespace SpringRabbit.NET.Metrics;

/// <summary>
/// Collects and aggregates metrics for all listeners.
/// </summary>
public class MetricsCollector
{
    private readonly ConcurrentDictionary<string, ListenerMetrics> _metrics = new();

    /// <summary>
    /// Gets or creates metrics for a queue.
    /// </summary>
    public ListenerMetrics GetMetrics(string queueName)
    {
        return _metrics.GetOrAdd(queueName, _ => new ListenerMetrics { QueueName = queueName });
    }

    /// <summary>
    /// Gets all collected metrics.
    /// </summary>
    public IEnumerable<ListenerMetrics> GetAllMetrics()
    {
        return _metrics.Values;
    }

    /// <summary>
    /// Gets metrics for a specific queue.
    /// </summary>
    public ListenerMetrics? GetMetricsForQueue(string queueName)
    {
        return _metrics.TryGetValue(queueName, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Resets all metrics.
    /// </summary>
    public void Reset()
    {
        _metrics.Clear();
    }
}







