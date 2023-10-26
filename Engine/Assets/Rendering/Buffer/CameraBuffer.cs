using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Engine.Rendering;

public sealed unsafe class CameraBuffer
{
    public ViewConstantBuffer ViewConstantBuffer;

    private ID3D12Resource _view;

    private Renderer _renderer => Renderer.Instance;

    public CameraBuffer()
    {
        //Create View Constant Buffer.
        _view = _renderer.Device.CreateCommittedResource(
            HeapType.Upload,
            ResourceDescription.Buffer(sizeof(ViewConstantBuffer)),
            ResourceStates.VertexAndConstantBuffer);
    }

    public void UpdateConstantBuffer()
    {
        // Map the constant buffer and copy the camera's view-projection matrix and position into it.
        var pointer = _view.Map<ViewConstantBuffer>(0);
        // Copy the data from the constant buffer to the mapped resource.
        Unsafe.Copy(pointer, ref ViewConstantBuffer);
        // Unmap the constant buffer from memory.
        _view.Unmap(0);

        // Set the constant buffer in the vertex shader stage of the device context.
        _renderer.Data.Material?.CommandList.SetGraphicsRootConstantBufferView(0, _view.GPUVirtualAddress);
    }

    public void Dispose() =>
        _view?.Dispose();
}