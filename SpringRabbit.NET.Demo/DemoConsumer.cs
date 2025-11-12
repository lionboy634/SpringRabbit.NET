using Microsoft.Extensions.Logging;
using SpringRabbit.NET;

namespace SpringRabbit.NET.Demo;

/// <summary>
/// Example consumer demonstrating the @RabbitListener equivalent in .NET
/// </summary>
public class DemoConsumer
{
    private readonly ILogger<DemoConsumer> _logger;

    public DemoConsumer(ILogger<DemoConsumer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Equivalent to: @RabbitListener(queues = "demo.queue", concurrency = "3-10")
    /// </summary>
    [RabbitListener("demo.queue", Concurrency = "3-10", PrefetchCount = 5)]
    public async Task HandleDemoMessage(DemoMessage message)
    {
        _logger.LogInformation("Processing demo message: {Id} - {Content}", message.Id, message.Content);
        
        // Simulate some work
        await Task.Delay(100);
        
        _logger.LogInformation("Completed processing message: {Id}", message.Id);
    }

    /// <summary>
    /// Example with priority queue and DLQ
    /// </summary>
    [RabbitListener("priority.queue", Concurrency = "2-5", MaxPriority = 10, EnableDeadLetterQueue = true)]
    public void HandlePriorityMessage(DemoMessage message)
    {
        _logger.LogInformation("Processing priority message: {Id}", message.Id);
    }
}

