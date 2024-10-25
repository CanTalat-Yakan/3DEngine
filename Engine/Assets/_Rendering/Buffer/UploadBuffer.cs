
using System.Runtime.CompilerServices;

using Vortice.Direct3D12;

namespace Engine.Buffer;

public unsafe partial class UploadBuffer : IDisposable
{
    public ID3D12Resource Resource;
    public int Size;
    
    private IntPtr CPUResourcePointer;
    private ulong GPUResourcePointer;

    private int AllocateIndex = 0;

    private const int DefaultAlignment = 256;
    private const int TextureAlignment = D3D12.TextureDataPlacementAlignment;

    public GraphicsContext GraphicsContext => _graphicsContext ??= Kernel.Instance.Context.GraphicsContext;
    private GraphicsContext _graphicsContext;

    public void Initialize(GraphicsDevice device, int size)
    {
        Size = size;
        device.CreateUploadBuffer(this, size);

        void* pointer = null;
        Resource.Map(0, null, &pointer).CheckError();

        CPUResourcePointer = new IntPtr(pointer);
        GPUResourcePointer = Resource.GPUVirtualAddress;
    }

    public void Dispose()
    {
        Resource?.Dispose();

        GC.SuppressFinalize(this);
    }
}

public unsafe partial class UploadBuffer : IDisposable
{
    public void Upload<T>(Span<T> data, out uint offset) where T : struct
    {
        UploadData(defaultAllignment: true, data.Length * Unsafe.SizeOf<T>(), out var mappedData, out offset);
        data.CopyTo(new Span<T>(mappedData, data.Length));
    }

    public void Upload<T>(T data, out uint offset)
    {
        UploadData(defaultAllignment: true, Unsafe.SizeOf<T>(), out var mappedData, out offset);
        Unsafe.Copy(mappedData, ref data);
    }

    public void UploadData(bool defaultAllignment, int size, out void* mappedData, out uint offset)
    {
        if (defaultAllignment)
        {
            if (!AllocateUploadMemory(size, DefaultAlignment, out offset))
                throw new InvalidOperationException("Not enough space in the RingUploadBuffer.");
        }
        else
        {
            if (!AllocateUploadMemory(AlignUp(size, TextureAlignment), TextureAlignment, out offset))
                throw new InvalidOperationException("Not enough space in the RingUploadBuffer.");
        }

        mappedData = (CPUResourcePointer + (int)offset).ToPointer();
    }
}

public unsafe partial class UploadBuffer : IDisposable
{
    private bool AllocateUploadMemory(int size, int alignment, out uint offset)
    {
        int alignedSize = AlignUp(size, alignment);
        int alignedAllocateIndex = AlignUp(AllocateIndex, alignment);

        if (alignedAllocateIndex + alignedSize > Size)
        {
            // Wrap around if not enough space
            AllocateIndex = 0;
            alignedAllocateIndex = AlignUp(AllocateIndex, alignment);

            if (alignedAllocateIndex + alignedSize > Size)
            {
                offset = 0;
                return false; // Not enough space even after wrap-around
            }
        }

        offset = (uint)alignedAllocateIndex;
        AllocateIndex = (alignedAllocateIndex + alignedSize) % Size;
        return true;
    }

    private int AlignUp(int value, int alignment) =>
        (value + alignment - 1) & ~(alignment - 1);
}
