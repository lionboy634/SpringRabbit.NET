namespace SpringRabbit.NET.RetryPolicies;

/// <summary>
/// Simple retry policy with fixed delay between retries.
/// </summary>
public class SimpleRetryPolicy : IRetryPolicy
{
    private readonly TimeSpan _delay;
    private readonly int _maxAttempts;
    private readonly HashSet<Type> _retryableExceptions;

    public SimpleRetryPolicy(
        TimeSpan delay,
        int maxAttempts = 3,
        IEnumerable<Type>? retryableExceptions = null)
    {
        _delay = delay;
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

        if (_retryableExceptions.Count == 0)
        {
            return true;
        }

        var exceptionType = exception.GetType();
        return _retryableExceptions.Any(t => t.IsAssignableFrom(exceptionType));
    }

    public TimeSpan GetRetryDelay(int attemptNumber)
    {
        return _delay;
    }
}


