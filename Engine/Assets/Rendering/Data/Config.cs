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

public class Config
{
    public CameraProjection CameraProjection;
    public RenderMode RenderMode;

    public PresentInterval VSync = PresentInterval.Immediate;
    public double ResolutionScale = 1;

    public void SetVSync(PresentInterval interval) =>
        VSync = interval;

    public void SetResolutionScale(double scale) =>
        ResolutionScale = scale;
}
