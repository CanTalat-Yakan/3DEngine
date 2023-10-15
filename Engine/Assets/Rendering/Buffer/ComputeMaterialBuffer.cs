namespace Engine.Rendering;

internal class ComputeMaterialBuffer
{
    public string ShaderName;

    private object _propertiesConstantBuffer;

    private Renderer _renderer => Renderer.Instance;
}
