using System.Text;
using System.Xml.Serialization;

namespace SpringRabbit.NET.Converters;

/// <summary>
/// XML message converter using System.Xml.Serialization.
/// </summary>
public class XmlMessageConverter : IMessageConverter
{
    public string ContentType => "application/xml";

    public byte[] ToMessage(object obj, out string? contentType)
    {
        contentType = ContentType;
        var serializer = new XmlSerializer(obj.GetType());
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, obj);
        return Encoding.UTF8.GetBytes(stringWriter.ToString());
    }

    public object? FromMessage(byte[] body, Type targetType, string? contentType = null)
    {
        var xml = Encoding.UTF8.GetString(body);
        var serializer = new XmlSerializer(targetType);
        using var stringReader = new StringReader(xml);
        return serializer.Deserialize(stringReader);
    }
}

