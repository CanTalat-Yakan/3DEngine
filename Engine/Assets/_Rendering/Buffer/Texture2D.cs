using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Engine.Buffer;

public sealed class Texture2D : IDisposable
{
    public ID3D12Resource Resource;
    public ResourceStates ResourceStates;

    public Format Format;

    public bool AllowUnorderedAccess = false;

    public string Name;

    public uint Width;
    public uint Height;
    public uint MipLevels;

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

        GC.SuppressFinalize(this);
    }
}