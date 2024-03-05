using System.Collections.Generic;
using System.Xml.Serialization;

[XmlInclude(typeof(Vector2))]
[XmlInclude(typeof(Vector3))]
[XmlInclude(typeof(Vector4))]
public class SerializeEntry
{
    public string FieldName;
    public object Value;

    // Parameterless constructor needed for Serialization.
    public SerializeEntry() { }

    public SerializeEntry(string fieldName, object value)
    {
        FieldName = fieldName;
        Value = value;
    }
}

public interface IMaterialBuffer { }

public sealed class SerializeConstantBuffer
{
    public List<SerializeEntry> SerializableProperties { get; private set; } = new();

    public string ShaderName;

    private object _propertiesConstantBuffer;

    public object GetConstantBufferObject() =>
        _propertiesConstantBuffer;

    public void SafeToSerializableConstantBuffer()
    {
        SerializableProperties.Clear();

        foreach (var fieldInfo in _propertiesConstantBuffer.GetType().GetFields())
            SerializableProperties.Add(new(fieldInfo.Name, fieldInfo.GetValue(_propertiesConstantBuffer)));
    }

    public void PasteToPropertiesConstantBuffer()
    {
        foreach (var serializeEntry in SerializableProperties)
            foreach (var fieldInfo in _propertiesConstantBuffer.GetType().GetFields())
                if (fieldInfo.Name.Equals(serializeEntry.FieldName))
                    serializeEntry.Value = fieldInfo.GetValue(_propertiesConstantBuffer);
    }
}