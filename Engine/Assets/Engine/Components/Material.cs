using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using System.IO;
using System;
using Vortice.WIC;
using System.Runtime.CompilerServices;
using Engine.Data;
using Engine.Helper;
using Engine.Utilities;

namespace Engine.Components
{
    internal class Material
    {
        Renderer d3d;

        ID3D11VertexShader vertexShader;
        ID3D11PixelShader pixelShader;
        ID3D11GeometryShader geometryShader;

        ID3D11InputLayout inputLayout;

        ID3D11ShaderResourceView resourceView;
        ID3D11SamplerState sampler;
        ID3D11Buffer model;

        internal Material(string _shaderFileName, string _imageFileName, bool _includeGeometryShader = false)
        {
            #region //Get Instances
            d3d = Renderer.Instance;
            #endregion

            #region //Create InputLayout
            var inputElements = new InputElementDescription[] {
                new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0),
                new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, InputElementDescription.AppendAligned, 0)};
            #endregion

            #region //Create VertexShader
            ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode(_shaderFileName, "VS", "vs_4_0");

            vertexShader = d3d.device.CreateVertexShader(vertexShaderByteCode.Span);
            inputLayout = d3d.device.CreateInputLayout(inputElements, vertexShaderByteCode.Span);
            #endregion

            #region //Create PixelShader 
            ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode(_shaderFileName, "PS", "ps_4_0");

            pixelShader = d3d.device.CreatePixelShader(pixelShaderByteCode.Span);
            #endregion

            #region //Create GeometryShader
            if (_includeGeometryShader)
            {
                ReadOnlyMemory<byte> geometryShaderByteCode = CompileBytecode(_shaderFileName, "GS", "ps_4_0");
                geometryShader = d3d.device.CreateGeometryShader(geometryShaderByteCode.Span);
            }
            #endregion

            #region //Create ConstantBuffers for Model
            SPerModelConstantBuffer cbModel = new SPerModelConstantBuffer();

            BufferDescription bufferDescription = new BufferDescription
            {
                BindFlags = BindFlags.ConstantBuffer,
                CPUAccessFlags = CpuAccessFlags.Write,
                Usage = ResourceUsage.Dynamic,
            };

            model = d3d.device.CreateBuffer(cbModel, bufferDescription);
            #endregion

            #region //Create Texture and Sampler
            var texture = ImageLoader.LoadTexture(d3d.device, _imageFileName);
            resourceView = d3d.device.CreateShaderResourceView(texture);

            SamplerDescription samplerStateDescription = new SamplerDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                ComparisonFunction = ComparisonFunction.Always,
                MaxAnisotropy = 16,
                MinLOD = 0,
                MaxLOD = float.MaxValue,
            };

            sampler = d3d.device.CreateSamplerState(samplerStateDescription);
            #endregion
        }

        protected static ReadOnlyMemory<byte> CompileBytecode(string shaderName, string entryPoint, string profile)
        {
            string assetsPath = Path.Combine(AppContext.BaseDirectory, @"Assets\Engine\Resources\");
            string fileName = Path.Combine(assetsPath, shaderName);
            //string shaderSource = File.ReadAllText(Path.Combine(assetsPath, shaderName));

            ShaderFlags shaderFlags = ShaderFlags.EnableStrictness;
#if DEBUG
            shaderFlags |= ShaderFlags.Debug;
            shaderFlags |= ShaderFlags.SkipValidation;
#else
        shaderFlags |= ShaderFlags.OptimizationLevel3;
#endif

            return Compiler.CompileFromFile(fileName, entryPoint, profile, shaderFlags);
        }

        internal void Render(SPerModelConstantBuffer _data)
        {
            d3d.deviceContext.IASetInputLayout(inputLayout);
            d3d.deviceContext.VSSetShader(vertexShader);
            d3d.deviceContext.PSSetShader(pixelShader);
            d3d.deviceContext.GSSetShader(geometryShader);

            unsafe
            {
                // Update constant buffer data
                MappedSubresource mappedResource = d3d.deviceContext.Map(model, MapMode.WriteDiscard);
                Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref _data);
                d3d.deviceContext.Unmap(model, 0);
            }
            d3d.deviceContext.VSSetConstantBuffer(1, model);

            d3d.deviceContext.PSSetShaderResource(0, resourceView);
            d3d.deviceContext.PSSetSampler(0, sampler);
        }
    }
}
