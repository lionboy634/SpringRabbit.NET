using Microsoft.Extensions.Logging;
using SpringRabbit.NET.Converters;

namespace SpringRabbit.NET;

/// <summary>
/// Factory for creating and managing message converters.
/// </summary>
public class MessageConverterFactory
{
    private readonly Dictionary<string, IMessageConverter> _converters = new();
    private readonly ILogger<MessageConverterFactory>? _logger;
    private IMessageConverter _defaultConverter;

    public MessageConverterFactory(ILogger<MessageConverterFactory>? logger = null)
    {
        _logger = logger;
        
        // Register default converters
        var jsonConverter = new JsonMessageConverter();
        _defaultConverter = jsonConverter;
        RegisterConverter(jsonConverter);
        RegisterConverter(new XmlMessageConverter());
        RegisterConverter(new BinaryMessageConverter());
    }

    /// <summary>
    /// Registers a message converter.
    /// </summary>
    public void RegisterConverter(IMessageConverter converter)
    {
        _converters[converter.ContentType] = converter;
        _logger?.LogDebug("Registered message converter: {ContentType}", converter.ContentType);
    }

    /// <summary>
    /// Gets a converter by content type, or returns the default converter.
    /// </summary>
    public IMessageConverter GetConverter(string? contentType = null)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return _defaultConverter;
        }

        if (_converters.TryGetValue(contentType, out var converter))
        {
            return converter;
        }

        _logger?.LogWarning("No converter found for content type {ContentType}, using default", contentType);
        return _defaultConverter;
    }

    /// <summary>
    /// Sets the default converter.
    /// </summary>
    public void SetDefaultConverter(IMessageConverter converter)
    {
        _defaultConverter = converter;
        _logger?.LogDebug("Set default converter to: {ContentType}", converter.ContentType);
    }
}


