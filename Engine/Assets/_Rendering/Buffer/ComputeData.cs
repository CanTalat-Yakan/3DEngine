using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Vortice.Direct3D12;

namespace Engine.Buffer;

public sealed class ComputeData : IDisposable
{
    public string Name;

    private Texture2D _texture2D;
    private ID3D12Resource _bufferResource;
    private ID3D12Resource _readbackBuffer;

    public CommonContext Context => _commonContext ??= Kernel.Instance.Context;
    private CommonContext _commonContext;

    // Method to set a pre-initialized Texture2D for compute shader usage
    public void SetTexture2D(Texture2D texture)
    {
        if (texture is null || texture.Resource is null)
            throw new ArgumentNullException(nameof(texture), "The provided Texture2D must be initialized and have a valid resource.");

        _texture2D = texture;

        // Example data for uploading, replace with actual data if needed
        byte[] textureData = new byte[_texture2D.Width * _texture2D.Height * 4]; // Assuming RGBA 8 bits per channel
        Context.UploadBuffer.Upload(textureData, out uint textureUploadOffset);

        Context.GraphicsDevice.Device.CreateCommandList(0, CommandListType.Compute, Context.GraphicsDevice.GetComputeCommandAllocator(), null,
            out ID3D12GraphicsCommandList5 commandList).ThrowIfFailed();

        commandList.CopyBufferRegion(_texture2D.Resource, 0, Context.UploadBuffer.Resource, textureUploadOffset, (ulong)textureData.Length);

        // Use the StateChange method to transition the resource state for compute usage
        _texture2D.StateChange(Context.ComputeContext.CommandList, ResourceStates.UnorderedAccess);

        commandList.Close();
        Context.GraphicsDevice.CommandQueue.ExecuteCommandList(commandList);

        commandList.Dispose();
    }

    // Method to set or upload data for general-purpose buffers
    public void SetData<T>(T[] data, ResourceFlags flags = ResourceFlags.None) where T : struct
    {
        int sizeInBytes = Marshal.SizeOf<T>() * data.Length;

        _bufferResource = Context.GraphicsDevice.Device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            ResourceDescription.Buffer((ulong)sizeInBytes, flags),
            ResourceStates.CopyDest
        );

        // Use RingUploadBuffer to upload the data to the GPU
        Context.UploadBuffer.Upload(data, out uint bufferUploadOffset);

        Context.GraphicsDevice.Device.CreateCommandList(0, CommandListType.Compute, Context.GraphicsDevice.GetComputeCommandAllocator(), null,
            out ID3D12GraphicsCommandList5 commandList).ThrowIfFailed();

        commandList.CopyBufferRegion(_bufferResource, 0, Context.UploadBuffer.Resource, bufferUploadOffset, (ulong)sizeInBytes);
        commandList.ResourceBarrierTransition(_bufferResource, ResourceStates.CopyDest, ResourceStates.UnorderedAccess);

        commandList.Close();
        Context.GraphicsDevice.CommandQueue.ExecuteCommandList(commandList);

        commandList.Dispose();

    }

    // Method to read back data from a buffer (e.g., a structured buffer)
    public T[] ReadData<T>(int elementCount) where T : struct
    {
        int sizeInBytes = Marshal.SizeOf<T>() * elementCount;

        Context.GraphicsDevice.Device.CreateCommandList(0, CommandListType.Compute, Context.GraphicsDevice.GetComputeCommandAllocator(), null, 
            out ID3D12GraphicsCommandList5 commandList).ThrowIfFailed();

        // Create a readback buffer
        _readbackBuffer = Context.GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
            new HeapProperties(HeapType.Readback),
            HeapFlags.None,
            ResourceDescription.Buffer((uint)sizeInBytes),
            ResourceStates.CopyDest
        );

        // Copy the buffer resource to the readback buffer
        commandList.CopyResource(_readbackBuffer, _bufferResource);

        commandList.Close();
        Context.GraphicsDevice.CommandQueue.ExecuteCommandList(commandList);

        commandList.Dispose();

        T[] readbackData = new T[elementCount];

        unsafe
        {
            Context.UploadBuffer.UploadData(true, sizeInBytes, out var mappedData, out var offset);
            Unsafe.Copy(mappedData, ref readbackData);
        }

        return readbackData;
    }

    public void Dispose()
    {
        _texture2D?.Dispose();
        _texture2D = null;

        _bufferResource?.Dispose();
        _bufferResource = null;

        _readbackBuffer?.Dispose();
        _readbackBuffer = null;

        GC.SuppressFinalize(this);
    }
}