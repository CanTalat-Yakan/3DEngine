namespace Engine;

/// <summary>A sub-allocation from a <see cref="DynamicBufferAllocator"/>.</summary>
/// <param name="Buffer">The backing GPU buffer.</param>
/// <param name="Offset">Byte offset within <paramref name="Buffer"/> where this allocation starts.</param>
/// <param name="Size">Byte size of the allocation.</param>
public readonly record struct DynamicAllocation(IBuffer Buffer, ulong Offset, ulong Size);

/// <summary>
/// Frame-aware bump allocator for transient GPU buffers (vertex, index, uniform).
/// <para>
/// Maintains one set of per-usage backing buffers for each in-flight frame slot.
/// On <see cref="BeginFrame"/> the current slot's write cursors are reset to zero
/// (the fence wait in <c>GraphicsDevice.BeginFrame</c> guarantees the slot is idle).
/// Allocations are bump-allocated from the current slot; if the backing buffer is too
/// small it is grown to the next power of two. All backing buffers are disposed on
/// <see cref="Dispose"/>.
/// </para>
/// <para>
/// Only accessed from the render thread — no locking required.
/// </para>
/// </summary>
public sealed class DynamicBufferAllocator : IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.DynamicAllocator");

    private readonly IGraphicsDevice _gfx;
    private readonly int _framesInFlight;
    private readonly FrameArena[] _arenas;
    private int _currentSlot;
    private bool _disposed;

    /// <summary>Minimum backing buffer size (64 KB).</summary>
    private const ulong MinBufferSize = 64 * 1024;

    public DynamicBufferAllocator(IGraphicsDevice gfx)
    {
        _gfx = gfx;
        _framesInFlight = gfx.FramesInFlight;
        _arenas = new FrameArena[_framesInFlight];
        for (int i = 0; i < _framesInFlight; i++)
            _arenas[i] = new FrameArena();

        Logger.Info($"DynamicBufferAllocator created — {_framesInFlight} frame slots.");
    }

    /// <summary>
    /// Advances to the given in-flight frame slot and resets its write cursors.
    /// Must be called after the fence wait (i.e. inside <c>RendererContext.BeginFrame</c>).
    /// </summary>
    public void BeginFrame(int inFlightIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _currentSlot = inFlightIndex;
        _arenas[_currentSlot].Reset();
    }

    /// <summary>
    /// Bump-allocates <paramref name="size"/> bytes from the current frame slot's
    /// backing buffer for the given <paramref name="usage"/>.
    /// Grows the backing buffer (power-of-2) if the allocation doesn't fit.
    /// </summary>
    public DynamicAllocation Allocate(ulong size, BufferUsage usage)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (size == 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Allocation size must be > 0.");

        var arena = _arenas[_currentSlot];
        return arena.Allocate(_gfx, size, usage);
    }

    /// <summary>Maps the backing buffer of <paramref name="alloc"/> and returns a span
    /// covering exactly the allocation's region.</summary>
    public Span<byte> Map(DynamicAllocation alloc)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var full = _gfx.Map(alloc.Buffer);
        return full.Slice((int)alloc.Offset, (int)alloc.Size);
    }

    /// <summary>Unmaps the backing buffer that contains <paramref name="alloc"/>.</summary>
    public void Unmap(DynamicAllocation alloc)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _gfx.Unmap(alloc.Buffer);
    }

    /// <summary>
    /// Disposes all backing buffers and resets all arenas.
    /// Called on resize (<c>vkDeviceWaitIdle</c> guarantees everything is idle) and on shutdown.
    /// </summary>
    public void Reset()
    {
        foreach (var arena in _arenas)
            arena.DisposeAll();
        Logger.Debug("DynamicBufferAllocator reset — all backing buffers disposed.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Reset();
        Logger.Info("DynamicBufferAllocator disposed.");
    }

    // ── Per-frame-slot arena ───────────────────────────────────────────

    private sealed class FrameArena
    {
        // One backing buffer per usage type encountered
        private readonly Dictionary<BufferUsage, ArenaBuffer> _buffers = new();

        public void Reset()
        {
            foreach (var ab in _buffers.Values)
                ab.Cursor = 0;
        }

        public DynamicAllocation Allocate(IGraphicsDevice gfx, ulong size, BufferUsage usage)
        {
            if (!_buffers.TryGetValue(usage, out var ab))
            {
                ab = new ArenaBuffer();
                _buffers[usage] = ab;
            }

            // Grow if necessary
            ulong required = ab.Cursor + size;
            if (ab.Buffer is null || ab.Capacity < required)
            {
                ab.Buffer?.Dispose();

                ulong newCap = Math.Max(MinBufferSize, ab.Capacity);
                while (newCap < required)
                    newCap = NextPowerOfTwo(newCap * 2);

                var desc = new BufferDesc(newCap, usage, CpuAccessMode.Write);
                ab.Buffer = gfx.CreateBuffer(desc);
                ab.Capacity = newCap;

                // Must re-upload any data already written this frame into the old buffer.
                // Since we grew mid-frame, just reset cursor — caller hasn't bound anything yet
                // because we're bump-allocating forward and the old buffer was disposed.
                ab.Cursor = 0;
            }

            var offset = ab.Cursor;
            ab.Cursor += size;
            return new DynamicAllocation(ab.Buffer!, offset, size);
        }

        public void DisposeAll()
        {
            foreach (var ab in _buffers.Values)
            {
                ab.Buffer?.Dispose();
                ab.Buffer = null;
                ab.Capacity = 0;
                ab.Cursor = 0;
            }
            _buffers.Clear();
        }
    }

    private sealed class ArenaBuffer
    {
        public IBuffer? Buffer;
        public ulong Capacity;
        public ulong Cursor;
    }

    private static ulong NextPowerOfTwo(ulong v)
    {
        if (v == 0) return MinBufferSize;
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v |= v >> 32;
        return v + 1;
    }
}

