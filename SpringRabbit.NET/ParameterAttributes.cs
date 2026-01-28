namespace SpringRabbit.NET;

/// <summary>
/// Injects a specific message header value as a method parameter.
/// </summary>
/// <example>
/// <code>
/// [RabbitListener("orders.queue")]
/// public void HandleOrder(
///     OrderMessage order, 
///     [Header("x-correlation-id")] string correlationId,
///     [Header("x-tenant-id")] int tenantId,
///     [Header("x-priority", Required = false)] int? priority)
/// {
///     // correlationId, tenantId, and priority are extracted from message headers
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class HeaderAttribute : Attribute
{
    /// <summary>
    /// The name of the header to extract.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Whether the header is required. If true and header is missing, an exception is thrown.
    /// Default is true.
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Default value to use if header is not present (only used when Required = false).
    /// </summary>
    public object? DefaultValue { get; set; }

    public HeaderAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}

/// <summary>
/// Injects all message headers as a dictionary parameter.
/// </summary>
/// <example>
/// <code>
/// [RabbitListener("orders.queue")]
/// public void HandleOrder(
///     OrderMessage order, 
///     [Headers] IDictionary&lt;string, object&gt; headers)
/// {
///     var tenantId = headers["x-tenant-id"];
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class HeadersAttribute : Attribute
{
}

/// <summary>
/// Injects the raw message body as byte[] parameter.
/// </summary>
/// <example>
/// <code>
/// [RabbitListener("orders.queue")]
/// public void HandleOrder(OrderMessage order, [Payload] byte[] rawBody)
/// {
///     // rawBody contains the original message bytes
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class PayloadAttribute : Attribute
{
}
