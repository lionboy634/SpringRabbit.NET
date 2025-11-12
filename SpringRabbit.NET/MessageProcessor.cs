using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SpringRabbit.NET;

/// <summary>
/// Processes messages from RabbitMQ queues and invokes listener methods.
/// </summary>
public class MessageProcessor
{
    private readonly ConnectionManager _connectionManager;
    private readonly MessageConverterFactory _converterFactory;
    private readonly Metrics.MetricsCollector? _metricsCollector;
    private readonly ILogger<MessageProcessor>? _logger;
    private readonly List<ConsumerRegistration> _registrations = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public MessageProcessor(ConnectionManager connectionManager, MessageConverterFactory? converterFactory = null, Metrics.MetricsCollector? metricsCollector = null, ILogger<MessageProcessor>? logger = null)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _converterFactory = converterFactory ?? new MessageConverterFactory();
        _metricsCollector = metricsCollector;
        _logger = logger;
    }

    /// <summary>
    /// Registers a listener method to process messages from a queue.
    /// </summary>
    public void RegisterListener(ConsumerRegistration registration)
    {
        _registrations.Add(registration);
        _logger?.LogInformation("Registered listener for queue(s): {Queues}", string.Join(", ", registration.Queues));
    }

    /// <summary>
    /// Starts all registered listeners.
    /// </summary>
    public void StartAll()
    {
        foreach (var registration in _registrations)
        {
            StartListener(registration);
        }
    }

    /// <summary>
    /// Stops all listeners.
    /// </summary>
    public void StopAll()
    {
        _cancellationTokenSource.Cancel();
        _logger?.LogInformation("Stopped all listeners");
    }

    private void StartListener(ConsumerRegistration registration)
    {
        var (minConcurrency, maxConcurrency) = registration.Attribute.ParseConcurrency();
        var actualConcurrency = Math.Max(minConcurrency, Math.Min(maxConcurrency, Environment.ProcessorCount * 2));

        foreach (var queueName in registration.Queues)
        {
            _connectionManager.EnsureQueue(
                queueName,
                registration.Attribute.EnableDeadLetterQueue,
                registration.Attribute.MaxPriority);

            var channel = _connectionManager.GetChannel(queueName);
            channel.BasicQos(prefetchSize: 0, prefetchCount: registration.Attribute.PrefetchCount, global: false);

            for (int i = 0; i < actualConcurrency; i++)
            {
                var consumer = new EventingBasicConsumer(channel);
                var consumerTag = $"consumer-{queueName}-{i}";

                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var deliveryTag = ea.DeliveryTag;

                    // Create retry policy if retry is enabled
                    IRetryPolicy? retryPolicy = null;
                    if (registration.Attribute.MaxRetryAttempts > 0)
                    {
                        retryPolicy = new RetryPolicies.ExponentialBackoffRetryPolicy(
                            initialDelay: TimeSpan.FromMilliseconds(registration.Attribute.RetryInitialDelayMs),
                            multiplier: registration.Attribute.RetryMultiplier,
                            maxDelay: TimeSpan.FromMilliseconds(registration.Attribute.RetryMaxDelayMs),
                            maxAttempts: registration.Attribute.MaxRetryAttempts);
                    }

                    var attemptNumber = 0;
                    Exception? lastException = null;

                    while (true)
                    {
                        try
                        {
                            attemptNumber++;
                            var startTime = DateTime.UtcNow;
                            _logger?.LogDebug("Processing message from queue {Queue}, consumer {Consumer}, attempt {Attempt}", 
                                queueName, consumerTag, attemptNumber);

                            // Get content type from message properties
                            var contentType = ea.BasicProperties?.ContentType;
                            var converter = _converterFactory.GetConverter(contentType);

                            // Deserialize message using converter
                            object? deserializedMessage = null;
                            if (registration.MessageType != null)
                            {
                                deserializedMessage = converter.FromMessage(body, registration.MessageType, contentType);
                            }

                            // Invoke handler
                            var result = registration.InvokeHandler(deserializedMessage);

                            // Handle async methods
                            if (result is Task task)
                            {
                                await task;
                            }

                            // Acknowledge message on success
                            channel.BasicAck(deliveryTag, false);
                            var processingTime = DateTime.UtcNow - startTime;
                            _metricsCollector?.GetMetrics(queueName).RecordSuccess(processingTime);
                            _logger?.LogDebug("Message processed successfully from queue {Queue} on attempt {Attempt}", 
                                queueName, attemptNumber);
                            return; // Success, exit retry loop
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            _logger?.LogWarning(ex, "Error processing message from queue {Queue} on attempt {Attempt}", 
                                queueName, attemptNumber);

                            // Check if we should retry
                            if (retryPolicy != null && retryPolicy.ShouldRetry(ex, attemptNumber))
                            {
                                var delay = retryPolicy.GetRetryDelay(attemptNumber);
                                _metricsCollector?.GetMetrics(queueName).RecordRetry();
                                _logger?.LogInformation("Retrying message from queue {Queue} after {Delay}ms (attempt {Attempt}/{MaxAttempts})", 
                                    queueName, delay.TotalMilliseconds, attemptNumber, retryPolicy.MaxAttempts);
                                await Task.Delay(delay);
                                continue; // Retry
                            }

                            // No more retries or retry not enabled - use error handler
                            _metricsCollector?.GetMetrics(queueName).RecordFailure();
                            var errorHandler = registration.ErrorHandler ?? new ErrorHandlers.DefaultErrorHandler();
                            var shouldAck = errorHandler.HandleError(ex, body, deliveryTag, channel);
                            if (shouldAck)
                            {
                                channel.BasicAck(deliveryTag, false);
                                _logger?.LogInformation("Error handler acknowledged message from queue {Queue} after {Attempt} attempt(s)", 
                                    queueName, attemptNumber);
                            }
                            return; // Exit retry loop
                        }
                    }
                };

                channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
                _logger?.LogInformation("Started consumer {Consumer} for queue {Queue} (concurrency: {Concurrency})", consumerTag, queueName, actualConcurrency);
            }
        }
    }
}

/// <summary>
/// Represents a registered consumer with its handler method.
/// </summary>
public class ConsumerRegistration
{
    public string[] Queues { get; set; } = Array.Empty<string>();
    public RabbitListenerAttribute Attribute { get; set; } = null!;
    public Type? MessageType { get; set; }
    public Func<object?, object?> InvokeHandler { get; set; } = null!;
    public IErrorHandler? ErrorHandler { get; set; }
}

