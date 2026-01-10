using System.Collections.Concurrent;
using RabbitMQ.Client;

namespace SpringRabbit.NET.Testing;

/// <summary>
/// Mock RabbitMQ implementation for testing without a real RabbitMQ server.
/// </summary>
public class MockRabbitMQ : IDisposable
{
    private readonly ConcurrentDictionary<string, Queue<byte[]>> _queues = new();
    private readonly ConcurrentDictionary<string, List<Action<byte[], IBasicProperties>>> _consumers = new();
    private readonly object _lock = new();

    /// <summary>
    /// Publishes a message to a queue.
    /// </summary>
    public void Publish(string queueName, byte[] body, IBasicProperties? properties = null)
    {
        if (!_queues.ContainsKey(queueName))
        {
            _queues[queueName] = new Queue<byte[]>();
        }

        _queues[queueName].Enqueue(body);

        // Notify consumers
        if (_consumers.TryGetValue(queueName, out var consumerList))
        {
            lock (_lock)
            {
                foreach (var consumer in consumerList)
                {
                    consumer(body, properties ?? new MockBasicProperties());
                }
            }
        }
    }

    /// <summary>
    /// Consumes messages from a queue.
    /// </summary>
    public void Consume(string queueName, Action<byte[], IBasicProperties> handler)
    {
        if (!_consumers.ContainsKey(queueName))
        {
            _consumers[queueName] = new List<Action<byte[], IBasicProperties>>();
        }

        _consumers[queueName].Add(handler);

        // Process any existing messages
        if (_queues.TryGetValue(queueName, out var queue))
        {
            while (queue.Count > 0)
            {
                var message = queue.Dequeue();
                handler(message, new MockBasicProperties());
            }
        }
    }

    /// <summary>
    /// Gets the message count in a queue.
    /// </summary>
    public int GetMessageCount(string queueName)
    {
        return _queues.TryGetValue(queueName, out var queue) ? queue.Count : 0;
    }

    /// <summary>
    /// Clears all queues and consumers.
    /// </summary>
    public void Clear()
    {
        _queues.Clear();
        _consumers.Clear();
    }

    public void Dispose()
    {
        Clear();
    }

    private class MockBasicProperties : IBasicProperties
    {
        public string? AppId { get; set; }
        public string? ClusterId { get; set; }
        public string? ContentEncoding { get; set; }
        public string? ContentType { get; set; }
        public string? CorrelationId { get; set; }
        public byte DeliveryMode { get; set; }
        public string? Expiration { get; set; }
        public IDictionary<string, object> Headers { get; set; } = new Dictionary<string, object>();
        public string? MessageId { get; set; }
        public bool Persistent { get; set; }
        public byte Priority { get; set; }
        public string? ReplyTo { get; set; }
        public PublicationAddress? ReplyToAddress { get; set; }
        public AmqpTimestamp Timestamp { get; set; }
        public string? Type { get; set; }
        public string? UserId { get; set; }
        public ushort ProtocolClassId => 60;
        public string ProtocolClassName => "basic";
        public void ClearAppId() => AppId = null;
        public void ClearClusterId() => ClusterId = null;
        public void ClearContentEncoding() => ContentEncoding = null;
        public void ClearContentType() => ContentType = null;
        public void ClearCorrelationId() => CorrelationId = null;
        public void ClearDeliveryMode() => DeliveryMode = 0;
        public void ClearExpiration() => Expiration = null;
        public void ClearHeaders() => Headers.Clear();
        public void ClearMessageId() => MessageId = null;
        public void ClearPriority() => Priority = 0;
        public void ClearReplyTo() => ReplyTo = null;
        public void ClearReplyToAddress() => ReplyToAddress = null;
        public void ClearTimestamp() => Timestamp = default;
        public void ClearType() => Type = null;
        public void ClearUserId() => UserId = null;
        public bool IsAppIdPresent() => AppId != null;
        public bool IsClusterIdPresent() => ClusterId != null;
        public bool IsContentEncodingPresent() => ContentEncoding != null;
        public bool IsContentTypePresent() => ContentType != null;
        public bool IsCorrelationIdPresent() => CorrelationId != null;
        public bool IsDeliveryModePresent() => DeliveryMode != 0;
        public bool IsExpirationPresent() => Expiration != null;
        public bool IsHeadersPresent() => Headers.Count > 0;
        public bool IsMessageIdPresent() => MessageId != null;
        public bool IsPriorityPresent() => Priority != 0;
        public bool IsReplyToPresent() => ReplyTo != null;
        public bool IsReplyToAddressPresent() => ReplyToAddress != null;
        public bool IsTimestampPresent() => Timestamp.UnixTime != 0;
        public bool IsTypePresent() => Type != null;
        public bool IsUserIdPresent() => UserId != null;
    }
}








