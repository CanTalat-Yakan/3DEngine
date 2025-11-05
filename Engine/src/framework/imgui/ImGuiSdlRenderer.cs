using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using SDL3;

namespace Engine;

/// <summary>
/// Renders ImGui draw data using the SDL3 renderer API.
/// </summary>
/// <remarks>
/// - Uploads the ImGui font atlas into an SDL texture and assigns it to ImGui's texture ID system.
/// - Converts ImGui's draw lists into batched SDL vertex/index buffers and issues SDL.RenderGeometry calls.
/// - Manages clipping rectangles per draw command and enables alpha blending.
/// - This renderer targets SDL's 2D renderer (not a GPU backend like Vulkan/Direct3D).
/// - Not thread-safe; call methods from the same thread that owns the SDL renderer.
/// </remarks>
public sealed class ImGuiSdlRenderer : IDisposable
{
    // SDL renderer handle used for all draw calls.
    private readonly nint _renderer;

    // SDL texture containing the uploaded ImGui font atlas.
    private nint _fontTexture;

    // Tracks whether resources have already been disposed.
    private bool _disposed;

    /// <summary>
    /// Creates a new ImGui renderer bound to an existing SDL renderer.
    /// </summary>
    /// <param name="renderer">The SDL renderer handle created alongside a window.</param>
    /// <remarks>
    /// The constructor uploads ImGui device objects (font texture) immediately.
    /// </remarks>
    public ImGuiSdlRenderer(nint renderer)
    {
        _renderer = renderer;
        CreateDeviceObjects();
    }

    /// <summary>
    /// Prepares ImGui's IO for a new frame by updating display size and framebuffer scale.
    /// </summary>
    /// <param name="window">The SDL window associated with the renderer.</param>
    /// <remarks>
    /// - DisplaySize is set to the logical window size (in points).
    /// - DisplayFramebufferScale is set based on the renderer output size vs. window size (for HiDPI).
    /// </remarks>
    public void NewFrame(nint window)
    {
        var io = ImGui.GetIO();
        SDL.GetWindowSize(window, out int w, out int h);
        SDL.GetRenderOutputSize(_renderer, out int fbW, out int fbH);
        io.DisplaySize = new Vector2(w, h);
        if (w > 0 && h > 0)
        {
            io.DisplayFramebufferScale = new Vector2(fbW / (float)w, fbH / (float)h);
        }
    }

