namespace SpringRabbit.NET;

/// <summary>
/// Interface for converting messages to and from byte arrays.
/// </summary>
public interface IMessageConverter
{
    /// <summary>
    /// Converts an object to a byte array.
    /// </summary>
    byte[] ToMessage(object obj, out string? contentType);

    /// <summary>
    /// Converts a byte array to an object of the specified type.
    /// </summary>
    object? FromMessage(byte[] body, Type targetType, string? contentType = null);

    /// <summary>
    /// Gets the content type this converter handles.
    /// </summary>
    string ContentType { get; }
}

