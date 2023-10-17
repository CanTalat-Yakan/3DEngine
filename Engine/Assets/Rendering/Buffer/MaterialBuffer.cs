using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

using Vortice.Direct3D11;

namespace Engine.Rendering;

public interface IMaterialBuffer { }

[XmlInclude(typeof(Vector2))]
[XmlInclude(typeof(Vector3))]
[XmlInclude(typeof(Vector4))]
[XmlInclude(typeof(Color))]
public class SerializeEntry
{
    public string FieldName;
    public object Value;

    public SerializeEntry() { } // Parameterless Ctor needed to Serialize.

    public SerializeEntry(string fieldName, object value)
    {
        FieldName = fieldName;
        Value = value;
    }
}

public class MaterialBuffer()
{
    public string ShaderName;

    public List<SerializeEntry> SerializableProperties = new();

    private object _propertiesConstantBuffer;
    private ID3D11Buffer _properties;
    private ID3D11Buffer _model;

    private Renderer _renderer => Renderer.Instance;

    public object GetPropertiesConstantBuffer() =>
        _propertiesConstantBuffer;

    public void CreatePropertiesConstantBuffer(Type constantBufferType)
    {
        _propertiesConstantBuffer = Activator.CreateInstance(constantBufferType);

        MethodInfo createConstantBufferMethod = _renderer.Device.GetType()
            .GetMethod("CreateConstantBuffer")
            .MakeGenericMethod(constantBufferType);

        _properties = (ID3D11Buffer)createConstantBufferMethod.Invoke(_renderer.Device, null);
    }

    public void UpdatePropertiesConstantBuffer()
    {
        if (_properties is null)
            return;

        Type type = ShaderCompiler.ShaderLibrary.GetShader(ShaderName).ConstantBufferType;

        // Map the constant buffer and copy the properties into it.
        unsafe
        {
            //Map the constant buffer to memory for write access.
            var mappedResource = _renderer.Data.DeviceContext.Map(_properties, MapMode.WriteDiscard);

            // Create a MethodInfo for Unsafe.Copy with the correct generic type.
            MethodInfo genericCopyMethod = typeof(Unsafe)
                .GetMethods().FirstOrDefault(method => method.Name == "Copy")
                .MakeGenericMethod(type);
            // Perform the unsafe copy using reflection
            genericCopyMethod.Invoke(null, new object[] {
                new IntPtr(mappedResource.DataPointer.ToPointer()),
                _propertiesConstantBuffer });

            // Unmap the constant buffer from memory.
            _renderer.Data.DeviceContext.Unmap(_properties, 0);
        }

        //Set the constant buffer in the vertex shader stage of the device context.
        _renderer.Data.SetConstantBufferVS(2, _properties);
        _renderer.Data.SetConstantBufferPS(2, _properties);
    }

    public void CreatePerModelConstantBuffer() =>
        _model = _renderer.Device.CreateConstantBuffer<PerModelConstantBuffer>();

    public void UpdateModelConstantBuffer(PerModelConstantBuffer constantBuffer)
    {
        // Map the constant buffer and copy the models model-view matrix into it.
        unsafe
        {
            // Map the constant buffer to memory for write access.
            var mappedResource = _renderer.Data.DeviceContext.Map(_model, MapMode.WriteDiscard);
            // Copy the data from the constant buffer to the mapped resource.
            Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref constantBuffer);
            // Unmap the constant buffer from memory.
            _renderer.Data.DeviceContext.Unmap(_model, 0);
        }

        // Set the constant buffer in the vertex shader stage of the device context.
        _renderer.Data.SetConstantBufferVS(1, _model);
    }

    public void SafeToSerializableProperties()
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

    public void Dispose()
    {
        _properties?.Dispose();
        _model?.Dispose();
    }
}
