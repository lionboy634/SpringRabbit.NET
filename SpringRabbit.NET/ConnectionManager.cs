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
    /// </summary>
    public void EnsureQueue(string queueName, QueueOptions options)
    {
        var channel = GetChannel(queueName);
        var arguments = new Dictionary<string, object>();

        if (options.EnableDeadLetterQueue)
        {
            var dlqName = $"{queueName}.dlq";
            arguments["x-dead-letter-exchange"] = "";
            arguments["x-dead-letter-routing-key"] = dlqName;

            // Ensure DLQ exists
            channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false, arguments: null);
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

        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);
        _logger?.LogDebug("Queue {Queue} ensured with DLQ={Dlq}, MaxPriority={Priority}, TTL={Ttl}, Lazy={Lazy}, Quorum={Quorum}", 
            queueName, options.EnableDeadLetterQueue, options.MaxPriority, options.MessageTtl, options.Lazy, options.Quorum);
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

