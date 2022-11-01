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
        Renderer m_d3d;

        ID3D11VertexShader m_vertexShader;
        ID3D11PixelShader m_pixelShader;
        ID3D11GeometryShader m_geometryShader;

        ID3D11InputLayout m_inputLayout;

        ID3D11ShaderResourceView m_resourceView;
        ID3D11SamplerState m_sampler;
        ID3D11Buffer m_model;

        internal Material(string _shaderFileName, string _imageFileName, bool _includeGeometryShader = false)
        {
            #region //Get Instances
            m_d3d = Renderer.Instance;
            #endregion

            #region //Create InputLayout
            var inputElements = new InputElementDescription[] {
                new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0),
                new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, InputElementDescription.AppendAligned, 0)};
            #endregion

            #region //Create VertexShader
            ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode(_shaderFileName, "VS", "vs_4_0");

            m_vertexShader = m_d3d.m_Device.CreateVertexShader(vertexShaderByteCode.Span);
            m_inputLayout = m_d3d.m_Device.CreateInputLayout(inputElements, vertexShaderByteCode.Span);
            #endregion

            #region //Create PixelShader 
            ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode(_shaderFileName, "PS", "ps_4_0");

            m_pixelShader = m_d3d.m_Device.CreatePixelShader(pixelShaderByteCode.Span);
            #endregion

            #region //Create GeometryShader
            if (_includeGeometryShader)
            {
                ReadOnlyMemory<byte> geometryShaderByteCode = CompileBytecode(_shaderFileName, "GS", "ps_4_0");
                m_geometryShader = m_d3d.m_Device.CreateGeometryShader(geometryShaderByteCode.Span);
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

            m_model = m_d3d.m_Device.CreateBuffer(cbModel, bufferDescription);
            #endregion

            #region //Create Texture and Sampler
            var texture = ImageLoader.LoadTexture(m_d3d.m_Device, _imageFileName);
            m_resourceView = m_d3d.m_Device.CreateShaderResourceView(texture);

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

            m_sampler = m_d3d.m_Device.CreateSamplerState(samplerStateDescription);
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
            m_d3d.m_DeviceContext.IASetInputLayout(m_inputLayout);
            m_d3d.m_DeviceContext.VSSetShader(m_vertexShader);
            m_d3d.m_DeviceContext.PSSetShader(m_pixelShader);
            m_d3d.m_DeviceContext.GSSetShader(m_geometryShader);

            unsafe
            {
                // Update constant buffer data
                MappedSubresource mappedResource = m_d3d.m_DeviceContext.Map(m_model, MapMode.WriteDiscard);
                Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref _data);
                m_d3d.m_DeviceContext.Unmap(m_model, 0);
            }
            m_d3d.m_DeviceContext.VSSetConstantBuffer(1, m_model);

            m_d3d.m_DeviceContext.PSSetShaderResource(0, m_resourceView);
            m_d3d.m_DeviceContext.PSSetSampler(0, m_sampler);
        }
    }
}
