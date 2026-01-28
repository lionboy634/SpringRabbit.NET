# SpringRabbit.NET

A .NET port of Spring RabbitMQ's `@RabbitListener` annotation, providing declarative message consumption with automatic queue management, dead letter queue support, retry mechanisms, and concurrency control. Inspired by Spring AMQP and adapted for .NET.

## Features

- ✅ **Declarative Listeners**: Use `[RabbitListener]` attribute to mark methods as message consumers
- ✅ **Concurrency Control**: Configure min-max concurrency (e.g., "3-10")
- ✅ **Dead Letter Queue (DLQ)**: Automatic DLQ setup for failed messages
- ✅ **Priority Queues**: Support for priority-based message processing
- ✅ **Auto Discovery**: Automatically discovers and registers all `[RabbitListener]` methods
- ✅ **Dependency Injection**: Full integration with .NET DI container
- ✅ **Connection Management**: Automatic reconnection and channel pooling
- ✅ **RabbitTemplate**: Easy message publishing API
- ✅ **Message Converters**: JSON, XML, and Binary support
- ✅ **Retry Mechanisms**: Exponential backoff and configurable retry policies
- ✅ **Request/Reply Pattern**: RPC-style messaging with correlation IDs
- ✅ **Exchange & Binding Management**: Declarative exchange and binding setup
- ✅ **Error Handlers**: Custom error handling per listener
- ✅ **Advanced Queue Features**: TTL, Lazy queues, Quorum queues
- ✅ **Monitoring & Metrics**: Listener statistics and health checks
- ✅ **Testing Utilities**: Test helpers and mock RabbitMQ

## Installation

```bash
dotnet add package SpringRabbit.NET
```

Or via Package Manager Console:
```
Install-Package SpringRabbit.NET
```

## Quick Start

### 1. Configure RabbitMQ Connection

```csharp
using SpringRabbit.NET;

var builder = Host.CreateApplicationBuilder(args);

// Configure RabbitMQ connection
builder.Services.AddSpringRabbit(options =>
{
    // Option 1: Use connection string
    options.ConnectionString = "amqp://guest:guest@localhost:5672/";
    
    // Option 2: Use individual settings
    options.HostName = "localhost";
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
    options.VirtualHost = "/";
});
```

### 2. Create a Message Model

```csharp
public class OrderMessage
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 3. Create a Consumer

```csharp
using SpringRabbit.NET;
using Microsoft.Extensions.Logging;

public class OrderConsumer
{
    private readonly ILogger<OrderConsumer> _logger;

    public OrderConsumer(ILogger<OrderConsumer> logger)
    {
        _logger = logger;
    }

    // Equivalent to: @RabbitListener(queues = "orders.queue", concurrency = "3-10")
    [RabbitListener("orders.queue", Concurrency = "3-10", PrefetchCount = 5)]
    public async Task ProcessOrder(OrderMessage order)
    {
        _logger.LogInformation("Processing order: {OrderId} for customer {CustomerId}", 
            order.OrderId, order.CustomerId);
        
        // Your business logic here
        await ProcessOrderAsync(order);
        
        _logger.LogInformation("Order {OrderId} processed successfully", order.OrderId);
    }
}
```

### 4. Register Services and Start

```csharp
// Register your consumer services
builder.Services.AddScoped<OrderConsumer>();

// Discover and register all RabbitListener methods
builder.Services.AddRabbitListeners(typeof(Program).Assembly);

// Add health checks (optional)
builder.Services.AddHealthChecks()
    .AddRabbitMQHealthCheck();

var host = builder.Build();
await host.RunAsync();
```

### 5. Send Messages

```csharp
public class OrderService
{
    private readonly RabbitTemplate _rabbitTemplate;

    public OrderService(RabbitTemplate rabbitTemplate)
    {
        _rabbitTemplate = rabbitTemplate;
    }

