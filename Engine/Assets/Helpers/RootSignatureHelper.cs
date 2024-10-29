using System.Text;

namespace Engine.Helpers;

public enum RootSignatureParameterTypes
{
    ConstantBufferView,
    ConstantBufferViewTable,
    ShaderResourceView,
    ShaderResourceViewTable,
    UnorderedAccessView,
    UnorderedAccessViewTable
}

public class RootSignatureHelper
{
    private StringBuilder _rootSignatureParameters = new();

    public static string GetDefault() =>
        new RootSignatureHelper().CreateRootSignatureParameters(
            RootSignatureParameterTypes.ConstantBufferView,
            RootSignatureParameterTypes.ConstantBufferView,
            RootSignatureParameterTypes.ShaderResourceViewTable);

    public string GetString() =>
        _rootSignatureParameters.ToString();

    public RootSignatureHelper AddConstantBufferView(int count = 1)
    {
        for (int i = 0; i < count; i++)
            _rootSignatureParameters.Append(CreateRootSignatureParameters(RootSignatureParameterTypes.ConstantBufferView));
        
        return this;
    }

    public RootSignatureHelper AddConstantBufferViewTable(int count = 1)
    {
        for (int i = 0; i < count; i++)
            _rootSignatureParameters.Append(CreateRootSignatureParameters(RootSignatureParameterTypes.ConstantBufferViewTable));
    
        return this;
    }

    public RootSignatureHelper AddShaderResourceView(int count = 1)
    {
        for (int i = 0; i < count; i++)
            _rootSignatureParameters.Append(CreateRootSignatureParameters(RootSignatureParameterTypes.ShaderResourceView));
        
        return this;
    }

    public RootSignatureHelper AddShaderResourceViewTable(int count = 1)
    {
        for (int i = 0; i < count; i++)
            _rootSignatureParameters.Append(CreateRootSignatureParameters(RootSignatureParameterTypes.ShaderResourceViewTable));
        
        return this;
    }

    public RootSignatureHelper AddUnorderedAccessView(int count = 1)
    {
        for (int i = 0; i < count; i++)
            _rootSignatureParameters.Append(CreateRootSignatureParameters(RootSignatureParameterTypes.UnorderedAccessView));
        
        return this;
    }

    public RootSignatureHelper AddUnorderedAccessViewTable(int count = 1)
    {
        for (int i = 0; i < count; i++)
            _rootSignatureParameters.Append(CreateRootSignatureParameters(RootSignatureParameterTypes.UnorderedAccessViewTable));
        
        return this;
    }

    public string CreateRootSignatureParameters(params RootSignatureParameterTypes[] elements)
    {
        var rootSignatureParameters = new char[elements.Length];

        for (int i = 0; i < elements.Length; i++)
            rootSignatureParameters[i] = elements[i] switch
            {
                RootSignatureParameterTypes.ConstantBufferView => 'C',
                RootSignatureParameterTypes.ConstantBufferViewTable => 'c',
                RootSignatureParameterTypes.ShaderResourceView => 'S',
                RootSignatureParameterTypes.ShaderResourceViewTable => 's',
                RootSignatureParameterTypes.UnorderedAccessView => 'U',
                RootSignatureParameterTypes.UnorderedAccessViewTable => 'u',
                _ => throw new NotImplementedException(),
            };

        return new string(rootSignatureParameters);
    }
}