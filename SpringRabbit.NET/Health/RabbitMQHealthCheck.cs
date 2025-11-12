using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace SpringRabbit.NET.Health;

/// <summary>
/// Health check for RabbitMQ connection.
/// </summary>
public class RabbitMQHealthCheck : IHealthCheck
{
    private readonly ConnectionManager _connectionManager;

    public RabbitMQHealthCheck(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = _connectionManager.GetConnection();
            if (connection?.IsOpen == true)
            {
                return HealthCheckResult.Healthy("RabbitMQ connection is open");
            }

            return HealthCheckResult.Unhealthy("RabbitMQ connection is not open");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ connection check failed", ex);
        }
    }
}