    public void CreateOrder(OrderMessage order)
    {
        _rabbitTemplate.Send("orders.queue", order);
    }
}
```

## RabbitListener Attribute Options

```csharp
[RabbitListener(
    "queue.name",                    // Queue name(s) - required
    Concurrency = "3-10",            // Min-max concurrency (default: "1-1")
    PrefetchCount = 5,               // QoS prefetch count (default: 1)
    EnableDeadLetterQueue = true,    // Enable DLQ (default: true)
    MaxPriority = 10,                // Max priority for priority queues (default: 0)
    MaxRetryAttempts = 3,            // Max retry attempts (default: 0 = no retry)
    RetryInitialDelayMs = 1000,     // Initial retry delay in ms (default: 1000)
    RetryMultiplier = 2.0,           // Retry multiplier for exponential backoff (default: 2.0)
    RetryMaxDelayMs = 300000         // Max retry delay in ms (default: 300000 = 5 min)
)]
public void HandleMessage(YourMessageType message)
{
    // Process message
}
```

### Concurrency Format

- `"3-10"` - Minimum 3, maximum 10 concurrent consumers
- `"5"` - Exactly 5 concurrent consumers
- Default: `"1-1"` (single consumer)

## Advanced Examples

### Priority Queue with Retry

```csharp
[RabbitListener(
    "priority.orders",
    Concurrency = "2-5",
    MaxPriority = 10,
    EnableDeadLetterQueue = true,
    MaxRetryAttempts = 5,
    RetryInitialDelayMs = 2000,
    RetryMultiplier = 2.0
)]
public async Task HandlePriorityOrder(PriorityOrderMessage message)
{
    // Process priority order
    // Failed messages automatically retry with exponential backoff
    // After max retries, messages go to priority.orders.dlq
}
```

### Multiple Queues

```csharp
[RabbitListener("queue1", "queue2", "queue3", Concurrency = "1-3")]
public void HandleMultipleQueues(Message message)
{
    // Handles messages from all three queues
}
```

### Accessing Message Headers and Metadata

```csharp
[RabbitListener("orders.queue")]
public async Task HandleOrder(OrderMessage order, MessageContext context)
{
    // Access correlation ID for tracing
    var correlationId = context.CorrelationId;
    
    // Check message priority
    var priority = context.Priority;
    
    // Read custom headers
    var customHeader = context.GetHeader<string>("x-custom-header");
    var tenantId = context.GetHeader<int>("x-tenant-id");
    
    // Check if header exists
    if (context.HasHeader("x-urgent"))
    {
        // Handle urgent message
    }
    
    // Access raw properties
    var timestamp = context.Timestamp;
    var messageId = context.MessageId;
    var redelivered = context.Redelivered;
    
    _logger.LogInformation(
        "Processing order {OrderId} with correlation {CorrelationId}", 
        order.OrderId, correlationId);
}
```

### Custom Content Type (XML)

```csharp
[RabbitListener("xml.queue", Concurrency = "1-2")]
public void HandleXmlMessage(XmlOrderMessage message)
{
    // Message will be automatically deserialized from XML
}

// Send XML message
_rabbitTemplate.Send("xml.queue", order, contentType: "application/xml");
```

### Request/Reply Pattern (RPC)

```csharp
// Server side - handle requests
[RabbitListener("rpc.orders", Concurrency = "1-3")]
public OrderResponse HandleOrderRequest(OrderRequest request)
{
    // Process request and return response
    return new OrderResponse 
    { 
        OrderId = request.OrderId, 
        Status = "Processed" 
    };
}

// Client side - send request and wait for response
public class OrderClient
{
    private readonly RabbitTemplate _rabbitTemplate;

    public async Task<OrderResponse> GetOrderStatus(string orderId)
    {
        var request = new OrderRequest { OrderId = orderId };
        
        // Send and wait for response (30 second timeout)
        var response = await _rabbitTemplate.SendAndReceiveAsync<OrderRequest, OrderResponse>(
            "rpc.orders", 
            request, 
            TimeSpan.FromSeconds(30)
        );
        
        return response!;
    }
}
```

### Exchange and Binding Setup

```csharp
// In your startup/configuration
var connectionManager = serviceProvider.GetRequiredService<ConnectionManager>();

// Create a topic exchange
var exchange = new ExchangeBuilder()
    .Name("orders.exchange")
    .Type(ExchangeType.Topic)
    .Durable(true);

connectionManager.DeclareExchange(exchange);

// Bind queue to exchange
var binding = BindingBuilder
    .BindQueue("orders.queue")
    .To("orders.exchange")
    .WithRoutingKey("order.*");

connectionManager.DeclareBinding(binding);

// Now publish to exchange with routing key
_rabbitTemplate.Send("orders.exchange", message, routingKey: "order.created");
```

### Custom Error Handler

```csharp
public class CustomErrorHandler : IErrorHandler
{
    private readonly ILogger<CustomErrorHandler> _logger;

    public CustomErrorHandler(ILogger<CustomErrorHandler> logger)
    {
        _logger = logger;
    }

