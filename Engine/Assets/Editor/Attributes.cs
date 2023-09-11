namespace Engine.Editor;

public sealed class ToolTipAttribute : Attribute
{
    public string ToolTip;

    public ToolTipAttribute(string toolTip)
    {
        ToolTip = toolTip;
    }
}

public sealed class HeaderAttribute : Attribute
{
    public string CustomHeader;

    public HeaderAttribute(string header)
    {
        CustomHeader = header;
    }
}

public sealed class SliderAttribute : Attribute
{
    public double CustomMin;
    public double CustomMax;

    public SliderAttribute(double min, double max)
    {
        CustomMin = min;
        CustomMax = max;
    }
}

public sealed class SpacerAttribute : Attribute { }
public sealed class ShowAttribute : Attribute { }
public sealed class HideAttribute : Attribute { }
