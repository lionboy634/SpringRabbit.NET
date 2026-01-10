using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Expression = System.Linq.Expressions.Expression;

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

            var queues = attribute.Queues;
            if (queues.Length == 0)
            {
                _logger?.LogWarning("RabbitListener method {Method} has no queues specified. Skipping.", method.Name);
                return null;
            }

            // Determine parameter types
            var messageType = parameters[0].ParameterType;
            var needsMessageContext = parameters.Length >= 2 && 
                                       parameters[1].ParameterType == typeof(MessageContext);

            if (parameters.Length > 2)
            {
                _logger?.LogWarning("RabbitListener method {Method} has more than 2 parameters. Only message and MessageContext are supported.", method.Name);
            }
            else if (parameters.Length == 2 && !needsMessageContext)
            {
                _logger?.LogWarning("RabbitListener method {Method} second parameter must be MessageContext. Got {Type}.", 
                    method.Name, parameters[1].ParameterType.Name);
            }

            // Create compiled delegate for fast invocation
            var compiledHandler = CreateCompiledHandler(serviceType, method, messageType, needsMessageContext);

            return new ConsumerRegistration
            {
                Queues = queues,
                Attribute = attribute,
                MessageType = messageType,
                ServiceType = serviceType,
                NeedsMessageContext = needsMessageContext,
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
    /// Supports optional MessageContext as second parameter.
    /// </summary>
    private Func<object, object?, MessageContext?, object?> CreateCompiledHandler(
        Type serviceType, MethodInfo method, Type messageType, bool needsMessageContext)
    {
        // Parameters: (instance, message, messageContext)
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var messageParam = Expression.Parameter(typeof(object), "message");
        var contextParam = Expression.Parameter(typeof(MessageContext), "context");

        var castInstance = Expression.Convert(instanceParam, serviceType);
        var castMessage = Expression.Convert(messageParam, messageType);

        System.Linq.Expressions.MethodCallExpression methodCall;
        
        if (needsMessageContext)
        {
            // Call with both message and context
            methodCall = Expression.Call(castInstance, method, castMessage, contextParam);
        }
        else
        {
            // Call with just message
            methodCall = Expression.Call(castInstance, method, castMessage);
        }

        System.Linq.Expressions.Expression body;
        if (method.ReturnType == typeof(void))
        {
            body = Expression.Block(
                methodCall,
                Expression.Constant(null, typeof(object)));
        }
        else
        {
            body = Expression.Convert(methodCall, typeof(object));
        }

        var lambda = Expression.Lambda<Func<object, object?, MessageContext?, object?>>(
            body, instanceParam, messageParam, contextParam);

        return lambda.Compile();
    }
}
