using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SpringRabbit.NET;

/// <summary>
/// Hosted service that starts RabbitMQ listeners on application startup.
/// </summary>
public class RabbitListenerHostedService : IHostedService
{
    private readonly MessageProcessor _messageProcessor;
    private readonly ILogger<RabbitListenerHostedService>? _logger;

    public RabbitListenerHostedService(MessageProcessor messageProcessor, ILogger<RabbitListenerHostedService>? logger = null)
    {
        _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Starting RabbitMQ listeners...");
        _messageProcessor.StartAll();
        _logger?.LogInformation("RabbitMQ listeners started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Stopping RabbitMQ listeners...");
        _messageProcessor.StopAll();
        _logger?.LogInformation("RabbitMQ listeners stopped");
        return Task.CompletedTask;
    }
}


