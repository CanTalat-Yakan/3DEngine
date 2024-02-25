﻿using System.Threading;

using Vortice.Direct3D12;
using Vortice.Direct3D;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.Data;

public struct RenderData
{
    public Material_OLD Material { get; set; }

    public IDXGISwapChain3 SwapChain;

    public ID3D12CommandQueue GraphicsQueue;

    public ID3D12CommandAllocator CommandAllocator;
    public ID3D12GraphicsCommandList4 CommandList;

    public ID3D12Fence FrameFence;
    public AutoResetEvent FrameFenceEvent;

    public ID3D12Resource[] BufferRenderTargetTextures;
    public ID3D12DescriptorHeap BufferRenderTargetView;

    public ID3D12Resource OutputRenderTargetTexture;
    public ID3D12DescriptorHeap OutputRenderTargetView;

    public ID3D12Resource DepthStencilTexture;
    public ID3D12DescriptorHeap DepthStencilView;

    public RasterizerDescription RasterizerDescription;
    public BlendDescription BlendDescription;

    public PrimitiveTopology PrimitiveTopology;

    // 10 bits == Format.R10G10B10A2_UNorm, 16 bits == Format.R16G16B16A16_UNorm.
    public const Format RenderTargetFormat = Format.R8G8B8A8_UNorm; 
    public const Format DepthStencilFormat = Format.D32_Float;

    public const int RenderLatency = 2;

    public ulong FrameIndex => FrameCount % RenderLatency;
    public ulong FrameCount;

    public void SetViewport(SizeI size)
    {
        CommandList.RSSetViewport(new Viewport(size.Width, size.Height));
        CommandList.RSSetScissorRect(size.Width, size.Height);
    }

    public void SetupInputAssembler(IndexBufferView indexBufferView, params VertexBufferView[] vertexBufferViews)
    {
        CommandList.IASetVertexBuffers(0, vertexBufferViews);
        CommandList.IASetIndexBuffer(indexBufferView);
        CommandList.IASetPrimitiveTopology(PrimitiveTopology);
    }

    public void Dispose()
    {
        foreach (var materialEntry in MaterialCompiler.Library.Materials)
            materialEntry.Material.Dispose();

        SwapChain?.Dispose();

        GraphicsQueue?.Dispose();

        FrameFence?.Dispose();
        FrameFenceEvent?.Dispose();

        DisposeTexturesAndViews();
    }

    public void DisposeTexturesAndViews()
    {
        foreach (var bufferRenderTargetTexture in BufferRenderTargetTextures)
            bufferRenderTargetTexture?.Dispose();
        BufferRenderTargetView?.Dispose();

        OutputRenderTargetView?.Dispose();
        OutputRenderTargetTexture?.Dispose();

        DepthStencilTexture?.Dispose();
        DepthStencilView?.Dispose();
    }
}