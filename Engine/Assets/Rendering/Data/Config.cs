namespace Engine.Data;

public class Config
{
    public CameraProjection CameraProjection;
    public RenderMode RenderMode;

    public PresentInterval VSync = PresentInterval.Immediate;
    public double ResolutionScale = 1;
    public MultiSample MultiSample = MultiSample.None;
    public int SupportedSampleCount;

    public void SetMSAA(MultiSample multiSample) =>
        MultiSample = multiSample;

    public void SetVSync(PresentInterval interval) =>
        VSync = interval;

    public void SetResolutionScale(double scale) =>
        ResolutionScale = scale;
}

public enum CameraProjection
{
    Perspective,
    Orthographic
}

public enum RenderMode
{
    Shaded,
    Wireframe,
    ShadedWireframe
}

public enum PresentInterval
{
    Immediate,
    Default,
    One,
    Two,
    Three,
    Four
}

public enum MultiSample
{
    None = 1,
    x2 = 2,
    x4 = 4,
    x8 = 8,
}
