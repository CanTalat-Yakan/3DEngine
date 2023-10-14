namespace Engine.Data;

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
    x16 = 16
}

public class Config
{
    public CameraProjection CameraProjection;
    public RenderMode RenderMode;

    public MultiSample MultiSample = MultiSample.None;
    public PresentInterval VSync = PresentInterval.Immediate;
    public double ResolutionScale = 1;

    public void SetMSAA(MultiSample multiSample) =>
        MultiSample = multiSample;

    public void SetVSync(PresentInterval interval) =>
        VSync = interval;

    public void SetResolutionScale(double scale) =>
        ResolutionScale = scale;
}
