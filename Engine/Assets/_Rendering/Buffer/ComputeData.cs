
using System.Runtime.CompilerServices;

namespace Engine.Buffer;

internal class ComputeData
{
    public GraphicsDevice GraphicsDevice;

    public RingUploadBuffer UploadBuffer = new();

    public ComputeData(GraphicsDevice graphicsDevice) =>
        GraphicsDevice = graphicsDevice;

    public void SetData<T>(T[] data) where T : struct
    {
        UploadBuffer.Initialize(GraphicsDevice, Unsafe.SizeOf<T>() * data.Length);
    }

    public void SetTexture(Texture2D texture2D)
    {

    }

    public void ReadData()
    {

    }
}
