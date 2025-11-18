using RabbitMQ.Client;

namespace SpringRabbit.NET;

/// <summary>
/// Builder for creating RabbitMQ bindings between queues/exchanges.
/// </summary>
public class BindingBuilder
{
    private string _destination = string.Empty;
    private string _source = string.Empty;
    private string _routingKey = string.Empty;
    private Dictionary<string, object> _arguments = new();
    private bool _isQueueBinding = true;

    /// <summary>
    /// Creates a binding from a queue to an exchange.
    /// </summary>
    public static BindingBuilder BindQueue(string queueName)
    {
        return new BindingBuilder
        {
            _destination = queueName,
            _isQueueBinding = true
        };
    }

    /// <summary>
    /// Creates a binding from an exchange to another exchange.
    /// </summary>
    public static BindingBuilder BindExchange(string exchangeName)
    {
        return new BindingBuilder
        {
            _destination = exchangeName,
            _isQueueBinding = false
        };
    }

    /// <summary>
    /// Sets the source exchange to bind to.
    /// </summary>
    public BindingBuilder To(string exchangeName)
    {
        _source = exchangeName;
        return this;
    }

    /// <summary>
    /// Sets the routing key for the binding.
    /// </summary>
    public BindingBuilder WithRoutingKey(string routingKey)
    {
        _routingKey = routingKey;
        return this;
    }

    /// <summary>
    /// Adds an argument to the binding (useful for headers exchange).
    /// </summary>
    public BindingBuilder Argument(string key, object value)
    {
        _arguments[key] = value;
        return this;
    }

    /// <summary>
    /// Builds and declares the binding on the given channel.
    /// </summary>
    public void Declare(IModel channel)
    {
        if (string.IsNullOrEmpty(_source))
        {
            throw new InvalidOperationException("Source exchange must be specified");
        }

        if (string.IsNullOrEmpty(_destination))
        {
            throw new InvalidOperationException("Destination queue or exchange must be specified");
        }

        if (_isQueueBinding)
        {
            channel.QueueBind(_destination, _source, _routingKey, _arguments);
        }
        else
        {
            channel.ExchangeBind(_destination, _source, _routingKey, _arguments);
        }
    }
}







