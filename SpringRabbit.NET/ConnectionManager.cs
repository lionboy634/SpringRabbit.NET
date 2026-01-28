using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace SpringRabbit.NET;

/// <summary>
/// Manages RabbitMQ connections and channels with automatic reconnection support.
/// </summary>
public class ConnectionManager : IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<ConnectionManager>? _logger;
    private IConnection? _connection;
    private readonly ConcurrentDictionary<string, IModel> _channels = new();
    private readonly object _lock = new();
    private bool _disposed = false;

    public ConnectionManager(IConnectionFactory connectionFactory, ILogger<ConnectionManager>? logger = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a connection to RabbitMQ.
    /// </summary>
    public IConnection GetConnection()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ConnectionManager));

        if (_connection?.IsOpen == true)
            return _connection;

        lock (_lock)
        {
            if (_connection?.IsOpen == true)
                return _connection;

            try
            {
                _connection = _connectionFactory.CreateConnection();
                _connection.ConnectionShutdown += (sender, args) =>
                {
                    _logger?.LogWarning("RabbitMQ connection shut down: {Reason}", args.ReplyText);
                    _channels.Clear();
                };

                _logger?.LogInformation("RabbitMQ connection established");
                return _connection;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create RabbitMQ connection");
                throw;
            }
        }
    }

    /// <summary>
    /// Gets or creates a channel for the specified queue.
    /// </summary>
    public IModel GetChannel(string queueName)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ConnectionManager));

        return _channels.GetOrAdd(queueName, _ =>
        {
            var connection = GetConnection();
            var channel = connection.CreateModel();
            channel.ModelShutdown += (sender, args) =>
            {
                _logger?.LogWarning("Channel for queue {Queue} shut down: {Reason}", queueName, args.ReplyText);
                _channels.TryRemove(queueName, out IModel? _);
            };
            return channel;
        });
    }

    /// <summary>
    /// Ensures a queue exists with optional DLQ and priority support.
    /// </summary>
    public void EnsureQueue(string queueName, bool enableDlq = true, byte maxPriority = 0)
    {
        EnsureQueue(queueName, new QueueOptions
        {
            EnableDeadLetterQueue = enableDlq,
            MaxPriority = maxPriority
        });
    }

    /// <summary>
    /// Ensures a queue exists with advanced options.
    /// Uses passive declare first to check if the queue already exists.
    /// If it exists, uses it as-is to avoid PRECONDITION_FAILED errors when
    /// the queue was created with different arguments by another service.
    /// </summary>
    public void EnsureQueue(string queueName, QueueOptions options)
    {
        var channel = GetChannel(queueName);

        // First, ensure DLQ exists if enabled (DLQs are simple queues, less likely to have conflicts)
        if (options.EnableDeadLetterQueue)
        {
            var dlqName = $"{queueName}.dlq";
            EnsureQueueExists(dlqName, channel, null);
        }

        // Build arguments for the main queue
        var arguments = new Dictionary<string, object>();

        if (options.EnableDeadLetterQueue)
        {
            var dlqName = $"{queueName}.dlq";
            arguments["x-dead-letter-exchange"] = "";
            arguments["x-dead-letter-routing-key"] = dlqName;
        }

        if (options.MaxPriority > 0)
        {
            arguments["x-max-priority"] = options.MaxPriority;
        }

        if (options.MessageTtl.HasValue)
        {
            arguments["x-message-ttl"] = (int)options.MessageTtl.Value.TotalMilliseconds;
        }

        if (options.Lazy)
        {
            arguments["x-queue-mode"] = "lazy";
        }

        if (options.Quorum)
        {
            arguments["x-queue-type"] = "quorum";
        }

        // Add custom arguments
        if (options.Arguments != null)
        {
            foreach (var arg in options.Arguments)
            {
                arguments[arg.Key] = arg.Value;
            }
        }

        // Try to ensure the queue exists, handling pre-existing queues gracefully
        if (EnsureQueueExists(queueName, channel, arguments.Count > 0 ? arguments : null))
        {
            _logger?.LogDebug("Queue {Queue} ensured with DLQ={Dlq}, MaxPriority={Priority}, TTL={Ttl}, Lazy={Lazy}, Quorum={Quorum}", 
                queueName, options.EnableDeadLetterQueue, options.MaxPriority, options.MessageTtl, options.Lazy, options.Quorum);
        }
        else
        {
            _logger?.LogDebug("Queue {Queue} already exists, using existing configuration", queueName);
        }
    }

    /// <summary>
    /// Ensures a queue exists, using passive declare to check first.
    /// Returns true if the queue was created, false if it already existed.
    /// </summary>
    private bool EnsureQueueExists(string queueName, IModel channel, IDictionary<string, object>? arguments)
    {
        try
        {
            // Try passive declare first - this checks if queue exists without modifying it
            channel.QueueDeclarePassive(queueName);
            return false; // Queue already exists
        }
        catch (RabbitMQ.Client.Exceptions.OperationInterruptedException)
        {
            // Queue doesn't exist - channel is now closed, need to recreate it
            _channels.TryRemove(queueName, out _);
            var newChannel = GetChannel(queueName);
            
            // Now declare the queue with our arguments
            newChannel.QueueDeclare(
                queue: queueName, 
                durable: true, 
                exclusive: false, 
                autoDelete: false, 
                arguments: arguments);
            
            return true; // Queue was created
        }
    }

    /// <summary>
    /// Declares an exchange using the provided builder.
    /// </summary>
    public void DeclareExchange(ExchangeBuilder builder)
    {
        var channel = GetChannel("_exchange_channel");
        builder.Declare(channel);
        _logger?.LogDebug("Exchange declared: {Exchange}", builder);
    }

    /// <summary>
    /// Declares a binding using the provided builder.
    /// </summary>
    public void DeclareBinding(BindingBuilder builder)
    {
        var channel = GetChannel("_binding_channel");
        builder.Declare(channel);
        _logger?.LogDebug("Binding declared: {Binding}", builder);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var channel in _channels.Values)
        {
            try
            {
                if (channel.IsOpen)
                    channel.Close();
                channel.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error closing channel");
            }
        }
        _channels.Clear();

        try
        {
            if (_connection?.IsOpen == true)
                _connection.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error closing connection");
        }

        _logger?.LogInformation("Connection manager disposed");
    }
}

