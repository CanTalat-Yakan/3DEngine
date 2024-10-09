using System.Text;

namespace Engine.Helper;

public enum InputLayoutElementTypes
{
    Float,
    Position2D,
    Position3D,
    Normal,
    Tangent,
    ColorRGBA,
    ColorSRGBA,
    UV
}

public class InputLayoutHelper
{
    private StringBuilder _inputLayoutElements = new();

    public static string GetDefault() =>
        new InputLayoutHelper().CreateInputLayoutDescription(
            InputLayoutElementTypes.Position3D,
            InputLayoutElementTypes.Normal,
            InputLayoutElementTypes.Tangent,
            InputLayoutElementTypes.UV);

    public string GetString() =>
        _inputLayoutElements.ToString();

    public InputLayoutHelper AddFloat()
    {
        _inputLayoutElements.Append(CreateInputLayoutDescription(InputLayoutElementTypes.Float));
        return this;
    }

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
                InputLayoutElementTypes.Float => 'f',
                InputLayoutElementTypes.Position2D => 'p',
                InputLayoutElementTypes.Position3D => 'P',
                InputLayoutElementTypes.UV => 't',
                InputLayoutElementTypes.Tangent => 'T',
                InputLayoutElementTypes.Normal => 'N',
                InputLayoutElementTypes.ColorSRGBA => 'c',
                InputLayoutElementTypes.ColorRGBA => 'C',
                _ => throw new NotImplementedException(),
            };

        return new string(inputLayoutElements);
    }
}