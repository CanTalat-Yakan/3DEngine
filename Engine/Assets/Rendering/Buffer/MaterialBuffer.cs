using ComputeSharp;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

using Vortice.Direct3D12;

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
    private ID3D12Resource _properties;
    private ID3D12Resource _model;

    private Renderer _renderer => Renderer.Instance;

    public object GetPropertiesConstantBuffer() =>
        _propertiesConstantBuffer;

    public void CreatePropertiesConstantBuffer(Type constantBufferType)
    {
        _propertiesConstantBuffer = Activator.CreateInstance(constantBufferType);

        MethodInfo createConstantBufferMethod = _renderer.Device.GetType()
            .GetMethod("CreateConstantBuffer")
            .MakeGenericMethod(constantBufferType);

        _properties = (ID3D12Resource)createConstantBufferMethod.Invoke(_renderer.Device, null);
    }

    public void UpdatePropertiesConstantBuffer()
    {
        if (_properties is null)
            return;

        Type type = ShaderCompiler.ShaderLibrary.GetShader(ShaderName).ConstantBufferType;

        // Map the constant buffer and copy the camera's view-projection matrix and position into it.
        unsafe
        {
            // Map the constant buffer to memory for write access.
            _properties.Map(0);

            // Create a MethodInfo for Unsafe.Copy with the correct generic type.
            MethodInfo copyMethod = typeof(Unsafe)
                .GetMethods().FirstOrDefault(method => method.Name == "Copy")
                .MakeGenericMethod(type);
            // Perform the unsafe copy using reflection
            copyMethod.Invoke(null, new object[] {
                new IntPtr(_properties.NativePointer.ToPointer()),
                _propertiesConstantBuffer });

            // Unmap the constant buffer from memory.
            _properties.Unmap(0);
        }

        //Set the constant buffer in the vertex shader stage of the device context.
        _renderer.Data.CommandList.SetGraphicsRootConstantBufferView(2, _properties.GPUVirtualAddress);
    }

    public void CreatePerModelConstantBuffer()
    {
        //Create View Constant Buffer when Camera is initialized.
        _model = _renderer.Device.CreateCommittedResource(
            new HeapProperties(HeapType.Upload),
            HeapFlags.None,
            ResourceDescription.Buffer(Unsafe.SizeOf<PerModelConstantBuffer>()),
            ResourceStates.GenericRead); // The resource is in a readable state.
    }

    public void UpdateModelConstantBuffer(PerModelConstantBuffer constantBuffer)
    {
        // Map the constant buffer and copy the camera's view-projection matrix and position into it.
        unsafe
        {
            // Map the constant buffer to memory for write access.
            _model.Map(0);

            // Copy the data from the constant buffer to the mapped resource.
            Unsafe.Copy(_model.NativePointer.ToPointer(), ref constantBuffer);

            // Unmap the constant buffer from memory.
            _model.Unmap(0);
        }

        // Set the constant buffer in the vertex shader stage of the device context.
        _renderer.Data.CommandList.SetGraphicsRootConstantBufferView(1, _model.GPUVirtualAddress);
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
