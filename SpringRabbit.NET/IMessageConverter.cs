namespace SpringRabbit.NET;

/// <summary>
/// Interface for converting messages to and from byte arrays.
/// </summary>
public interface IMessageConverter
{
    /// <summary>
    /// Converts an object to a byte array.
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <param name="contentType">When this method returns, contains the content type for the converted message.</param>
    /// <returns>A byte array representing the serialized object.</returns>
    byte[] ToMessage(object obj, out string? contentType);

    /// <summary>
    /// Converts a byte array to an object of the specified type.
    /// </summary>
    /// <param name="body">The byte array containing the serialized data.</param>
    /// <param name="targetType">The type of object to deserialize to.</param>
    /// <param name="contentType">The content type of the message (optional, may be used for deserialization logic).</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    object? FromMessage(byte[] body, Type targetType, string? contentType = null);

    /// <summary>
    /// Gets the content type this converter handles (e.g., "application/json", "application/xml", "application/octet-stream").
    /// </summary>
    /// <value>The MIME content type string.</value>
    string ContentType { get; }
}







