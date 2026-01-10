using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Reflection;

namespace SpringRabbit.NET;

/// <summary>
/// Extension methods for registering SpringRabbit.NET services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SpringRabbit.NET services to the service collection.
    /// </summary>
    public static IServiceCollection AddSpringRabbit(this IServiceCollection services, Action<RabbitMQOptions>? configure = null)
    {
        var options = new RabbitMQOptions();
        configure?.Invoke(options);

        // Register connection factory
        services.AddSingleton<IConnectionFactory>(sp =>
        {
            var factory = new ConnectionFactory
            {
                HostName = options.HostName ?? "localhost",
                Port = options.Port ?? 5672,
                UserName = options.UserName ?? "guest",
                Password = options.Password ?? "guest",
                VirtualHost = options.VirtualHost ?? "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            if (!string.IsNullOrEmpty(options.ConnectionString))
            {
                factory.Uri = new Uri(options.ConnectionString);
            }

            return factory;
        });

        // Register connection manager
        services.AddSingleton<ConnectionManager>();

        // Register message processor (DI will inject ConnectionManager, ServiceProvider, MessageConverterFactory, MetricsCollector)
        services.AddSingleton<MessageProcessor>(sp =>
        {
            var connectionManager = sp.GetRequiredService<ConnectionManager>();
            var converterFactory = sp.GetRequiredService<MessageConverterFactory>();
            var metricsCollector = sp.GetRequiredService<Metrics.MetricsCollector>();
            var logger = sp.GetService<ILogger<MessageProcessor>>();
            return new MessageProcessor(connectionManager, sp, converterFactory, metricsCollector, logger);
        });

        // Register consumer discovery
        services.AddSingleton<ConsumerDiscovery>();

        // Register message converter factory
        services.AddSingleton<MessageConverterFactory>();

        // Register correlation manager for request/reply
        services.AddSingleton<CorrelationManager>();

        // Register metrics collector
        services.AddSingleton<Metrics.MetricsCollector>();

        // Register RabbitTemplate (DI will inject ConnectionManager, MessageConverterFactory, CorrelationManager)
        services.AddSingleton<RabbitTemplate>(sp =>
        {
            var connectionManager = sp.GetRequiredService<ConnectionManager>();
            var converterFactory = sp.GetRequiredService<MessageConverterFactory>();
            var correlationManager = sp.GetRequiredService<CorrelationManager>();
            var logger = sp.GetService<ILogger<RabbitTemplate>>();
            return new RabbitTemplate(connectionManager, converterFactory, correlationManager, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds RabbitMQ health check.
    /// </summary>
    public static IHealthChecksBuilder AddRabbitMQHealthCheck(this IHealthChecksBuilder builder, string name = "rabbitmq")
    {
        return builder.AddCheck<Health.RabbitMQHealthCheck>(name);
    }

    /// <summary>
    /// Registers RabbitListener services and starts consumers.
    /// </summary>
    public static IServiceCollection AddRabbitListeners(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddHostedService<RabbitListenerHostedService>(sp =>
        {
            var discovery = sp.GetRequiredService<ConsumerDiscovery>();
            var processor = sp.GetRequiredService<MessageProcessor>();
            var logger = sp.GetService<ILogger<RabbitListenerHostedService>>();

            var registrations = discovery.DiscoverListeners(assemblies);
            foreach (var registration in registrations)
            {
                processor.RegisterListener(registration);
            }

            return new RabbitListenerHostedService(processor, logger);
        });

        return services;
    }
}

/// <summary>
/// Configuration options for RabbitMQ connection.
/// </summary>
public class RabbitMQOptions
{
    public string? ConnectionString { get; set; }
    public string? HostName { get; set; }
    public int? Port { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? VirtualHost { get; set; }
}

