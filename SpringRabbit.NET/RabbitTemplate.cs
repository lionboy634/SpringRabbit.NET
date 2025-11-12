using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace SpringRabbit.NET;

/// <summary>
/// Template for sending messages to RabbitMQ queues, similar to Spring's RabbitTemplate.
/// </summary>
public class RabbitTemplate
{
    private readonly ConnectionManager _connectionManager;
    private readonly MessageConverterFactory _converterFactory;
    private readonly CorrelationManager _correlationManager;
    private readonly ILogger<RabbitTemplate>? _logger;
    private string? _replyQueueName;
    private string? _replyConsumerTag;

    public RabbitTemplate(ConnectionManager connectionManager, MessageConverterFactory? converterFactory = null, CorrelationManager? correlationManager = null, ILogger<RabbitTemplate>? logger = null)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _converterFactory = converterFactory ?? new MessageConverterFactory();
        _correlationManager = correlationManager ?? new CorrelationManager();
        _logger = logger;
    }

    /// <summary>
    /// Sends a message to the specified queue.
    /// </summary>
    public bool Send(string queueName, object message, byte? priority = null, string? contentType = null)
    {
        try
        {
            var channel = _connectionManager.GetChannel(queueName);
            var converter = _converterFactory.GetConverter(contentType);
            var body = converter.ToMessage(message, out var actualContentType);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = actualContentType;

            if (priority.HasValue)
            {
                properties.Priority = priority.Value;
            }

            channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: properties,
                body: body);

            _logger?.LogDebug("Sent message to queue {Queue} with content type {ContentType}", queueName, actualContentType);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send message to queue {Queue}", queueName);
            return false;
        }
    }

    /// <summary>
    /// Sends a message asynchronously.
    /// </summary>
    public async Task<bool> SendAsync(string queueName, object message, byte? priority = null, string? contentType = null)
    {
        return await Task.Run(() => Send(queueName, message, priority, contentType));
    }

    /// <summary>
    /// Sends a message and waits for a reply (RPC pattern).
    /// </summary>
    public async Task<TResponse?> SendAndReceiveAsync<TRequest, TResponse>(
        string queueName, 
        TRequest request, 
        TimeSpan timeout, 
        string? contentType = null)
    {
        EnsureReplyQueue();

        var correlationId = _correlationManager.RegisterRequest(timeout, out var tcs);
        var channel = _connectionManager.GetChannel(queueName);
        var converter = _converterFactory.GetConverter(contentType);
        var body = converter.ToMessage(request!, out var actualContentType);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = actualContentType;
        properties.CorrelationId = correlationId;
        properties.ReplyTo = _replyQueueName;

        try
        {
            channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: properties,
                body: body);

            _logger?.LogDebug("Sent RPC request to queue {Queue} with correlation ID {CorrelationId}", queueName, correlationId);

            // Wait for response
            var responseBody = await tcs.Task;
            var responseConverter = _converterFactory.GetConverter(actualContentType);
            return (TResponse?)responseConverter.FromMessage(responseBody, typeof(TResponse), actualContentType);
        }
        catch (TimeoutException)
        {
            _logger?.LogWarning("RPC request to queue {Queue} with correlation ID {CorrelationId} timed out", queueName, correlationId);
            throw;
        }
        catch (Exception ex)
        {
            _correlationManager.CancelRequest(correlationId);
            _logger?.LogError(ex, "Failed to send RPC request to queue {Queue}", queueName);
            throw;
        }
    }

    /// <summary>
    /// Sends a message and waits for a reply (RPC pattern) with default timeout.
    /// </summary>
    public async Task<TResponse?> SendAndReceiveAsync<TRequest, TResponse>(
        string queueName, 
        TRequest request, 
        string? contentType = null)
    {
        return await SendAndReceiveAsync<TRequest, TResponse>(queueName, request, TimeSpan.FromSeconds(30), contentType);
    }

    private void EnsureReplyQueue()
    {
        if (_replyQueueName != null && _replyConsumerTag != null)
        {
            return; // Already set up
        }

        var channel = _connectionManager.GetChannel("_reply_queue");
        _replyQueueName = channel.QueueDeclare(exclusive: true, autoDelete: true).QueueName;

        var consumer = new RabbitMQ.Client.Events.EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var correlationId = ea.BasicProperties.CorrelationId;
            if (!string.IsNullOrEmpty(correlationId))
            {
                var body = ea.Body.ToArray();
                _correlationManager.CompleteRequest(correlationId, body);
                _logger?.LogDebug("Received reply with correlation ID {CorrelationId}", correlationId);
            }
        };

        _replyConsumerTag = channel.BasicConsume(_replyQueueName, autoAck: true, consumer: consumer);
        _logger?.LogDebug("Set up reply queue: {ReplyQueue}", _replyQueueName);
    }
}

