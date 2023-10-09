using System.Reflection;
using System.Runtime.CompilerServices;

using Vortice.Direct3D11;

namespace Engine.Rendering;
public interface IMaterialBuffer { }

public class MaterialBuffer(string shaderName)
{
    public readonly string ShaderName = shaderName;

    public ID3D11Buffer Properties;
    public object PropertiesConstantBuffer;

    private Renderer _renderer => Renderer.Instance;

    public void CreateConstantBuffer<T>() where T : unmanaged =>
        Properties = _renderer.Device.CreateConstantBuffer<T>();

    public void CreateConstantBuffer(Type constantBufferType)
    {
        MethodInfo createConstantBufferMethod = _renderer.Device.GetType()
            .GetMethod("CreateConstantBuffer")
            .MakeGenericMethod(constantBufferType);
        Properties = (ID3D11Buffer)createConstantBufferMethod.Invoke(_renderer.Device, null);
    }

    public void UpdateConstantBuffer()
    {
        // Map the constant buffer and copy the properties into it.
        unsafe
        {
            //Map the constant buffer to memory for write access.
            MappedSubresource mappedResource = _renderer.Data.DeviceContext.Map(Properties, MapMode.WriteDiscard);
            // Copy the data from the constant buffer to the mapped resource.
            Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref PropertiesConstantBuffer);
            // Unmap the constant buffer from memory.
            _renderer.Data.DeviceContext.Unmap(Properties, 0);
        }

        //Set the constant buffer in the vertex shader stage of the device context.
        _renderer.Data.SetConstantBuffer(10, Properties);
    }

    public void Dispose() =>
        Properties?.Dispose();
}
