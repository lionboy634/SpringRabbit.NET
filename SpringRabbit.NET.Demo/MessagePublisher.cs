using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpringRabbit.NET;
using SpringRabbit.NET.Demo;

namespace SpringRabbit.NET.Demo;

/// <summary>
/// Example service that publishes messages to RabbitMQ queues.
/// </summary>
public class MessagePublisher : BackgroundService
{
    private readonly RabbitTemplate _rabbitTemplate;
    private readonly ILogger<MessagePublisher> _logger;

    public MessagePublisher(RabbitTemplate rabbitTemplate, ILogger<MessagePublisher> logger)
    {
        _rabbitTemplate = rabbitTemplate;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit for consumers to start
        

        _logger.LogInformation("Starting to publish demo messages...");

        var range = Enumerable.Range(1, 1_000_000);
        var processorCount = Environment.ProcessorCount;

        List<Task> tasks = new();
        tasks.Add(CreateDataAsync());

        await Task.WhenAll(tasks);

        Parallel.ForEach(range, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount == 1 ? 1: Environment.ProcessorCount - 1}, i =>
        {
            var message = new DemoMessage
            {
                Id = Guid.NewGuid().ToString(),
                Content = $"Demo message #{i}",
                Timestamp = DateTime.UtcNow
            };

            _rabbitTemplate.Send("demo.queue", message);
            _logger.LogInformation("Published message #{Id}: {Content}", message.Id, message.Content);

            // Send a priority message every 3rd message
            if (i % 3 == 0 || i % 5 == 0)
            {
                var priorityMessage = new DemoMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = $"Priority message #{i}",
                    Timestamp = DateTime.UtcNow
                };
                _rabbitTemplate.Send("priority.queue", priorityMessage, priority: 10);
                _logger.LogInformation("Published priority message #{Id}", priorityMessage.Id);
            }

        });
        _logger.LogInformation("Finished publishing demo messages");
    }

    private Task DelayAsync(int milliseconds, CancellationToken token)
    {
        return Task.Delay(milliseconds, token);
    }

    private Task CreateDataAsync()
    {
        return Task.FromResult(0); 
    }
}








