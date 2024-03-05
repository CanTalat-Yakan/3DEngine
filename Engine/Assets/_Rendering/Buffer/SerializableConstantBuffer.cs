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

public sealed class SerializableConstantBuffer
{
    public List<SerializeEntry> SerializableProperties { get; private set; } = new();

    public string ShaderName;

    private object _constantBuffer;

    public object GetConstantBufferObject() =>
        _constantBuffer;
    
    public object SetConstantBufferObject(object constantBuffer) =>
        _constantBuffer = constantBuffer;

    public void SafeToSerializableConstantBuffer()
    {
        SerializableProperties.Clear();

        foreach (var fieldInfo in _constantBuffer.GetType().GetFields())
            SerializableProperties.Add(new(fieldInfo.Name, fieldInfo.GetValue(_constantBuffer)));
    }

    public void PasteToPropertiesConstantBuffer()
    {
        foreach (var serializeEntry in SerializableProperties)
            foreach (var fieldInfo in _constantBuffer.GetType().GetFields())
                if (fieldInfo.Name.Equals(serializeEntry.FieldName))
                    serializeEntry.Value = fieldInfo.GetValue(_constantBuffer);
    }
}