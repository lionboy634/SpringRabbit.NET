namespace SpringRabbit.NET;

/// <summary>
/// Interface for retry policies that determine retry behavior.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    /// Determines if a retry should be attempted for the given exception and attempt number.
    /// </summary>
    bool ShouldRetry(Exception exception, int attemptNumber);

    /// <summary>
    /// Gets the delay before the next retry attempt.
    /// </summary>
    TimeSpan GetRetryDelay(int attemptNumber);

    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    int MaxAttempts { get; }
}








