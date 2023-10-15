namespace Engine.Rendering;

internal class ComputeMaterialBuffer
{
    public string ComputeShaderName;

    private object _propertiesConstantBuffer;

    private Renderer _renderer => Renderer.Instance;
}
