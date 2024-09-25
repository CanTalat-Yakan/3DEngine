using System.Text;

namespace Engine.DataStructures;

public enum InputLayoutElementTypes
{
    Position3D,
    Position2D,
    Normal,
    Tangent,
    ColorRGBA,
    ColorSRGBA,
    UV
}

public class InputLayoutHelper
{
    private StringBuilder _inputLayoutElements = new();

    public string GetDefault() =>
        CreateInputLayoutDescription(
            InputLayoutElementTypes.Position3D,
            InputLayoutElementTypes.Normal,
            InputLayoutElementTypes.Tangent,
            InputLayoutElementTypes.UV);

    public string GetString() => 
        _inputLayoutElements.ToString();

    public InputLayoutHelper AddPosition3D()
    {
        _inputLayoutElements.Append(CreateInputLayoutDescription(InputLayoutElementTypes.Position3D));

        return this;
    }

    public InputLayoutHelper AddPosition2D()
    {
        _inputLayoutElements.Append(CreateInputLayoutDescription(InputLayoutElementTypes.Position2D));

        return this;
    }

    public InputLayoutHelper AddNormal()
    {
        _inputLayoutElements.Append(CreateInputLayoutDescription(InputLayoutElementTypes.Normal));

        return this;
    }

    public InputLayoutHelper AddTangent()
    {
        _inputLayoutElements.Append(CreateInputLayoutDescription(InputLayoutElementTypes.Tangent));

        return this;
    }

    public InputLayoutHelper AddColorRGBA()
    {
        _inputLayoutElements.Append(CreateInputLayoutDescription(InputLayoutElementTypes.ColorRGBA));

        return this;
    }

    public InputLayoutHelper AddColorSRGBA()
    {
        _inputLayoutElements.Append(CreateInputLayoutDescription(InputLayoutElementTypes.ColorSRGBA));

        return this;
    }

    public InputLayoutHelper AddUV()
    {
        _inputLayoutElements.Append(CreateInputLayoutDescription(InputLayoutElementTypes.UV));

        return this;
    }

    public string CreateInputLayoutDescription(params InputLayoutElementTypes[] elements)
    {
        var inputLayoutElements = new char[elements.Length];

        for (int i = 0; i < elements.Length; i++)
            inputLayoutElements[i] = elements[i] switch
            {
                InputLayoutElementTypes.Position3D => 'P',
                InputLayoutElementTypes.Position2D => 'p',
                InputLayoutElementTypes.Normal => 'N',
                InputLayoutElementTypes.Tangent => 'T',
                InputLayoutElementTypes.UV => 't',
                InputLayoutElementTypes.ColorRGBA => 'C',
                InputLayoutElementTypes.ColorSRGBA => 'c',
                _ => throw new NotImplementedException(),
            };

        return new string(inputLayoutElements);
    }
}