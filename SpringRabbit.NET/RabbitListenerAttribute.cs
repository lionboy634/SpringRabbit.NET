namespace SpringRabbit.NET;

/// <summary>
/// Marks a method as a RabbitMQ message listener, similar to Spring's @RabbitListener.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RabbitListenerAttribute : Attribute
{
    /// <summary>
    /// The queue name(s) to listen to.
    /// </summary>
    public string[] Queues { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Concurrency configuration in format "min-max" (e.g., "3-10").
    /// Defaults to "1-1" if not specified.
    /// </summary>
    public string Concurrency { get; set; } = "1-1";

    /// <summary>
    /// Whether to enable dead letter queue (DLQ) support.
    /// Defaults to true.
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Maximum priority for priority queues (0-255).
    /// If set to 0, priority queue support is disabled.
    /// </summary>
    public byte MaxPriority { get; set; } = 0;

    /// <summary>
    /// Prefetch count (QoS). Defaults to 1.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 1;

    /// <summary>
    /// Maximum number of retry attempts. Defaults to 0 (no retry).
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 0;

    /// <summary>
    /// Initial retry delay in milliseconds. Defaults to 1000ms.
    /// </summary>
    public int RetryInitialDelayMs { get; set; } = 1000;

    /// <summary>
    /// Retry multiplier for exponential backoff. Defaults to 2.0.
    /// </summary>
    public double RetryMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Maximum retry delay in milliseconds. Defaults to 300000ms (5 minutes).
    /// </summary>
    public int RetryMaxDelayMs { get; set; } = 300000;

    public RabbitListenerAttribute(params string[] queues)
    {
        Queues = queues;
    }

    /// <summary>
    /// Parses the concurrency string (e.g., "3-10") into min and max values.
    /// </summary>
    public (int Min, int Max) ParseConcurrency()
    {
        if (string.IsNullOrWhiteSpace(Concurrency))
            return (1, 1);

        var parts = Concurrency.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[0], out var min) && int.TryParse(parts[1], out var max))
        {
            return (Math.Max(1, min), Math.Max(min, max));
        }

        if (int.TryParse(Concurrency, out var single))
        {
            return (single, single);
        }

        return (1, 1);
    }
}

