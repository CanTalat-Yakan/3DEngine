using System;

namespace Engine.Editor;

public class ToolTipAttribute : Attribute
{
    public string ToolTip;

    public ToolTipAttribute(string toolTip)
    {
        ToolTip = toolTip;
    }
}

public class HeaderAttribute : Attribute
{
    public string CustomHeader;

    public HeaderAttribute(string header)
    {
        CustomHeader = header;
    }
}

public class SliderAttribute : Attribute
{
    public double CustomMin;
    public double CustomMax;

    public SliderAttribute(double min, double max)
    {
        CustomMin = min;
        CustomMax = max;
    }
}

public class SpacerAttribute : Attribute { }
public class ShowAttribute : Attribute { }
public class HideAttribute : Attribute { }
