using RabbitMQ.Client;

namespace SpringRabbit.NET;

/// <summary>
/// Builder for creating RabbitMQ exchanges.
/// </summary>
public class ExchangeBuilder
{
    private string _name = string.Empty;
    private ExchangeType _type = ExchangeType.Direct;
    private bool _durable = true;
    private bool _autoDelete = false;
    private Dictionary<string, object> _arguments = new();

    /// <summary>
    /// Sets the exchange name.
    /// </summary>
    public ExchangeBuilder Name(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the exchange type.
    /// </summary>
    public ExchangeBuilder Type(ExchangeType type)
    {
        _type = type;
        return this;
    }

    /// <summary>
    /// Sets whether the exchange is durable (survives broker restart).
    /// </summary>
    public ExchangeBuilder Durable(bool durable = true)
    {
        _durable = durable;
        return this;
    }

    /// <summary>
    /// Sets whether the exchange is auto-deleted when no longer in use.
    /// </summary>
    public ExchangeBuilder AutoDelete(bool autoDelete = true)
    {
        _autoDelete = autoDelete;
        return this;
    }

    /// <summary>
    /// Adds an argument to the exchange.
    /// </summary>
    public ExchangeBuilder Argument(string key, object value)
    {
        _arguments[key] = value;
        return this;
    }

    /// <summary>
    /// Builds and declares the exchange on the given channel.
    /// </summary>
    public void Declare(IModel channel)
    {
        if (string.IsNullOrEmpty(_name))
        {
            throw new InvalidOperationException("Exchange name must be specified");
        }

        var typeString = _type switch
        {
            ExchangeType.Direct => "direct",
            ExchangeType.Topic => "topic",
            ExchangeType.Fanout => "fanout",
            ExchangeType.Headers => "headers",
            _ => "direct"
        };

        channel.ExchangeDeclare(_name, typeString, _durable, _autoDelete, _arguments);
    }
}

