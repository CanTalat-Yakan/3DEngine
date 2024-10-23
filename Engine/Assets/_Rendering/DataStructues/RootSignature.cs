using System.Collections.Generic;

using Vortice.Direct3D12;

namespace Engine.DataStructures;

public enum RootSignatureParameters
{
    ConstantBufferView,
    ConstantBufferViewTable,
    ShaderResourceView,
    ShaderResourceViewTable,
    UnorderedAccessView,
    UnorderedAccessViewTable,
}

public sealed class RootSignature : IDisposable
{
    public Dictionary<uint, uint> ConstantBufferView = new();
    public Dictionary<uint, uint> ShaderResourceView = new();
    public Dictionary<uint, uint> UnorderedAccessView = new();

    public ID3D12RootSignature Resource;
    public string Name;

    public void Dispose()
    {
        Resource?.Dispose();
        Resource = null;

        GC.SuppressFinalize(this);
    }
}