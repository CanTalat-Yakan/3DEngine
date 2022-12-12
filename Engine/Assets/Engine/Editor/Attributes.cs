using System;

namespace Engine.Editor
{
    internal class SliderAttribute : Attribute
    {
        public double CustomMin;
        public double CustomMax;

        public SliderAttribute(double min, double max)
        {
            CustomMin = min;
            CustomMax = max;
        }
    }

    internal class HeaderAttribute : Attribute
    {
        public string CustomHeader;

        public HeaderAttribute(string header)
        {
            CustomHeader = header;
        }
    }

    internal class SpacerAttribute : Attribute { }

    internal class ShowAttribute : Attribute { }

    internal class HideAttribute : Attribute { }
}