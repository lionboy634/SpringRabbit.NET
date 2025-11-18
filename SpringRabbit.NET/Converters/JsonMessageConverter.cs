using System.Text;
using System.Text.Json;

namespace SpringRabbit.NET.Converters;

/// <summary>
/// JSON message converter using System.Text.Json.
/// </summary>
public class JsonMessageConverter : IMessageConverter
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonMessageConverter"/> class.
    /// </summary>
    /// <param name="options">Optional JSON serializer options. If not provided, uses default options with case-insensitive property names.</param>
    public JsonMessageConverter(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Gets the content type for JSON messages: "application/json".
    /// </summary>
    public string ContentType => "application/json";

    /// <summary>
    /// Converts an object to a byte array using JSON serialization.
    /// </summary>
    /// <param name="obj">The object to convert to JSON.</param>
    /// <param name="contentType">When this method returns, contains the content type "application/json".</param>
    /// <returns>A byte array containing the JSON representation of the object.</returns>
    public byte[] ToMessage(object obj, out string? contentType)
    {
        contentType = ContentType;
        var json = JsonSerializer.Serialize(obj, _options);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Converts a byte array containing JSON to an object of the specified type.
    /// </summary>
    /// <param name="body">The byte array containing the JSON data.</param>
    /// <param name="targetType">The type of object to deserialize to.</param>
    /// <param name="contentType">The content type of the message (optional, not used for JSON deserialization).</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    public object? FromMessage(byte[] body, Type targetType, string? contentType = null)
    {
        var json = Encoding.UTF8.GetString(body);
        return JsonSerializer.Deserialize(json, targetType, _options);
    }
}







