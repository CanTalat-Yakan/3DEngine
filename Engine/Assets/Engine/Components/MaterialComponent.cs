using Vortice.D3DCompiler;
using Vortice.Direct3D11;
using Vortice.DXGI;
using System.IO;
using System;
using System.Runtime.CompilerServices;
using Engine.Data;
using Engine.Helper;
using Engine.Utilities;

namespace Engine.Components
{
    public class MaterialComponent
    {
        private Renderer _d3d;

        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader _pixelShader;
        private ID3D11GeometryShader _geometryShader;

        private ID3D11InputLayout _inputLayout;

        private ID3D11ShaderResourceView _resourceView;
        private ID3D11SamplerState _sampler;
        private ID3D11Buffer _model;

        public MaterialComponent(string shaderFileName, string imageFileName, bool includeGeometryShader = false)
        {
            #region //Get Instances
            _d3d = Renderer.Instance;
            #endregion

            #region //Create InputLayout
            var inputElements = new InputElementDescription[] {
                new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0),
                new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, InputElementDescription.AppendAligned, 0)};
            #endregion

            #region //Create VertexShader
            ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode(shaderFileName, "VS", "vs_4_0");

            _vertexShader = _d3d.Device.CreateVertexShader(vertexShaderByteCode.Span);
            _inputLayout = _d3d.Device.CreateInputLayout(inputElements, vertexShaderByteCode.Span);
            #endregion

            #region //Create PixelShader 
            ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode(shaderFileName, "PS", "ps_4_0");

            _pixelShader = _d3d.Device.CreatePixelShader(pixelShaderByteCode.Span);
            #endregion

            #region //Create GeometryShader
            if (includeGeometryShader)
            {
                ReadOnlyMemory<byte> geometryShaderByteCode = CompileBytecode(shaderFileName, "GS", "ps_4_0");
                _geometryShader = _d3d.Device.CreateGeometryShader(geometryShaderByteCode.Span);
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

            _model = _d3d.Device.CreateBuffer(cbModel, bufferDescription);
            #endregion

            #region //Create Texture and Sampler
            var texture = ImageLoader.LoadTexture(_d3d.Device, imageFileName);
            _resourceView = _d3d.Device.CreateShaderResourceView(texture);

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

            _sampler = _d3d.Device.CreateSamplerState(samplerStateDescription);
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

        public void Render(SPerModelConstantBuffer data)
        {
            _d3d.DeviceContext.IASetInputLayout(_inputLayout);
            _d3d.DeviceContext.VSSetShader(_vertexShader);
            _d3d.DeviceContext.PSSetShader(_pixelShader);
            _d3d.DeviceContext.GSSetShader(_geometryShader);

            unsafe
            {
                // Update constant buffer data
                MappedSubresource mappedResource = _d3d.DeviceContext.Map(_model, MapMode.WriteDiscard);
                Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref data);
                _d3d.DeviceContext.Unmap(_model, 0);
            }
            _d3d.DeviceContext.VSSetConstantBuffer(1, _model);

            _d3d.DeviceContext.PSSetShaderResource(0, _resourceView);
            _d3d.DeviceContext.PSSetSampler(0, _sampler);
        }
    }
}
