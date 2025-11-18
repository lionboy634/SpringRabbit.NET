namespace SpringRabbit.NET.Converters;

/// <summary>
/// Binary message converter for byte arrays.
/// </summary>
public class BinaryMessageConverter : IMessageConverter
{
    /// <summary>
    /// Gets the content type for binary messages: "application/octet-stream".
    /// </summary>
    public string ContentType => "application/octet-stream";

    /// <summary>
    /// Converts a byte array to a byte array (pass-through for binary data).
    /// </summary>
    /// <param name="obj">The object to convert. Must be a byte array.</param>
    /// <param name="contentType">When this method returns, contains the content type "application/octet-stream".</param>
    /// <returns>The byte array representation of the object.</returns>
    /// <exception cref="ArgumentException">Thrown when the object is not a byte array.</exception>
    public byte[] ToMessage(object obj, out string? contentType)
    {
        contentType = ContentType;
        
        if (obj is byte[] bytes)
        {
            return bytes;
        }

        throw new ArgumentException($"BinaryMessageConverter can only convert byte[] objects, got {obj.GetType()}", nameof(obj));
    }

    /// <summary>
    /// Converts a byte array to an object of the specified type (only supports byte[]).
    /// </summary>
    /// <param name="body">The byte array containing the binary data.</param>
    /// <param name="targetType">The type of object to convert to. Must be byte[].</param>
    /// <param name="contentType">The content type of the message (optional, not used for binary conversion).</param>
    /// <returns>The byte array, or null if the target type is not byte[].</returns>
    /// <exception cref="ArgumentException">Thrown when the target type is not byte[].</exception>
    public object? FromMessage(byte[] body, Type targetType, string? contentType = null)
    {
        if (targetType == typeof(byte[]))
        {
            return body;
        }

        throw new ArgumentException($"BinaryMessageConverter can only convert to byte[], got {targetType}", nameof(targetType));
    }
}







