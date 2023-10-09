﻿using System.Reflection;
using System.Runtime.CompilerServices;

using Vortice.Direct3D11;

namespace Engine.Rendering;

public interface IMaterialBuffer { }

public class MaterialBuffer()
{
    public string ShaderName;

    public object PropertiesConstantBuffer;

    private ID3D11Buffer _properties;

    private Renderer _renderer => Renderer.Instance;

    public void CreateConstantBuffer<T>() where T : unmanaged =>
        _properties = _renderer.Device.CreateConstantBuffer<T>();

    public void CreateConstantBuffer(Type constantBufferType)
    {
        MethodInfo createConstantBufferMethod = _renderer.Device.GetType()
            .GetMethod("CreateConstantBuffer")
            .MakeGenericMethod(constantBufferType);
        _properties = (ID3D11Buffer)createConstantBufferMethod.Invoke(_renderer.Device, null);
    }

    public void UpdateConstantBuffer()
    {
        // Map the constant buffer and copy the properties into it.
        unsafe
        {
            //Map the constant buffer to memory for write access.
            MappedSubresource mappedResource = _renderer.Data.DeviceContext.Map(_properties, MapMode.WriteDiscard);
            // Copy the data from the constant buffer to the mapped resource.
            Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref PropertiesConstantBuffer);
            // Unmap the constant buffer from memory.
            _renderer.Data.DeviceContext.Unmap(_properties, 0);
        }

        //Set the constant buffer in the vertex shader stage of the device context.
        _renderer.Data.SetConstantBuffer(10, _properties);
    }

    public void Dispose() =>
        _properties?.Dispose();
}