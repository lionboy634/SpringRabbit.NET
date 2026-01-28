namespace SpringRabbit.NET;

/// <summary>
/// Specifies that the method return value should be automatically sent to a destination queue.
/// Mimics Spring AMQP's @SendTo annotation.
/// </summary>
/// <example>
/// <code>
/// [RabbitListener("request.queue")]
/// [SendTo("response.queue")]
/// public OrderResponse HandleOrder(OrderRequest request)
/// {
///     // The returned OrderResponse is automatically sent to "response.queue"
///     return new OrderResponse { Status = "Processed" };
/// }
/// 
/// // Dynamic routing based on message content:
/// [RabbitListener("orders.queue")]
/// [SendTo]  // Uses ReplyTo from incoming message
/// public OrderResponse HandleOrder(OrderRequest request)
/// {
///     return new OrderResponse { Status = "Done" };
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class SendToAttribute : Attribute
{
    /// <summary>
    /// The destination queue or exchange to send the return value to.
    /// If empty, uses the ReplyTo header from the incoming message.
    /// </summary>
    public string Destination { get; }

    /// <summary>
    /// The exchange to publish to. If empty, uses default exchange.
    /// </summary>
    public string Exchange { get; set; } = "";

    /// <summary>
    /// The routing key to use. If empty, uses the Destination as routing key.
    /// </summary>
    public string RoutingKey { get; set; } = "";

    /// <summary>
    /// Creates a SendTo attribute with an optional destination.
    /// If no destination, uses ReplyTo from incoming message.
    /// </summary>
    public SendToAttribute(string destination = "")
    {
        Destination = destination;
    }
}