    public bool HandleError(Exception exception, byte[] messageBody, ulong deliveryTag, IModel channel)
    {
        _logger.LogError(exception, "Custom error handling for message");
        
        // Custom logic - maybe save to database, send notification, etc.
        
        // Return false to reject and send to DLQ
        // Return true to acknowledge (message will be lost)
        return false;
    }
}

// Register error handler
var registration = new ConsumerRegistration
{
    Queues = new[] { "orders.queue" },
    Attribute = new RabbitListenerAttribute("orders.queue"),
    MessageType = typeof(OrderMessage),
    ErrorHandler = new CustomErrorHandler(logger),
    InvokeHandler = (msg) => { /* handler */ }
};
```

### Advanced Queue Features

```csharp
// Using QueueOptions for advanced configuration
var connectionManager = serviceProvider.GetRequiredService<ConnectionManager>();

connectionManager.EnsureQueue("orders.queue", new QueueOptions
{
    EnableDeadLetterQueue = true,
    MaxPriority = 10,
    MessageTtl = TimeSpan.FromHours(24),  // Messages expire after 24 hours
    Lazy = true,                           // Lazy queue (disk-based)
    Quorum = false                         // Use classic queue (set to true for quorum)
});
```

### Delay Queues ("The Waiting Room" Strategy)

You can use standard RabbitMQ TTL and Dead Lettering to implement message delays without any plugins:

```csharp
builder.Services.AddSpringRabbit(options =>
{
    options.ConnectionString = "amqp://guest:guest@localhost:5672/";

    // Setting up a 5-minute delay queue
    options.DeclareQueue("sms.grace.period", queueOptions => 
    {
        // 1. Message stays here for 5 minutes (300,000ms)
        queueOptions.Arguments["x-message-ttl"] = 300000; 
        
        // 2. When it expires, it jumps back to the default exchange ("")
        queueOptions.Arguments["x-dead-letter-exchange"] = ""; 
        
        // 3. And is routed to the actual consumer queue
        queueOptions.Arguments["x-dead-letter-routing-key"] = "sms.notifications";
    });
});
```

### Monitoring and Metrics

```csharp
// Get metrics for a queue
var metricsCollector = serviceProvider.GetRequiredService<MetricsCollector>();
var metrics = metricsCollector.GetMetricsForQueue("orders.queue");

Console.WriteLine($"Messages Processed: {metrics.MessagesProcessed}");
Console.WriteLine($"Messages Failed: {metrics.MessagesFailed}");
Console.WriteLine($"Messages Retried: {metrics.MessagesRetried}");
Console.WriteLine($"Average Processing Time: {metrics.AverageProcessingTime}");
Console.WriteLine($"Last Message Processed: {metrics.LastMessageProcessed}");

// Get all metrics
foreach (var queueMetrics in metricsCollector.GetAllMetrics())
{
    Console.WriteLine($"Queue: {queueMetrics.QueueName}, Processed: {queueMetrics.MessagesProcessed}");
}
```

### Health Checks

```csharp
// Add health check in startup
builder.Services.AddHealthChecks()
    .AddRabbitMQHealthCheck();

// Health check endpoint (if using ASP.NET Core)
app.MapHealthChecks("/health");
```

## Message Converters

SpringRabbit.NET supports multiple message formats:

### JSON (Default)

```csharp
// Automatically used if no content type specified
_rabbitTemplate.Send("queue", message); // Uses JSON
_rabbitTemplate.Send("queue", message, contentType: "application/json");
```

### XML

```csharp
_rabbitTemplate.Send("queue", message, contentType: "application/xml");
```

### Binary

```csharp
byte[] data = File.ReadAllBytes("file.pdf");
_rabbitTemplate.Send("queue", data, contentType: "application/octet-stream");
```

### Custom Converter

```csharp
public class CustomConverter : IMessageConverter
{
    public string ContentType => "application/custom";

    public byte[] ToMessage(object obj, out string? contentType)
    {
        contentType = ContentType;
        // Your serialization logic
        return Encoding.UTF8.GetBytes(/* serialized */);
    }

    public object? FromMessage(byte[] body, Type targetType, string? contentType = null)
    {
        // Your deserialization logic
        return /* deserialized object */;
    }
}

