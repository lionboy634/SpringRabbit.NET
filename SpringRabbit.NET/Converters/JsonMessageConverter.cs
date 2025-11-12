using System.Text;
using System.Text.Json;

namespace SpringRabbit.NET.Converters;

/// <summary>
/// JSON message converter using System.Text.Json.
/// </summary>
public class JsonMessageConverter : IMessageConverter
{
    private readonly JsonSerializerOptions _options;

    public JsonMessageConverter(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public string ContentType => "application/json";

    public byte[] ToMessage(object obj, out string? contentType)
    {
        contentType = ContentType;
        var json = JsonSerializer.Serialize(obj, _options);
        return Encoding.UTF8.GetBytes(json);
    }

    public object? FromMessage(byte[] body, Type targetType, string? contentType = null)
    {
        var json = Encoding.UTF8.GetString(body);
        return JsonSerializer.Deserialize(json, targetType, _options);
    }
}

