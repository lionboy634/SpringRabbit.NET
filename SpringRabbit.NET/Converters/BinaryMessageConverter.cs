namespace SpringRabbit.NET.Converters;

/// <summary>
/// Binary message converter for byte arrays.
/// </summary>
public class BinaryMessageConverter : IMessageConverter
{
    public string ContentType => "application/octet-stream";

    public byte[] ToMessage(object obj, out string? contentType)
    {
        contentType = ContentType;
        
        if (obj is byte[] bytes)
        {
            return bytes;
        }

        throw new ArgumentException($"BinaryMessageConverter can only convert byte[] objects, got {obj.GetType()}", nameof(obj));
    }

    public object? FromMessage(byte[] body, Type targetType, string? contentType = null)
    {
        if (targetType == typeof(byte[]))
        {
            return body;
        }

        throw new ArgumentException($"BinaryMessageConverter can only convert to byte[], got {targetType}", nameof(targetType));
    }
}


