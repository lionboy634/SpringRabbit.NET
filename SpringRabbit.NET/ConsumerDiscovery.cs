using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace SpringRabbit.NET;

/// <summary>
/// Discovers and registers RabbitListener methods from assemblies.
/// </summary>
public class ConsumerDiscovery
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsumerDiscovery>? _logger;

    public ConsumerDiscovery(IServiceProvider serviceProvider, ILogger<ConsumerDiscovery>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger;
    }

    /// <summary>
    /// Discovers all RabbitListener methods in the specified assemblies.
    /// </summary>
    public List<ConsumerRegistration> DiscoverListeners(params Assembly[] assemblies)
    {
        var registrations = new List<ConsumerRegistration>();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract);

            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.GetCustomAttribute<RabbitListenerAttribute>() != null);

                foreach (var method in methods)
                {
                    var attribute = method.GetCustomAttribute<RabbitListenerAttribute>()!;
                    var registration = CreateRegistration(type, method, attribute);
                    if (registration != null)
                    {
                        registrations.Add(registration);
                    }
                }
            }
        }

        _logger?.LogInformation("Discovered {Count} RabbitListener methods", registrations.Count);
        return registrations;
    }

    private ConsumerRegistration? CreateRegistration(Type serviceType, MethodInfo method, RabbitListenerAttribute attribute)
    {
        try
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                _logger?.LogWarning("RabbitListener method {Method} has no parameters. Skipping.", method.Name);
                return null;
            }

            if (parameters.Length > 1)
            {
                _logger?.LogWarning("RabbitListener method {Method} has more than one parameter. Only the first will be used.", method.Name);
            }

            var messageType = parameters[0].ParameterType;

            var queues = attribute.Queues;
            if (queues.Length == 0)
            {
                _logger?.LogWarning("RabbitListener method {Method} has no queues specified. Skipping.", method.Name);
                return null;
            }

            return new ConsumerRegistration
            {
                Queues = queues,
                Attribute = attribute,
                MessageType = messageType,
                InvokeHandler = (message) =>
                {
                    var serviceInstance = _serviceProvider.GetService(serviceType);
                    if (serviceInstance == null)
                    {
                        // Try to create instance if not registered
                        var constructor = serviceType.GetConstructors()
                            .OrderByDescending(c => c.GetParameters().Length)
                            .FirstOrDefault();

                        if (constructor != null)
                        {
                            var ctorParams = constructor.GetParameters();
                            var args = ctorParams.Select(p => _serviceProvider.GetService(p.ParameterType)).ToArray();
                            serviceInstance = Activator.CreateInstance(serviceType, args);
                        }
                        else
                        {
                            serviceInstance = Activator.CreateInstance(serviceType);
                        }
                    }

                    return method.Invoke(serviceInstance, new[] { message });
                }
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create registration for method {Method}", method.Name);
            return null;
        }
    }
}







