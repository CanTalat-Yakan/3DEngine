using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

using Vortice.Direct3D12;

namespace Engine.Rendering;

[XmlInclude(typeof(Vector2))]
[XmlInclude(typeof(Vector3))]
[XmlInclude(typeof(Vector4))]
public class SerializeEntry
{
    public string FieldName;
    public object Value;

    // Parameterless constructor needed to Serialize.
    public SerializeEntry() { } 

    public SerializeEntry(string fieldName, object value)
    {
        FieldName = fieldName;
        Value = value;
    }
}

public interface IMaterialBuffer { }

public unsafe partial class MaterialBuffer
{
    public string ShaderName;

    public List<SerializeEntry> SerializableProperties = new();

    private object _propertiesConstantBuffer;

    private ID3D12Resource _properties;
    private ID3D12Resource _model;

    internal Renderer Renderer => _renderer is not null ? _renderer : _renderer = Renderer.Instance;
    private Renderer _renderer;

    public void Dispose()
    {
        _properties?.Dispose();
        _model?.Dispose();
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
}

public unsafe partial class MaterialBuffer
{
    public void CreatePerModelConstantBuffer()
    {
        //Create Per Model Constant Buffer.
        _model = Renderer.Device.CreateCommittedResource(
            HeapType.Upload,
            ResourceDescription.Buffer(sizeof(PerModelConstantBuffer)),
            ResourceStates.GenericRead);
        _model.Name = "PerModel ConstantBuffer";
    }

    public void UpdateModelConstantBuffer(PerModelConstantBuffer constantBuffer)
    {
        // Map the constant buffer and copy the camera's view-projection matrix and position into it.
        var pointer = _model.Map<PerModelConstantBuffer>(0);
        // Copy the data from the constant buffer to the mapped resource.
        Unsafe.Copy(pointer, ref constantBuffer);
        // Unmap the constant buffer from memory.
        _model.Unmap(0);

        // Set the constant buffer in the vertex shader stage of the device context.
        Renderer.Data.Material?.CommandList.SetGraphicsRootConstantBufferView(1, _model.GPUVirtualAddress);
    }
}

public unsafe partial class MaterialBuffer
{
    public object GetPropertiesConstantBuffer() =>
        _propertiesConstantBuffer;

    public void CreatePropertiesConstantBuffer(Type constantBufferType)
    {
        _propertiesConstantBuffer = Activator.CreateInstance(constantBufferType);

        MethodInfo sizeOfMethod = typeof(Unsafe)
            .GetMethod("SizeOf")
            .MakeGenericMethod(constantBufferType);
        var dynamicPropertiesMemoryLength = (int)sizeOfMethod.Invoke(null, null);

        MethodInfo createConstantBufferMethod = Renderer.Device.GetType()
            .GetMethod("CreateCommittedResource")
            .MakeGenericMethod(constantBufferType);
        _properties = (ID3D12Resource)createConstantBufferMethod.Invoke(Renderer.Device, new object[]
        {
            HeapType.Upload,
            ResourceDescription.Buffer(dynamicPropertiesMemoryLength),
            ResourceStates.GenericRead
        });
        _properties.Name = "Properties ConstantBuffer";
    }

    public void UpdatePropertiesConstantBuffer()
    {
        if (_properties is null)
            return;

        Type type = ShaderCompiler.ShaderLibrary.GetShader(ShaderName).ConstantBufferType;

        // Map the constant buffer and copy the view-projection matrix and position of the camera into it.
        MethodInfo mapMethod = typeof(ID3D12Resource)
            .GetMethod("Map")
            .MakeGenericMethod(type);
        // Perform the map using reflection
        var pointer = mapMethod.Invoke(null, new object[] { 0 });

        // Create a MethodInfo for Unsafe.Copy with the correct generic type.
        MethodInfo copyMethod = typeof(Unsafe)
            .GetMethods().FirstOrDefault(method => method.Name == "Copy")
            .MakeGenericMethod(type);
        // Perform the unsafe copy using reflection
        copyMethod.Invoke(null, new object[]
        {
            pointer,
            _propertiesConstantBuffer
        });

        // Unmap the constant buffer from memory.
        _properties.Unmap(0);

        //Set the constant buffer in the vertex shader stage of the device context.
        Renderer.Data.Material?.CommandList.SetGraphicsRootConstantBufferView(10, _properties.GPUVirtualAddress);
    }
}