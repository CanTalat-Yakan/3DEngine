using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Engine.DataStructures;

public sealed class Texture2D : IDisposable
{
    public ID3D12Resource Resource;
    public string Name;

    public ResourceStates ResourceStates;

    public ID3D12DescriptorHeap RenderTargetView;
    public ID3D12DescriptorHeap DepthStencilView;

    public int Width;
    public int Height;
    public int MipLevels;

    public Format Format;
    public Format RenderTextureViewFormat;
    public Format DepthStencilViewFormat;
    public Format UnorderedAccessViewFormat;

    public void StateChange(ID3D12GraphicsCommandList commandList, ResourceStates states)
    {
        if (!states.Equals(ResourceStates))
        {
            commandList.ResourceBarrierTransition(Resource, ResourceStates, states);

            ResourceStates = states;
        }
        else if (states.Equals(ResourceStates.UnorderedAccess))
            commandList.ResourceBarrierUnorderedAccessView(Resource);
    }

    public void Dispose()
    {
        Resource?.Dispose();
        Resource = null;
        RenderTargetView?.Dispose();
        RenderTargetView = null;
        DepthStencilView?.Dispose();
        DepthStencilView = null;
    }
}