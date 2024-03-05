using Vortice.Direct3D12;

namespace Engine.DataTypes;

public sealed class DescriptorHeapX : IDisposable
{
    public ID3D12DescriptorHeap Heap;

    public uint AllocatedCount;
    public uint DescriptorCount;
    public uint IncrementSize;

    public void Initialize(GraphicsDevice graphicsDevice, DescriptorHeapDescription descriptorHeapDescription)
    {
        AllocatedCount = 0;
        DescriptorCount = (uint)descriptorHeapDescription.DescriptorCount;

        graphicsDevice.Device.CreateDescriptorHeap(descriptorHeapDescription, out Heap).ThrowIfFailed();

        IncrementSize = (uint)graphicsDevice.Device.GetDescriptorHandleIncrementSize(descriptorHeapDescription.Type);
    }

    public void GetTemporaryHandle(out CpuDescriptorHandle handleCPU, out GpuDescriptorHandle handleGPU)
    {
        handleCPU = Heap.GetCPUDescriptorHandleForHeapStart();
        handleCPU.Ptr += AllocatedCount * IncrementSize;

        handleGPU = Heap.GetGPUDescriptorHandleForHeapStart();
        handleGPU.Ptr += AllocatedCount * IncrementSize;

        AllocatedCount = (AllocatedCount + 1) % DescriptorCount;
    }

    public CpuDescriptorHandle GetTemporaryCPUHandle()
    {
        CpuDescriptorHandle handle = Heap.GetCPUDescriptorHandleForHeapStart();
        handle.Ptr += AllocatedCount * IncrementSize;

        AllocatedCount = (AllocatedCount + 1) % DescriptorCount;

        return handle;
    }

    public void Dispose()
    {
        Heap?.Dispose();
        Heap = null;
    }
}