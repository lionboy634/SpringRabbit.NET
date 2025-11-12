namespace SpringRabbit.NET;

/// <summary>
/// Options for queue configuration.
/// </summary>
public class QueueOptions
{
    /// <summary>
    /// Whether to enable dead letter queue (DLQ) support.
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Maximum priority for priority queues (0-255).
    /// </summary>
    public byte MaxPriority { get; set; } = 0;

    /// <summary>
    /// Message Time-To-Live (TTL). Messages older than this will be expired.
    /// </summary>
    public TimeSpan? MessageTtl { get; set; }

    /// <summary>
    /// Whether to use lazy queue mode (messages stored on disk).
    /// </summary>
    public bool Lazy { get; set; } = false;

    /// <summary>
    /// Whether to use quorum queue type (replicated, highly available).
    /// </summary>
    public bool Quorum { get; set; } = false;
}


