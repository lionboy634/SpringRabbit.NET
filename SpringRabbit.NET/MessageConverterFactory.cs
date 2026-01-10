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

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageConverterFactory"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for factory operations.</param>
    /// <remarks>
    /// Automatically registers default converters: JSON (default), XML, and Binary.
    /// </remarks>
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
    /// Registers a message converter for a specific content type.
    /// </summary>
    /// <param name="converter">The message converter to register.</param>
    /// <remarks>
    /// If a converter with the same content type already exists, it will be replaced.
    /// </remarks>
    public void RegisterConverter(IMessageConverter converter)
    {
        _converters[converter.ContentType] = converter;
        _logger?.LogDebug("Registered message converter: {ContentType}", converter.ContentType);
    }

    /// <summary>
    /// Gets a converter by content type, or returns the default converter if no content type is specified or no matching converter is found.
    /// </summary>
    /// <param name="contentType">The content type to get a converter for. If null or empty, returns the default converter.</param>
    /// <returns>The message converter for the specified content type, or the default converter if not found.</returns>
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
    /// Sets the default converter to use when no specific converter is found or requested.
    /// </summary>
    /// <param name="converter">The message converter to set as the default.</param>
    public void SetDefaultConverter(IMessageConverter converter)
    {
        _defaultConverter = converter;
        _logger?.LogDebug("Set default converter to: {ContentType}", converter.ContentType);
    }
}








