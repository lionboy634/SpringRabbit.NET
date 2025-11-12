# SpringRabbit.NET

A .NET port of Spring RabbitMQ's `@RabbitListener` annotation, providing declarative message consumption with automatic queue management, dead letter queue support, and concurrency control.

## Features

- ✅ **Declarative Listeners**: Use `[RabbitListener]` attribute to mark methods as message consumers
- ✅ **Concurrency Control**: Configure min-max concurrency (e.g., "3-10")
- ✅ **Dead Letter Queue (DLQ)**: Automatic DLQ setup for failed messages
- ✅ **Priority Queues**: Support for priority-based message processing
- ✅ **Auto Discovery**: Automatically discovers and registers all `[RabbitListener]` methods
- ✅ **Dependency Injection**: Full integration with .NET DI container
- ✅ **Connection Management**: Automatic reconnection and channel pooling
- ✅ **RabbitTemplate**: Easy message publishing API

## Installation

```bash
dotnet add package SpringRabbit.NET
```

## Quick Start

### 1. Configure RabbitMQ Connection

```csharp
builder.Services.AddSpringRabbit(options =>
{
    options.HostName = "localhost";
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
    
    // Or use connection string:
    // options.ConnectionString = "amqp://guest:guest@localhost:5672/";
});
```

### 2. Create a Message Model

```csharp
public class DemoMessage
{
    public string Id { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### 3. Create a Consumer

```csharp
public class DemoConsumer
{
    private readonly ILogger<DemoConsumer> _logger;

    public DemoConsumer(ILogger<DemoConsumer> logger)
    {
        _logger = logger;
    }

    // Equivalent to: @RabbitListener(queues = "demo.queue", concurrency = "3-10")
    [RabbitListener("demo.queue", Concurrency = "3-10", PrefetchCount = 5)]
    public async Task HandleMessage(DemoMessage message)
    {
        _logger.LogInformation("Processing: {Id} - {Content}", message.Id, message.Content);
        await Task.Delay(100); // Simulate work
    }
}
```

### 4. Register Services and Start

```csharp
// Register your consumer
builder.Services.AddScoped<DemoConsumer>();

// Discover and register all RabbitListener methods
builder.Services.AddRabbitListeners(typeof(Program).Assembly);

var host = builder.Build();
await host.RunAsync();
```

### 5. Send Messages

```csharp
public class MessageService
{
    private readonly RabbitTemplate _rabbitTemplate;

    public MessageService(RabbitTemplate rabbitTemplate)
    {
        _rabbitTemplate = rabbitTemplate;
    }

    public void SendMessage(DemoMessage message)
    {
        _rabbitTemplate.Send("demo.queue", message);
    }
}
```

## RabbitListener Attribute Options

```csharp
[RabbitListener(
    "queue.name",                    // Queue name(s)
    Concurrency = "3-10",            // Min-max concurrency
    PrefetchCount = 5,               // QoS prefetch count
    EnableDeadLetterQueue = true,    // Enable DLQ (default: true)
    MaxPriority = 10                 // Max priority for priority queues
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

### Priority Queue with DLQ

```csharp
[RabbitListener(
    "priority.queue",
    Concurrency = "2-5",
    MaxPriority = 10,
    EnableDeadLetterQueue = true
)]
public void HandlePriorityMessage(PriorityMessage message)
{
    // Process priority message
    // Failed messages automatically go to priority.queue.dlq
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

### Async Processing

```csharp
[RabbitListener("async.queue", Concurrency = "5-20")]
public async Task HandleAsync(Message message)
{
    await ProcessAsync(message);
}
```

## Architecture

The library is inspired by both:
- **Spring RabbitMQ**: For the declarative `@RabbitListener` pattern
- **DotKafka patterns**: For .NET-specific implementation approaches

### Key Components

- **RabbitListenerAttribute**: Marks methods as message listeners
- **ConnectionManager**: Manages RabbitMQ connections and channels
- **MessageProcessor**: Processes messages and invokes listener methods
- **ConsumerDiscovery**: Discovers `[RabbitListener]` methods via reflection
- **RabbitTemplate**: Provides easy message publishing API

## Comparison with Spring RabbitMQ

| Feature | Spring RabbitMQ | SpringRabbit.NET |
|---------|----------------|------------------|
| Annotation | `@RabbitListener` | `[RabbitListener]` |
| Concurrency | `concurrency = "3-10"` | `Concurrency = "3-10"` |
| DLQ Support | Automatic | Automatic (configurable) |
| Priority Queues | Yes | Yes |
| Auto Discovery | Yes | Yes |
| DI Integration | Spring | .NET DI |

## Requirements

- .NET 8.0 or later
- RabbitMQ Server
- RabbitMQ.Client NuGet package (included)

## License

MIT License

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

