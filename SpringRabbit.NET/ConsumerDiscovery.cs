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

            // Create compiled delegate for fast invocation (no reflection on hot path)
            var compiledHandler = CreateCompiledHandler(serviceType, method, messageType);

            return new ConsumerRegistration
            {
                Queues = queues,
                Attribute = attribute,
                MessageType = messageType,
                ServiceType = serviceType,
                InvokeHandler = compiledHandler
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create registration for method {Method}", method.Name);
            return null;
        }
    }

    /// <summary>
    /// Creates a compiled delegate for the handler method, eliminating reflection overhead.
    /// This compiles at startup, so message processing uses fast delegate invocation.
    /// </summary>
    private Func<object, object?, object?> CreateCompiledHandler(Type serviceType, MethodInfo method, Type messageType)
    {
        // Create expression-based compiled delegate for maximum performance
        var instanceParam = System.Linq.Expressions.Expression.Parameter(typeof(object), "instance");
        var messageParam = System.Linq.Expressions.Expression.Parameter(typeof(object), "message");

        var castInstance = System.Linq.Expressions.Expression.Convert(instanceParam, serviceType);
        var castMessage = System.Linq.Expressions.Expression.Convert(messageParam, messageType);

        var methodCall = System.Linq.Expressions.Expression.Call(castInstance, method, castMessage);

        System.Linq.Expressions.Expression body;
        if (method.ReturnType == typeof(void))
        {
            // For void methods, return null after calling
            body = System.Linq.Expressions.Expression.Block(
                methodCall,
                System.Linq.Expressions.Expression.Constant(null, typeof(object)));
        }
        else
        {
            // For methods with return value, box the result
            body = System.Linq.Expressions.Expression.Convert(methodCall, typeof(object));
        }

        var lambda = System.Linq.Expressions.Expression.Lambda<Func<object, object?, object?>>(
            body, instanceParam, messageParam);

        return lambda.Compile();
    }
}
