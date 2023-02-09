using Vortice.D3DCompiler;
using Vortice.Direct3D11;
using Vortice.DXGI;
using System.IO;
using System.Runtime.CompilerServices;
using System;
using Engine.Data;
using Engine.Helper;

namespace Engine.Utilities
{
    internal class Material
    {
        private Renderer _d3d { get => Renderer.Instance; }

        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader _pixelShader;
        private ID3D11GeometryShader _geometryShader;

        private ID3D11InputLayout _inputLayout;

        private ID3D11ShaderResourceView _resourceView;
        private ID3D11SamplerState _sampler;
        private ID3D11Buffer _model;

        public Material(string shaderFileName, string imageFileName, bool includeGeometryShader = false)
        {
            #region //Create InputLayout
            // Define the input layout for the vertex buffer.
            InputElementDescription[] inputElements = new[] {
                new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0), // Position element.
                new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0), // Texture coordinate element.
                new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, InputElementDescription.AppendAligned, 0)}; // Normal element.
            #endregion

            #region //Create VertexShader
            // Compile the vertex shader bytecode from the specified shader file name.
            ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode(shaderFileName, "VS", "vs_4_0");

            // Create the vertex shader using the compiled bytecode.
            _vertexShader = _d3d.Device.CreateVertexShader(vertexShaderByteCode.Span);
            // Create the input layout using the specified input elements and vertex shader bytecode.
            _inputLayout = _d3d.Device.CreateInputLayout(inputElements, vertexShaderByteCode.Span);
            #endregion

            #region //Create PixelShader 
            // Compile the bytecode for the pixel shader using the specified shader file name and target profile.
            ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode(shaderFileName, "PS", "ps_4_0");

            // Create the pixel shader using the compiled bytecode.
            _pixelShader = _d3d.Device.CreatePixelShader(pixelShaderByteCode.Span);
            #endregion

            #region //Create GeometryShader
            // This code creates a Geometry Shader, if the includeGeometryShader flag is set to true.
            if (includeGeometryShader)
            {
                // Compile the bytecode for the geometry shader using the specified shader file name and target profile.
                ReadOnlyMemory<byte> geometryShaderByteCode = CompileBytecode(shaderFileName, "GS", "ps_4_0");

                // Create the geometry shader using the compiled bytecode.
                _geometryShader = _d3d.Device.CreateGeometryShader(geometryShaderByteCode.Span);
            }
            #endregion

            #region //Create ConstantBuffers for Model
            // Create the constant buffer for model-related data.
            SPerModelConstantBuffer cbModel = new();

            // Set up the description for the constant buffer.
            BufferDescription bufferDescription = new()
            {
                BindFlags = BindFlags.ConstantBuffer,
                CPUAccessFlags = CpuAccessFlags.Write,
                Usage = ResourceUsage.Dynamic,
            };

            // Create the constant buffer with the given description.
            _model = _d3d.Device.CreateBuffer(cbModel, bufferDescription);
            #endregion

            #region //Create Texture and Sampler
            // Load the texture and create a shader resource view for it.
            var texture = ImageLoader.LoadTexture(_d3d.Device, imageFileName);
            _resourceView = _d3d.Device.CreateShaderResourceView(texture);

            // Set the properties for the sampler state.
            SamplerDescription samplerStateDescription = new()
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

            // Create the sampler state using the sampler state description.
            _sampler = _d3d.Device.CreateSamplerState(samplerStateDescription);
            #endregion
        }

        public void Set(SPerModelConstantBuffer constantBuffer)
        {
            // Set input layout, vertex shader, and pixel shader in the device context.
            _d3d.DeviceContext.IASetInputLayout(_inputLayout);
            _d3d.DeviceContext.VSSetShader(_vertexShader);
            _d3d.DeviceContext.PSSetShader(_pixelShader);
            _d3d.DeviceContext.GSSetShader(_geometryShader);

            #region //Update constant buffer data
            // Map the constant buffer and copy the models's model-view matrix into it.
            unsafe
            {
                // Map the constant buffer to memory for write access.
                MappedSubresource mappedResource = _d3d.DeviceContext.Map(_model, MapMode.WriteDiscard);
                // Copy the data from the constant buffer to the mapped resource.
                Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref constantBuffer);
                // Unmap the constant buffer from memory.
                _d3d.DeviceContext.Unmap(_model, 0);

            }
            #endregion

            // Set the constant buffer in the vertex shader stage of the device context.
            _d3d.DeviceContext.VSSetConstantBuffer(1, _model);

            // Set the shader resource and sampler in the pixel shader stage of the device context.
            _d3d.DeviceContext.PSSetShaderResource(0, _resourceView);
            _d3d.DeviceContext.PSSetSampler(0, _sampler);
        }

        protected static ReadOnlyMemory<byte> CompileBytecode(string shaderPath, string entryPoint, string profile)
        {
            // Combine the base directory and the relative path to the resources directory.
            string resourcesPath = Path.Combine(AppContext.BaseDirectory, @"Assets\Engine\Resources\");
            // Define the full path to the shader file.
            string shaderFilePath = Path.Combine(resourcesPath, shaderPath);

            // Shader flags to enable strictness and set optimization level or debug mode.
            ShaderFlags shaderFlags = ShaderFlags.EnableStrictness;
#if DEBUG
            shaderFlags |= ShaderFlags.Debug;
            shaderFlags |= ShaderFlags.SkipValidation;
#else
            shaderFlags |= ShaderFlags.OptimizationLevel3;
#endif

            // Compile the shader from the specified file using the specified entry point, profile, and flags.
            return Compiler.CompileFromFile(shaderFilePath, entryPoint, profile, shaderFlags);
        }
    }
}
