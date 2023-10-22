using System.Runtime.CompilerServices;

using Vortice.Direct3D12;

namespace Engine.Rendering;

public sealed class CameraBuffer
{
    public ViewConstantBuffer ViewConstantBuffer;

    private ID3D12Resource _view;

    private Renderer _renderer => Renderer.Instance;

    public CameraBuffer()
    {
        //Create View Constant Buffer when Camera is initialized.
        _view = _renderer.Device.CreateCommittedResource(
            new HeapProperties(HeapType.Upload),
            HeapFlags.None,
            ResourceDescription.Buffer(Unsafe.SizeOf<ViewConstantBuffer>()),
            ResourceStates.GenericRead); // The resource is in a readable state.
    }

    public void UpdateConstantBuffer()
    {
        // Map the constant buffer and copy the camera's view-projection matrix and position into it.
        unsafe
        {
            // Map the constant buffer to memory for write access.
            _view.Map(0);

            // Copy the data from the constant buffer to the mapped resource.
            Unsafe.Copy(_view.NativePointer.ToPointer(), ref ViewConstantBuffer);

            // Unmap the constant buffer from memory.
            _view.Unmap(0);
        }
        
        // Set the constant buffer in the vertex shader stage of the device context.
        _renderer.Data.CommandList.SetGraphicsRootConstantBufferView(0, _view.GPUVirtualAddress);
    }

    public void Dispose() =>
        _view?.Dispose();
}