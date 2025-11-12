namespace SpringRabbit.NET.Metrics;

/// <summary>
/// Metrics for a message listener.
/// </summary>
public class ListenerMetrics
{
    public string QueueName { get; set; } = string.Empty;
    public long MessagesProcessed { get; set; }
    public long MessagesFailed { get; set; }
    public long MessagesRetried { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
    public DateTime LastMessageProcessed { get; set; }
    public DateTime LastError { get; set; }
    private readonly object _lock = new();

    public void RecordSuccess(TimeSpan processingTime)
    {
        lock (_lock)
        {
            MessagesProcessed++;
            LastMessageProcessed = DateTime.UtcNow;
            // Simple moving average
            var totalTime = AverageProcessingTime.TotalMilliseconds * (MessagesProcessed - 1) + processingTime.TotalMilliseconds;
            AverageProcessingTime = TimeSpan.FromMilliseconds(totalTime / MessagesProcessed);
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            MessagesFailed++;
            LastError = DateTime.UtcNow;
        }
    }

    public void RecordRetry()
    {
        lock (_lock)
        {
            MessagesRetried++;
        }
    }
}


