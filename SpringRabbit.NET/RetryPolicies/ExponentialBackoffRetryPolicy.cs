namespace SpringRabbit.NET.RetryPolicies;

/// <summary>
/// Exponential backoff retry policy with configurable initial delay and max attempts.
/// </summary>
public class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly TimeSpan _initialDelay;
    private readonly double _multiplier;
    private readonly TimeSpan _maxDelay;
    private readonly int _maxAttempts;
    private readonly HashSet<Type> _retryableExceptions;

    public ExponentialBackoffRetryPolicy(
        TimeSpan initialDelay,
        double multiplier = 2.0,
        TimeSpan? maxDelay = null,
        int maxAttempts = 3,
        IEnumerable<Type>? retryableExceptions = null)
    {
        _initialDelay = initialDelay;
        _multiplier = multiplier;
        _maxDelay = maxDelay ?? TimeSpan.FromMinutes(5);
        _maxAttempts = maxAttempts;
        _retryableExceptions = retryableExceptions?.ToHashSet() ?? new HashSet<Type>();
    }

    public int MaxAttempts => _maxAttempts;

    public bool ShouldRetry(Exception exception, int attemptNumber)
    {
        if (attemptNumber >= _maxAttempts)
        {
            return false;
        }

        // If no specific exceptions are configured, retry all exceptions
        if (_retryableExceptions.Count == 0)
        {
            return true;
        }

        // Check if the exception type (or any base type) is in the retryable list
        var exceptionType = exception.GetType();
        return _retryableExceptions.Any(t => t.IsAssignableFrom(exceptionType));
    }

    public TimeSpan GetRetryDelay(int attemptNumber)
    {
        var delay = TimeSpan.FromMilliseconds(_initialDelay.TotalMilliseconds * Math.Pow(_multiplier, attemptNumber - 1));
        return delay > _maxDelay ? _maxDelay : delay;
    }
}








