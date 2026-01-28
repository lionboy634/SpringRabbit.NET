namespace SpringRabbit.NET.Exceptions;

/// <summary>
/// Base exception for all SpringRabbit.NET errors.
/// </summary>
public class RabbitException : Exception
{
    public RabbitException(string message) : base(message) { }
    public RabbitException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a connection to RabbitMQ cannot be established or is lost.
/// </summary>
public class RabbitConnectionException : RabbitException
{
    public string? HostName { get; }
    public int? Port { get; }

    public RabbitConnectionException(string message, string? hostName = null, int? port = null) 
        : base(message)
    {
        HostName = hostName;
        Port = port;
    }

    public RabbitConnectionException(string message, Exception innerException, string? hostName = null, int? port = null) 
        : base(message, innerException)
    {
        HostName = hostName;
        Port = port;
    }
}

/// <summary>
/// Thrown when a queue does not exist or cannot be accessed.
/// </summary>
public class QueueNotFoundException : RabbitException
{
    public string QueueName { get; }

    public QueueNotFoundException(string queueName) 
        : base($"Queue '{queueName}' was not found or could not be accessed.")
    {
        QueueName = queueName;
    }

    public QueueNotFoundException(string queueName, Exception innerException) 
        : base($"Queue '{queueName}' was not found or could not be accessed.", innerException)
    {
        QueueName = queueName;
    }
}

/// <summary>
/// Thrown when queue declaration fails due to argument mismatch with existing queue.
/// </summary>
public class QueueDeclarationException : RabbitException
{
    public string QueueName { get; }
    public string? MismatchedArgument { get; }

    public QueueDeclarationException(string queueName, string? mismatchedArgument = null) 
        : base($"Failed to declare queue '{queueName}'. {(mismatchedArgument != null ? $"Argument mismatch: {mismatchedArgument}" : "Queue may exist with different configuration.")}")
    {
        QueueName = queueName;
        MismatchedArgument = mismatchedArgument;
    }

    public QueueDeclarationException(string queueName, Exception innerException, string? mismatchedArgument = null) 
        : base($"Failed to declare queue '{queueName}'.", innerException)
    {
        QueueName = queueName;
        MismatchedArgument = mismatchedArgument;
    }
}

/// <summary>
/// Thrown when a message cannot be deserialized to the expected type.
/// </summary>
public class MessageDeserializationException : RabbitException
{
    public Type TargetType { get; }
    public string? ContentType { get; }
    public byte[]? RawBody { get; }

    public MessageDeserializationException(Type targetType, string? contentType, byte[]? rawBody = null) 
        : base($"Failed to deserialize message to type '{targetType.Name}'. Content-Type: {contentType ?? "unknown"}")
    {
        TargetType = targetType;
        ContentType = contentType;
        RawBody = rawBody;
    }

    public MessageDeserializationException(Type targetType, string? contentType, Exception innerException, byte[]? rawBody = null) 
        : base($"Failed to deserialize message to type '{targetType.Name}'. Content-Type: {contentType ?? "unknown"}", innerException)
    {
        TargetType = targetType;
        ContentType = contentType;
        RawBody = rawBody;
    }
}

/// <summary>
/// Thrown when a message handler method fails during invocation.
/// </summary>
public class HandlerInvocationException : RabbitException
{
    public string QueueName { get; }
    public string? HandlerName { get; }
    public int AttemptNumber { get; }

    public HandlerInvocationException(string queueName, string? handlerName, int attemptNumber, Exception innerException) 
        : base($"Handler '{handlerName ?? "unknown"}' failed processing message from queue '{queueName}' on attempt {attemptNumber}.", innerException)
    {
        QueueName = queueName;
        HandlerName = handlerName;
        AttemptNumber = attemptNumber;
    }
}

/// <summary>
/// Thrown when no handler is found for a message type.
/// </summary>
public class NoHandlerFoundException : RabbitException
{
    public Type MessageType { get; }
    public string QueueName { get; }

    public NoHandlerFoundException(Type messageType, string queueName) 
        : base($"No handler found for message type '{messageType.Name}' on queue '{queueName}'.")
    {
        MessageType = messageType;
        QueueName = queueName;
    }
}

/// <summary>
/// Thrown when a listener method is incorrectly configured.
/// </summary>
public class ListenerConfigurationException : RabbitException
{
    public string MethodName { get; }
    public string? ClassName { get; }

    public ListenerConfigurationException(string methodName, string? className, string reason) 
        : base($"Listener method '{className}.{methodName}' is incorrectly configured: {reason}")
    {
        MethodName = methodName;
        ClassName = className;
    }
}

/// <summary>
/// Thrown when a message publish operation fails.
/// </summary>
public class MessagePublishException : RabbitException
{
    public string? Exchange { get; }
    public string? RoutingKey { get; }

    public MessagePublishException(string? exchange, string? routingKey, string message) 
        : base(message)
    {
        Exchange = exchange;
        RoutingKey = routingKey;
    }

    public MessagePublishException(string? exchange, string? routingKey, Exception innerException) 
        : base($"Failed to publish message to exchange '{exchange ?? "(default)"}' with routing key '{routingKey}'.", innerException)
    {
        Exchange = exchange;
        RoutingKey = routingKey;
    }
}

/// <summary>
/// Thrown when an RPC (request/reply) operation times out.
/// </summary>
public class RpcTimeoutException : RabbitException
{
    public string QueueName { get; }
    public TimeSpan Timeout { get; }
    public string? CorrelationId { get; }

    public RpcTimeoutException(string queueName, TimeSpan timeout, string? correlationId = null) 
        : base($"RPC request to queue '{queueName}' timed out after {timeout.TotalSeconds:F1} seconds. Correlation ID: {correlationId ?? "unknown"}")
    {
        QueueName = queueName;
        Timeout = timeout;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Thrown when message acknowledgment fails.
/// </summary>
public class AcknowledgmentException : RabbitException
{
    public ulong DeliveryTag { get; }
    public string QueueName { get; }

    public AcknowledgmentException(ulong deliveryTag, string queueName, Exception innerException) 
        : base($"Failed to acknowledge message (delivery tag: {deliveryTag}) from queue '{queueName}'.", innerException)
    {
        DeliveryTag = deliveryTag;
        QueueName = queueName;
    }
}

/// <summary>
/// Thrown when the channel is closed unexpectedly.
/// </summary>
public class ChannelClosedException : RabbitException
{
    public string? QueueName { get; }
    public int? ReplyCode { get; }
    public string? ReplyText { get; }

    public ChannelClosedException(string? queueName, int? replyCode = null, string? replyText = null) 
        : base($"Channel for queue '{queueName ?? "unknown"}' was closed. Code: {replyCode}, Reason: {replyText ?? "unknown"}")
    {
        QueueName = queueName;
        ReplyCode = replyCode;
        ReplyText = replyText;
    }

    public ChannelClosedException(string? queueName, Exception innerException) 
        : base($"Channel for queue '{queueName ?? "unknown"}' was closed unexpectedly.", innerException)
    {
        QueueName = queueName;
    }
}
