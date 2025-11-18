using System.Text;
using System.Xml.Serialization;

namespace SpringRabbit.NET.Converters;

/// <summary>
/// XML message converter using System.Xml.Serialization.
/// </summary>
public class XmlMessageConverter : IMessageConverter
{
    /// <summary>
    /// Gets the content type for XML messages: "application/xml".
    /// </summary>
    public string ContentType => "application/xml";

    /// <summary>
    /// Converts an object to a byte array using XML serialization.
    /// </summary>
    /// <param name="obj">The object to convert to XML.</param>
    /// <param name="contentType">When this method returns, contains the content type "application/xml".</param>
    /// <returns>A byte array containing the XML representation of the object.</returns>
    public byte[] ToMessage(object obj, out string? contentType)
    {
        contentType = ContentType;
        var serializer = new XmlSerializer(obj.GetType());
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, obj);
        return Encoding.UTF8.GetBytes(stringWriter.ToString());
    }

    /// <summary>
    /// Converts a byte array containing XML to an object of the specified type.
    /// </summary>
    /// <param name="body">The byte array containing the XML data.</param>
    /// <param name="targetType">The type of object to deserialize to.</param>
    /// <param name="contentType">The content type of the message (optional, not used for XML deserialization).</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    public object? FromMessage(byte[] body, Type targetType, string? contentType = null)
    {
        var xml = Encoding.UTF8.GetString(body);
        var serializer = new XmlSerializer(targetType);
        using var stringReader = new StringReader(xml);
        return serializer.Deserialize(stringReader);
    }
}