// Register custom converter
var converterFactory = serviceProvider.GetRequiredService<MessageConverterFactory>();
converterFactory.RegisterConverter(new CustomConverter());
```

## Retry Mechanisms

### Automatic Retry with Exponential Backoff

```csharp
[RabbitListener(
    "orders.queue",
    MaxRetryAttempts = 5,        // Retry up to 5 times
    RetryInitialDelayMs = 1000, // Start with 1 second delay
    RetryMultiplier = 2.0,       // Double delay each retry
    RetryMaxDelayMs = 60000      // Max 60 seconds between retries
)]
public void HandleOrder(OrderMessage order)
{
    // If this throws an exception, it will retry automatically
    // Retry delays: 1s, 2s, 4s, 8s, 16s, then DLQ
}
```

### Custom Retry Policy

```csharp
var retryPolicy = new ExponentialBackoffRetryPolicy(
    initialDelay: TimeSpan.FromSeconds(1),
    multiplier: 2.0,
    maxDelay: TimeSpan.FromMinutes(5),
    maxAttempts: 3,
    retryableExceptions: new[] { typeof(TimeoutException), typeof(HttpRequestException) }
);
```

## Testing

### Using Test Helpers

```csharp
using SpringRabbit.NET.Testing;

var channel = connectionManager.GetChannel("test.queue");

// Purge queue before test
RabbitMQTestHelpers.PurgeQueue(channel, "test.queue");

// Get message count
var count = RabbitMQTestHelpers.GetMessageCount(channel, "test.queue");

// Clean up
RabbitMQTestHelpers.DeleteQueue(channel, "test.queue");
```

### Using Mock RabbitMQ

```csharp
using SpringRabbit.NET.Testing;

var mockRabbit = new MockRabbitMQ();

// Set up consumer
mockRabbit.Consume("test.queue", (body, properties) =>
{
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($"Received: {message}");
});

// Publish message
mockRabbit.Publish("test.queue", Encoding.UTF8.GetBytes("Hello World"));

// Check message count
var count = mockRabbit.GetMessageCount("test.queue");
```

## Configuration Reference

### RabbitMQOptions

```csharp
builder.Services.AddSpringRabbit(options =>
{
    // Connection string (takes precedence if set)
    options.ConnectionString = "amqp://user:pass@host:5672/vhost";
    
    // Individual connection settings
    options.HostName = "localhost";
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
    options.VirtualHost = "/";
});
```

### QueueOptions

```csharp
var queueOptions = new QueueOptions
{
    EnableDeadLetterQueue = true,        // Enable DLQ
    MaxPriority = 10,                     // Priority queue support
    MessageTtl = TimeSpan.FromHours(24), // Message TTL
    Lazy = false,                         // Lazy queue mode
    Quorum = false                        // Quorum queue type
};
```

## Best Practices

### 1. Idempotent Message Processing

```csharp
[RabbitListener("orders.queue")]
public async Task ProcessOrder(OrderMessage order)
{
    // Check if already processed (idempotency)
    if (await _orderRepository.ExistsAsync(order.OrderId))
    {
        _logger.LogInformation("Order {OrderId} already processed", order.OrderId);
        return; // Acknowledge and skip
    }
    
    await ProcessOrderAsync(order);
}
```

### 2. Error Handling

```csharp
[RabbitListener("orders.queue", MaxRetryAttempts = 3)]
public async Task ProcessOrder(OrderMessage order)
{
    try
    {
        await ProcessOrderAsync(order);
    }
    catch (BusinessException ex)
    {
        // Don't retry business logic errors
        _logger.LogError(ex, "Business error processing order {OrderId}", order.OrderId);
        throw; // Will go to DLQ immediately
    }
    catch (TransientException ex)
    {
        // Transient errors will be retried
        _logger.LogWarning(ex, "Transient error, will retry");
        throw;
    }
}
```

### 3. Message Validation

```csharp
[RabbitListener("orders.queue")]
public async Task ProcessOrder(OrderMessage order)
{
    // Validate message
    if (string.IsNullOrEmpty(order.OrderId) || order.Amount <= 0)
    {
        _logger.LogWarning("Invalid order message: {Order}", order);
        return; // Acknowledge invalid message (don't retry)
    }
    
    await ProcessOrderAsync(order);
}
```

### 4. Concurrency Tuning

```csharp
// For CPU-intensive tasks
[RabbitListener("cpu.queue", Concurrency = "1-4")] // Match CPU cores

// For I/O-intensive tasks
[RabbitListener("io.queue", Concurrency = "5-20")] // Higher concurrency

