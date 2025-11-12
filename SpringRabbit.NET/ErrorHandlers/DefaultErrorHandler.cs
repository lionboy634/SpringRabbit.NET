using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace SpringRabbit.NET.ErrorHandlers;

/// <summary>
/// Default error handler that rejects messages and sends them to DLQ.
/// </summary>
public class DefaultErrorHandler : IErrorHandler
{
    private readonly ILogger<DefaultErrorHandler>? _logger;

    public DefaultErrorHandler(ILogger<DefaultErrorHandler>? logger = null)
    {
        _logger = logger;
    }

    public bool HandleError(Exception exception, byte[] messageBody, ulong deliveryTag, IModel channel)
    {
        _logger?.LogError(exception, "Error processing message with delivery tag {DeliveryTag}. Rejecting and sending to DLQ.", deliveryTag);
        channel.BasicNack(deliveryTag, false, false);
        return false; // Message rejected
    }
}


