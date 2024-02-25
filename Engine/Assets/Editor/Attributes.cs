namespace Engine.Editor;

public sealed class ColorAttribute : Attribute { }
public sealed class SpaceAttribute : Attribute { }
public sealed class ShowAttribute : Attribute { }
public sealed class ShowOnlyAttribute : Attribute { }
public sealed class HideAttribute : Attribute { }

public sealed class ToolTipAttribute(string toolTip) : Attribute
{
    public string ToolTip = toolTip;
}

public sealed class HeaderAttribute(string header) : Attribute
{
    public string Header = header;
}

public sealed class SliderAttribute(double min, double max) : Attribute
{
    public double Min = min;
    public double Max = max;
}

public sealed class IfAttribute(string fieldName, string value) : Attribute
{
    public string FieldName = fieldName;
    public string Value = value;
}

public sealed class IfNotAttribute(string fieldName, string value) : Attribute
{
    public string FieldName = fieldName;
    public string Value = value;
}