// For mixed workloads
[RabbitListener("mixed.queue", Concurrency = "3-10")] // Balanced
```

## Troubleshooting

### Connection Issues

```csharp
// Check connection health
var healthCheck = serviceProvider.GetRequiredService<RabbitMQHealthCheck>();
var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
Console.WriteLine($"RabbitMQ Health: {result.Status}");
```

### Message Not Being Processed

1. Check queue exists: `RabbitMQTestHelpers.GetMessageCount(channel, "queue.name")`
2. Check consumer count: `RabbitMQTestHelpers.GetConsumerCount(channel, "queue.name")`
3. Check logs for errors
4. Verify `[RabbitListener]` attribute is on a public method
5. Ensure service is registered in DI container

### DLQ Messages

Failed messages automatically go to `{queue.name}.dlq`. Monitor these queues:

```csharp
// Check DLQ message count
var dlqCount = RabbitMQTestHelpers.GetMessageCount(channel, "orders.queue.dlq");
```

## API Reference

### RabbitListenerAttribute

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Queues` | `string[]` | Required | Queue name(s) to listen to |
| `Concurrency` | `string` | `"1-1"` | Min-max concurrency format |
| `PrefetchCount` | `ushort` | `1` | QoS prefetch count |
| `EnableDeadLetterQueue` | `bool` | `true` | Enable DLQ support |
| `MaxPriority` | `byte` | `0` | Max priority (0 = disabled) |
| `MaxRetryAttempts` | `int` | `0` | Max retry attempts (0 = no retry) |
| `RetryInitialDelayMs` | `int` | `1000` | Initial retry delay (ms) |
| `RetryMultiplier` | `double` | `2.0` | Retry delay multiplier |
| `RetryMaxDelayMs` | `int` | `300000` | Max retry delay (ms) |

### RabbitTemplate Methods

```csharp
// Send message
bool Send(string queueName, object message, byte? priority = null, string? contentType = null)

// Send message asynchronously
Task<bool> SendAsync(string queueName, object message, byte? priority = null, string? contentType = null)

// Request/Reply (RPC)
Task<TResponse?> SendAndReceiveAsync<TRequest, TResponse>(
    string queueName, 
    TRequest request, 
    TimeSpan timeout, 
    string? contentType = null)
```

## Comparison with Spring RabbitMQ

| Feature | Spring RabbitMQ | SpringRabbit.NET |
|---------|----------------|------------------|
| Annotation | `@RabbitListener` | `[RabbitListener]` |
| Concurrency | `concurrency = "3-10"` | `Concurrency = "3-10"` |
| DLQ Support | Automatic | Automatic (configurable) |
| Priority Queues | Yes | Yes |
| Auto Discovery | Yes | Yes |
| DI Integration | Spring | .NET DI |
| Retry Mechanisms | Yes | Yes (configurable) |
| Request/Reply | Yes | Yes |
| Message Converters | Yes | Yes (JSON, XML, Binary) |
| Health Checks | Yes | Yes |

## Requirements

- .NET 8.0 or later
- RabbitMQ Server 3.8+ (for quorum queues: 3.8+)
- RabbitMQ.Client NuGet package (included as dependency)

## License

MIT License - see LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

- **Issues**: https://github.com/lionboy634/SpringRabbit.NET/issues
- **Repository**: https://github.com/lionboy634/SpringRabbit.NET

## Changelog

### Version 1.4.0
- **Feature**: `DeclareQueue` - Explicitly declare infrastructure queues with custom RabbitMQ arguments
- **Feature**: `QueueOptions.Arguments` - Support for arbitrary RabbitMQ queue arguments (e.g., `x-message-ttl`, `x-dead-letter-exchange`)
- **Improved**: Automatic queue provisioning on application startup for manually declared queues

### Version 1.3.0
- **Feature**: Enhanced XML documentation and package metadata
- **Improved**: More robust connection recovery and channel management

### Version 1.2.0
- **Feature**: `MessageContext` - Access message headers, properties, and metadata in handlers
- **Feature**: GitHub Actions CI/CD workflow for automated builds and NuGet publishing
- **Improved**: Handlers now support optional second parameter `MessageContext` for advanced scenarios

### Version 1.1.0
- **Performance**: Compiled delegates replace reflection for handler invocation (10x faster message processing)
- **Performance**: Scoped service resolution for proper DI lifecycle management
- **Fix**: Passive queue declare to handle pre-existing queues gracefully (no more PRECONDITION_FAILED errors)
- **Fix**: Corrected package dependencies to target .NET 8.0 (was incorrectly referencing .NET 10)
- **Improved**: Better XML documentation for all public types

### Version 1.0.0
- Initial release
- Core `[RabbitListener]` functionality
- Message converters (JSON, XML, Binary)
- Retry mechanisms with exponential backoff
- Request/Reply pattern support
- Exchange and binding management
- Error handlers
- Advanced queue features (TTL, Lazy, Quorum)
- Monitoring and metrics
- Health checks
- Testing utilities
