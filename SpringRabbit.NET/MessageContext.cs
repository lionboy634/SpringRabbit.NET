using RabbitMQ.Client;

namespace SpringRabbit.NET;

/// <summary>
/// Provides access to message metadata, headers, and delivery information.
/// Can be injected as a parameter in RabbitListener methods.
/// </summary>
/// <example>
/// <code>
/// [RabbitListener("orders.queue")]
/// public async Task HandleOrder(OrderMessage order, MessageContext context)
/// {
///     var correlationId = context.CorrelationId;
///     var priority = context.Priority;
///     var customHeader = context.GetHeader&lt;string&gt;("x-custom-header");
/// }
/// </code>
/// </example>
public class MessageContext
{
    /// <summary>
    /// The raw message body as bytes.
    /// </summary>
    public byte[] Body { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// The queue name this message was consumed from.
    /// </summary>
    public string QueueName { get; init; } = string.Empty;

    /// <summary>
    /// The exchange the message was published to.
    /// </summary>
    public string Exchange { get; init; } = string.Empty;

    /// <summary>
    /// The routing key used when publishing.
    /// </summary>
    public string RoutingKey { get; init; } = string.Empty;

    /// <summary>
    /// The delivery tag for this message (used for ack/nack).
    /// </summary>
    public ulong DeliveryTag { get; init; }

    /// <summary>
    /// Whether this message was redelivered.
    /// </summary>
    public bool Redelivered { get; init; }

    /// <summary>
    /// The consumer tag that received this message.
    /// </summary>
    public string ConsumerTag { get; init; } = string.Empty;

    /// <summary>
    /// Message content type (e.g., "application/json").
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Content encoding (e.g., "utf-8").
    /// </summary>
    public string? ContentEncoding { get; init; }

    /// <summary>
    /// Correlation ID for request/reply patterns.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Reply-to queue name for RPC patterns.
    /// </summary>
    public string? ReplyTo { get; init; }

    /// <summary>
    /// Message ID.
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Message timestamp.
    /// </summary>
    public DateTime? Timestamp { get; init; }

    /// <summary>
    /// Message type.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// User ID.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Application ID.
    /// </summary>
    public string? AppId { get; init; }

    /// <summary>
    /// Message priority (0-255).
    /// </summary>
    public byte? Priority { get; init; }

    /// <summary>
    /// Message expiration.
    /// </summary>
    public string? Expiration { get; init; }

    /// <summary>
    /// Whether the message is persistent.
    /// </summary>
    public bool Persistent { get; init; }

    /// <summary>
    /// Raw message headers dictionary.
    /// </summary>
    public IDictionary<string, object>? Headers { get; init; }

    /// <summary>
    /// The original basic properties from RabbitMQ.
    /// </summary>
    public IBasicProperties? BasicProperties { get; init; }

    /// <summary>
    /// Gets a header value by key, with type conversion.
    /// </summary>
    /// <typeparam name="T">The expected type of the header value.</typeparam>
    /// <param name="key">The header key.</param>
    /// <param name="defaultValue">Default value if header not found.</param>
    /// <returns>The header value or default.</returns>
    public T? GetHeader<T>(string key, T? defaultValue = default)
    {
        if (Headers == null || !Headers.TryGetValue(key, out var value))
            return defaultValue;

        if (value is T typedValue)
            return typedValue;

        // Handle byte[] to string conversion (common in RabbitMQ headers)
        if (typeof(T) == typeof(string) && value is byte[] bytes)
            return (T)(object)System.Text.Encoding.UTF8.GetString(bytes);

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Checks if a header exists.
    /// </summary>
    public bool HasHeader(string key) => Headers?.ContainsKey(key) == true;

    /// <summary>
    /// Creates a MessageContext from RabbitMQ delivery event args.
    /// </summary>
    internal static MessageContext FromDeliveryEventArgs(
        RabbitMQ.Client.Events.BasicDeliverEventArgs ea,
        string queueName,
        string consumerTag)
    {
        var props = ea.BasicProperties;
        
        return new MessageContext
        {
            Body = ea.Body.ToArray(),
            QueueName = queueName,
            Exchange = ea.Exchange,
            RoutingKey = ea.RoutingKey,
            DeliveryTag = ea.DeliveryTag,
            Redelivered = ea.Redelivered,
            ConsumerTag = consumerTag,
            ContentType = props?.ContentType,
            ContentEncoding = props?.ContentEncoding,
            CorrelationId = props?.CorrelationId,
            ReplyTo = props?.ReplyTo,
            MessageId = props?.MessageId,
            Timestamp = props?.Timestamp.UnixTime > 0 
                ? DateTimeOffset.FromUnixTimeSeconds(props.Timestamp.UnixTime).UtcDateTime 
                : null,
            Type = props?.Type,
            UserId = props?.UserId,
            AppId = props?.AppId,
            Priority = props?.Priority,
            Expiration = props?.Expiration,
            Persistent = props?.DeliveryMode == 2,
            Headers = props?.Headers,
            BasicProperties = props
        };
    }
}
