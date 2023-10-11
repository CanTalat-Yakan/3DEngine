using System.Runtime.CompilerServices;

using Vortice.Direct3D11;

namespace Engine.Rendering;

public sealed class CameraBuffer
{
    public ViewConstantBuffer ViewConstantBuffer;

    private ID3D11Buffer _view;

    private Renderer _renderer => Renderer.Instance;

    public CameraBuffer() =>
        //Create View Constant Buffer when Camera is initialized.
        _view = _renderer.Device.CreateConstantBuffer<ViewConstantBuffer>();

    public void UpdateConstantBuffer()
    {
        // Map the constant buffer and copy the camera's view-projection matrix and position into it.
        unsafe
        {
            // Map the constant buffer to memory for write access.
            MappedSubresource mappedResource = _renderer.Data.DeviceContext.Map(_view, MapMode.WriteDiscard);
            // Copy the data from the constant buffer to the mapped resource.
            Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref ViewConstantBuffer);
            // Unmap the constant buffer from memory.
            _renderer.Data.DeviceContext.Unmap(_view, 0);
        }

        // Set the constant buffer in the vertex shader stage of the device context.
        _renderer.Data.SetConstantBufferVS(0, _view);
    }

    public void Dispose() =>
        _view?.Dispose();
}