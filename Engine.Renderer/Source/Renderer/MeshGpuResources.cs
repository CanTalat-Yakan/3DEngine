using System.Collections.Concurrent;
using System.Numerics;

namespace Engine;

/// <summary>GPU resource registry mapping logical mesh IDs to vertex buffers.</summary>
public sealed class MeshGpuRegistry
{
    private readonly ConcurrentDictionary<int, IBuffer> _vertexBuffers = new();

    public bool TryGet(int entityId, out IBuffer buffer) => _vertexBuffers.TryGetValue(entityId, out buffer!);

    public IBuffer GetOrCreate(int entityId, Mesh mesh, IGraphicsDevice device)
    {
        return _vertexBuffers.GetOrAdd(entityId, _ => CreateVertexBuffer(mesh, device));
    }

    private static IBuffer CreateVertexBuffer(Mesh mesh, IGraphicsDevice device)
    {
        if (mesh.Positions is null || mesh.Positions.Length == 0)
            throw new InvalidOperationException("Mesh has no vertex positions.");

        int count = mesh.Positions.Length;
        int stride = System.Runtime.InteropServices.Marshal.SizeOf<Vector3>();
        ulong size = (ulong)(count * stride);

        var desc = new BufferDesc(size, BufferUsage.Vertex | BufferUsage.TransferDst, CpuAccessMode.None);
        var buffer = device.CreateBuffer(desc);

        if (device is GraphicsDevice gd)
        {
            Span<byte> temp = stackalloc byte[(int)size];
            var spanVec = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, Vector3>(temp);
            mesh.Positions.AsSpan().CopyTo(spanVec);
            gd.UploadBuffer(buffer, temp, 0);
        }

        return buffer;
    }
}
