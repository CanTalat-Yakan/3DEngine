﻿using System.Runtime.CompilerServices;

using Vortice.Direct3D12;

namespace Engine.Rendering;

public sealed unsafe class CameraBuffer
{
    public ViewConstantBuffer ViewConstantBuffer;

    private ID3D12Resource _view;

    internal Renderer Renderer => _renderer ??= Renderer.Instance;
    private Renderer _renderer;

    public CameraBuffer()
    {
        //Create View Constant Buffer.
        _view = Renderer.Device.CreateCommittedResource(
            HeapType.Upload,
            ResourceDescription.Buffer(sizeof(ViewConstantBuffer)),
            ResourceStates.GenericRead);
        _view.Name = "View ConstantBuffer";
    }

    public void UpdateConstantBuffer()
    {
        if (Renderer.CheckDeviceRemoved()) return;

        // Map the constant buffer and copy the view-projection matrix and position of the camera into it.
        void* pointer;
        _view.Map(0, &pointer);
        // Copy the data from the constant buffer to the mapped resource.
        Unsafe.Copy(pointer, ref ViewConstantBuffer);
        // Unmap the constant buffer from memory.
        _view.Unmap(0);
    }

    public void Setup()
    {
        //Renderer.Data.CommandList.ResourceBarrierTransition(_view, ResourceStates.GenericRead, ResourceStates.Common);

        // Set the constant buffer in the vertex shader stage of the device context.
        Renderer.Data.CommandList.SetGraphicsRootConstantBufferView(0, _view.GPUVirtualAddress);
    }

    public void Dispose() =>
        _view?.Dispose();
}