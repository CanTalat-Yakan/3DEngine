using System.Runtime.CompilerServices;

using Vortice.Direct3D11;

namespace Engine.Utilities;

public sealed class CameraBuffer
{
    public ID3D11Buffer View;
    public ViewConstantBuffer ViewConstantBuffer;

    private Renderer _renderer => Renderer.Instance;

    public void UpdateConstantBuffer()
    {
        // Map the constant buffer and copy the camera's view-projection matrix and position into it.
        unsafe
        {
            // Map the constant buffer to memory for write access.
            MappedSubresource mappedResource = _renderer.Data.DeviceContext.Map(View, MapMode.WriteDiscard);
            // Copy the data from the constant buffer to the mapped resource.
            Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref ViewConstantBuffer);
            // Unmap the constant buffer from memory.
            _renderer.Data.DeviceContext.Unmap(View, 0);
        }

        // Set the constant buffer in the vertex shader stage of the device context.
        _renderer.Data.SetConstantBuffer(0, View);
    }

    public void Dispose() =>
        View?.Dispose();
}