    /// <summary>
    /// Renders the provided ImGui draw data using the SDL renderer.
    /// </summary>
    /// <param name="drawData">Aggregated draw lists from ImGui for the current frame.</param>
    /// <remarks>
    /// - Enables blending, applies per-command clip rectangles, and draws geometry in batches.
    /// - Returns early if there is nothing to draw or the framebuffer size is zero.
    /// </remarks>
    public unsafe void RenderDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0)
            return;

        SDL.SetRenderDrawBlendMode(_renderer, SDL.BlendMode.Blend);

        int fbWidth = (int)(drawData.DisplaySize.X * drawData.FramebufferScale.X);
        int fbHeight = (int)(drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
        if (fbWidth <= 0 || fbHeight <= 0)
            return;

        var clipOff = drawData.DisplayPos;
        var clipScale = drawData.FramebufferScale;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            var vtxSpan = new ReadOnlySpan<ImDrawVert>(cmdList.VtxBuffer.Data.ToPointer(), cmdList.VtxBuffer.Size);
            var idxSpan = new ReadOnlySpan<ushort>(cmdList.IdxBuffer.Data.ToPointer(), cmdList.IdxBuffer.Size);

            fixed (ImDrawVert* vtxPtr = vtxSpan)
            fixed (ushort* idxPtr = idxSpan)
            {
                for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
                {
                    var pcmd = cmdList.CmdBuffer[cmdi];

                    Vector4 clipRect;
                    clipRect.X = (pcmd.ClipRect.X - clipOff.X) * clipScale.X;
                    clipRect.Y = (pcmd.ClipRect.Y - clipOff.Y) * clipScale.Y;
                    clipRect.Z = (pcmd.ClipRect.Z - clipOff.X) * clipScale.X;
                    clipRect.W = (pcmd.ClipRect.W - clipOff.Y) * clipScale.Y;

                    if (clipRect.X < fbWidth && clipRect.Y < fbHeight && clipRect.Z >= 0f && clipRect.W >= 0f)
                    {
                        var sdlRect = new SDL.Rect
                        {
                            X = (int)clipRect.X,
                            Y = (int)clipRect.Y,
                            W = (int)(clipRect.Z - clipRect.X),
                            H = (int)(clipRect.W - clipRect.Y)
                        };
                        SDL.SetRenderClipRect(_renderer, sdlRect);

                        var texture = (nint)pcmd.TextureId;

                        int baseIdx = (int)pcmd.IdxOffset;
                        int baseVtx = (int)pcmd.VtxOffset;
                        RenderGeometry(vtxPtr, idxPtr, baseIdx, baseVtx, pcmd.ElemCount, texture);
                    }
                }
            }
        }

        SDL.SetRenderClipRect(_renderer, IntPtr.Zero);
    }

    /// <summary>
    /// Converts a portion of ImGui's indexed triangle list into SDL geometry and submits it.
    /// </summary>
    /// <param name="vtx">Pointer to the first ImGui vertex.</param>
    /// <param name="idx">Pointer to the first ImGui index.</param>
    /// <param name="baseIdx">Starting index into the <paramref name="idx"/> array.</param>
    /// <param name="baseVtx">Offset to add to indices to get absolute vertex positions.</param>
    /// <param name="elemCount">Number of indices to draw (must be a multiple of 3).</param>
    /// <param name="texture">SDL texture handle to use for this draw batch.</param>
    /// <remarks>
    /// Batches geometry in chunks (MaxBatchIndices) and issues SDL.RenderGeometry per batch.
    /// </remarks>
    private unsafe void RenderGeometry(ImDrawVert* vtx, ushort* idx, int baseIdx, int baseVtx, uint elemCount, nint texture)
    {
        const int MaxBatchIndices = 2048;
        uint remaining = elemCount;
        int localBase = baseIdx;
        while (remaining > 0)
        {
            int thisBatch = (int)Math.Min(remaining, (uint)MaxBatchIndices);
            // ensure triangle multiple
            thisBatch -= thisBatch % 3;
            if (thisBatch <= 0) break;

            int vtxCount = 0;
            var vtxArr = new SDL.Vertex[thisBatch];
            var idxArr = new int[thisBatch];

            for (int i = 0; i < thisBatch; i += 3)
            {
                int i0 = baseVtx + idx[localBase + i + 0];
                int i1 = baseVtx + idx[localBase + i + 1];
                int i2 = baseVtx + idx[localBase + i + 2];

                var v0 = vtx[i0];
                var v1 = vtx[i1];
                var v2 = vtx[i2];

                vtxArr[vtxCount + 0] = ToSdlVertex(v0);
                vtxArr[vtxCount + 1] = ToSdlVertex(v1);
                vtxArr[vtxCount + 2] = ToSdlVertex(v2);

                idxArr[vtxCount + 0] = vtxCount + 0;
                idxArr[vtxCount + 1] = vtxCount + 1;
                idxArr[vtxCount + 2] = vtxCount + 2;

                vtxCount += 3;
            }

            SDL.RenderGeometry(_renderer, texture, vtxArr, vtxCount, idxArr, vtxCount);

            localBase += thisBatch;
            remaining -= (uint)thisBatch;
        }
    }

    /// <summary>
    /// Converts an ImGui vertex to an SDL vertex, handling position, UV, and packed color.
    /// </summary>
    /// <param name="v">The ImGui vertex.</param>
    /// <returns>An SDL vertex with equivalent attributes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SDL.Vertex ToSdlVertex(ImDrawVert v)
    {
        uint color = v.col;
        float r = (color & 0xFF) / 255f;
        float g = ((color >> 8) & 0xFF) / 255f;
        float b = ((color >> 16) & 0xFF) / 255f;
        float a = ((color >> 24) & 0xFF) / 255f;
        return new SDL.Vertex
        {
            Position = new SDL.FPoint { X = v.pos.X, Y = v.pos.Y },
            TexCoord = new SDL.FPoint { X = v.uv.X, Y = v.uv.Y },
            Color = new SDL.FColor { R = r, G = g, B = b, A = a }
        };
    }

    /// <summary>
    /// Creates device resources required by the renderer (font texture).
    /// </summary>
    /// <remarks>
    /// - Retrieves the ImGui font atlas pixels and uploads them into an SDL texture.
    /// - Sets the resulting texture as ImGui's font texture ID for subsequent rendering.
    /// </remarks>
    private unsafe void CreateDeviceObjects()
    {
        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out _);

        _fontTexture = SDL.CreateTexture(_renderer, SDL.PixelFormat.ABGR8888, SDL.TextureAccess.Static, width, height);
        SDL.SetTextureBlendMode(_fontTexture, SDL.BlendMode.Blend);

        SDL.UpdateTexture(_fontTexture, IntPtr.Zero, pixels, width * 4);

        io.Fonts.SetTexID(_fontTexture);
        io.Fonts.ClearTexData();
    }

    /// <summary>
    /// Disposes renderer resources owned by this instance.
    /// </summary>
    /// <remarks>
    /// Safe to call multiple times; subsequent calls are no-ops. Destroys the font texture if created.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_fontTexture != IntPtr.Zero)
        {
            SDL.DestroyTexture(_fontTexture);
        }
    }
}