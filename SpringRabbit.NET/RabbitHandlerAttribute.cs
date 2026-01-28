namespace SpringRabbit.NET;

/// <summary>
/// Marks a method as a message handler within a class decorated with [RabbitListener].
/// Allows multiple handlers in one class for different message types.
/// The correct handler is selected based on the message type at runtime.
/// </summary>
/// <example>
/// <code>
/// [RabbitListener("orders.queue")]
/// public class OrderHandler
/// {
///     [RabbitHandler]
///     public void HandleOrder(OrderMessage order) { }
///     
///     [RabbitHandler]
///     public void HandleRefund(RefundMessage refund) { }
///     
///     [RabbitHandler(IsDefault = true)]
///     public void HandleUnknown(object message) { }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class RabbitHandlerAttribute : Attribute
{
    /// <summary>
    /// If true, this handler is used when no other handler matches the message type.
    /// Only one default handler is allowed per listener class.
    /// </summary>
    public bool IsDefault { get; set; } = false;
}
