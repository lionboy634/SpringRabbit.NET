using RabbitMQ.Client;

namespace SpringRabbit.NET;

/// <summary>
/// Interface for handling errors during message processing.
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Handles an error that occurred during message processing.
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="messageBody">The raw message body</param>
    /// <param name="deliveryTag">The delivery tag of the message</param>
    /// <param name="channel">The channel the message was received on</param>
    /// <returns>True if the message should be acknowledged, false if it should be rejected</returns>
    bool HandleError(Exception exception, byte[] messageBody, ulong deliveryTag, IModel channel);
}








