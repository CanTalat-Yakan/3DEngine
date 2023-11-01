using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Engine.Rendering;

public sealed unsafe class CameraBuffer
{
    public ViewConstantBuffer ViewConstantBuffer;

    private ID3D12Resource _view;

    internal Renderer Renderer => _renderer is not null ? _renderer : _renderer = Renderer.Instance;
    private Renderer _renderer;

    public CameraBuffer()
    {
        //Create View Constant Buffer.
        _view = Renderer.Device.CreateCommittedResource(
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
    }

    public void Setup()
    {
        // Set the constant buffer in the vertex shader stage of the device context.
        Renderer.Data.CommandList.SetGraphicsRootConstantBufferView(0, _view.GPUVirtualAddress);
        Renderer.Data.CommandList.Close();
    }

    public void Dispose() =>
        _view?.Dispose();
}