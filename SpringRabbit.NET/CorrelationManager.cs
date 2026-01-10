using System.Collections.Concurrent;

namespace SpringRabbit.NET;

/// <summary>
/// Manages correlation IDs for request/reply messaging.
/// </summary>
public class CorrelationManager
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _pendingRequests = new();
    private readonly Timer _cleanupTimer;

    public CorrelationManager()
    {
        // Cleanup old pending requests every 5 minutes
        _cleanupTimer = new Timer(_ => CleanupExpiredRequests(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Creates a new correlation ID and registers a pending request.
    /// </summary>
    public string RegisterRequest(TimeSpan timeout, out TaskCompletionSource<byte[]> tcs)
    {
        var correlationId = Guid.NewGuid().ToString();
        tcs = new TaskCompletionSource<byte[]>();
        
        // Set timeout
        var timeoutTask = Task.Delay(timeout).ContinueWith(_ =>
        {
            if (_pendingRequests.TryRemove(correlationId, out var removedTcs))
            {
                removedTcs.TrySetException(new TimeoutException($"Request with correlation ID {correlationId} timed out after {timeout.TotalSeconds} seconds"));
            }
        });

        _pendingRequests[correlationId] = tcs;
        return correlationId;
    }

    /// <summary>
    /// Completes a pending request with the response.
    /// </summary>
    public bool CompleteRequest(string correlationId, byte[] response)
    {
        if (_pendingRequests.TryRemove(correlationId, out var tcs))
        {
            return tcs.TrySetResult(response);
        }
        return false;
    }

    /// <summary>
    /// Cancels a pending request.
    /// </summary>
    public bool CancelRequest(string correlationId)
    {
        if (_pendingRequests.TryRemove(correlationId, out var tcs))
        {
            return tcs.TrySetCanceled();
        }
        return false;
    }

    private void CleanupExpiredRequests()
    {
        // Remove any requests that have been pending for more than 1 hour
        var expiredKeys = _pendingRequests.Keys
            .Where(key => _pendingRequests.TryGetValue(key, out var tcs) && tcs.Task.IsCompleted)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _pendingRequests.TryRemove(key, out _);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}








