using Assimp;
using ImGuiNET;
using System;
using System.Numerics;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Direct3D11;
using Engine.Utilities;

namespace Engine.Editor
{
    internal class ImGui
    {
        Renderer m_d3d;
        InputLayoutDescription m_inputLayoutDescription;
        ID3D11Texture2D m_fontTexture;
        Mesh m_imguiMesh;

        internal ImGui()
        {
            m_d3d = Renderer.Instance;
            var con = ImGuiNET.ImGui.CreateContext();
            ImGuiNET.ImGui.SetCurrentContext(con);
            var fonts = ImGuiNET.ImGui.GetIO().Fonts;
            ImGuiNET.ImGui.GetIO().Fonts.AddFontDefault();
            var io = ImGuiNET.ImGui.GetIO();
            io.DisplaySize = m_d3d.m_SwapChainPanel.ActualSize;
            io.DisplayFramebufferScale = Vector2.One;
            io.DeltaTime = (float)Time.m_Watch.Elapsed.TotalSeconds;
            ImGuiNET.ImGui.StyleColorsDark();
            RecreateFontDeviceTexture();
        }

        static void RecreateFontDeviceTexture()
        {
            ImGuiIOPtr io = ImGuiNET.ImGui.GetIO();
            IntPtr pixels;
            int width, height, bytesPerPixel;
            io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height, out bytesPerPixel);
            io.Fonts.ClearTexData();
        }

        internal void Draw()
        {
            ImGuiNET.ImGui.GetIO().DeltaTime = (float)Time.m_Watch.Elapsed.TotalSeconds;
            ImGuiNET.ImGui.NewFrame();
            ImGuiNET.ImGui.Begin("Test");

            ImGuiLayout();

            ImGuiNET.ImGui.End();
            ImGuiNET.ImGui.Render();
            unsafe { RenderDrawData(ImGuiNET.ImGui.GetDrawData()); }
        }

        float f = 0.0f;
        bool show_test_window = false;
        bool show_another_window = false;
        Vector3 clear_color = new Vector3(114f / 255f, 144f / 255f, 154f / 255f);
        byte[] _textBuffer = new byte[100];
        void ImGuiLayout()
        {
            {
                ImGuiNET.ImGui.Text("Hello, world!");
                ImGuiNET.ImGui.SliderFloat("float", ref f, 0.0f, 1.0f, string.Empty);
                ImGuiNET.ImGui.ColorEdit3("clear color", ref clear_color);
                if (ImGuiNET.ImGui.Button("Test Window")) show_test_window = !show_test_window;
                if (ImGuiNET.ImGui.Button("Another Window")) show_another_window = !show_another_window;
                ImGuiNET.ImGui.Text(string.Format("Application average {0:F3} ms/frame ({1:F1} FPS)", 1000f / ImGuiNET.ImGui.GetIO().Framerate, ImGuiNET.ImGui.GetIO().Framerate));

                ImGuiNET.ImGui.InputText("Text input", _textBuffer, 100);
            }

            // 2. Show another simple window, this time using an explicit Begin/End pair
            if (show_another_window)
            {
                ImGuiNET.ImGui.SetNextWindowSize(new Vector2(200, 100), ImGuiCond.FirstUseEver);
                ImGuiNET.ImGui.Begin("Another Window", ref show_another_window);
                ImGuiNET.ImGui.Text("Hello");
                ImGuiNET.ImGui.End();
            }

            // 3. Show the ImGui test window. Most of the sample code is in ImGui.ShowTestWindow()
            if (show_test_window)
            {
                ImGuiNET.ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
                ImGuiNET.ImGui.ShowDemoWindow(ref show_test_window);
            }
        }

        void RenderDrawData(ImDrawDataPtr drawData)
        {
            // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
            drawData.ScaleClipRects(ImGuiNET.ImGui.GetIO().DisplayFramebufferScale);

            UpdateBuffers(drawData);
            RenderCommandLists(drawData);
        }

        private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
        {
            //            if (drawData.TotalVtxCount == 0)
            //                return;

            //            // Expand buffers if we need more room
            //            if (drawData.TotalVtxCount > _vertexBufferSize)
            //            {
            //                _vertexBuffer?.Dispose();

            //                _vertexBufferSize = (int)(drawData.TotalVtxCount * 1.5f);
            //                _vertexBuffer = new VertexBuffer(_graphicsDevice, DrawVertDeclaration.Declaration, _vertexBufferSize, BufferUsage.None);
            //                _vertexData = new byte[_vertexBufferSize * DrawVertDeclaration.Size];
            //            }

            //            if (drawData.TotalIdxCount > _indexBufferSize)
            //            {
            //                _indexBuffer?.Dispose();

            //                _indexBufferSize = (int)(drawData.TotalIdxCount * 1.5f);
            //                _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, _indexBufferSize, BufferUsage.None);
            //                _indexData = new byte[_indexBufferSize * sizeof(ushort)];
            //            }

            //            // Copy ImGui's vertices and indices to a set of managed byte arrays
            //            int vtxOffset = 0;
            //            int idxOffset = 0;

            //            for (int n = 0; n < drawData.CmdListsCount; n++)
            //            {
            //                ImDrawListPtr cmdList = drawData.CmdListsRange[n];

            //                fixed (void* vtxDstPtr = &_vertexData[vtxOffset * DrawVertDeclaration.Size])
            //                fixed (void* idxDstPtr = &_indexData[idxOffset * sizeof(ushort)])
            //                {
            //                    Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vtxDstPtr, _vertexData.Length, cmdList.VtxBuffer.Size * DrawVertDeclaration.Size);
            //                    Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, idxDstPtr, _indexData.Length, cmdList.IdxBuffer.Size * sizeof(ushort));
            //                }

            //                vtxOffset += cmdList.VtxBuffer.Size;
            //                idxOffset += cmdList.IdxBuffer.Size;
            //            }

            //            // Copy the managed byte arrays to the gpu vertex- and index buffers
            //            _vertexBuffer.SetData(_vertexData, 0, drawData.TotalVtxCount * DrawVertDeclaration.Size);
            //            _indexBuffer.SetData(_indexData, 0, drawData.TotalIdxCount * sizeof(ushort));
        }

        private unsafe void RenderCommandLists(ImDrawDataPtr drawData)
        {
            //            _graphicsDevice.SetVertexBuffer(_vertexBuffer);
            //            _graphicsDevice.Indices = _indexBuffer;

            //            int vtxOffset = 0;
            //            int idxOffset = 0;

            //            for (int n = 0; n < drawData.CmdListsCount; n++)
            //            {
            //                ImDrawListPtr cmdList = drawData.CmdListsRange[n];

            //                for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
            //                {
            //                    ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];

            //                    if (!_loadedTextures.ContainsKey(drawCmd.TextureId))
            //                    {
            //                        throw new InvalidOperationException($"Could not find a texture with id '{drawCmd.TextureId}', please check your bindings");
            //                    }

            //                    _graphicsDevice.ScissorRectangle = new Rectangle(
            //                        (int)drawCmd.ClipRect.X,
            //                        (int)drawCmd.ClipRect.Y,
            //                        (int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
            //                        (int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
            //                    );

            //                    var effect = UpdateEffect(_loadedTextures[drawCmd.TextureId]);

            //                    foreach (var pass in effect.CurrentTechnique.Passes)
            //                    {
            //                        pass.Apply();

            //#pragma warning disable CS0618 // // FNA does not expose an alternative method.
            //                        _graphicsDevice.DrawIndexedPrimitives(
            //                            primitiveType: PrimitiveType.TriangleList,
            //                            baseVertex: vtxOffset,
            //                            minVertexIndex: 0,
            //                            numVertices: cmdList.VtxBuffer.Size,
            //                            startIndex: idxOffset,
            //                            primitiveCount: (int)drawCmd.ElemCount / 3
            //                        );
            //#pragma warning restore CS0618
            //                    }

            //                    idxOffset += (int)drawCmd.ElemCount;
            //                }

            //                vtxOffset += cmdList.VtxBuffer.Size;
            //                //            }
            //            }
        }


        /*
        unsafe public class ImGuiRender
        {
            public CommonContext context;
            public InputLayoutDescription inputLayoutDescription;
            public Texture2D fontTexture;
            public Mesh imguiMesh;

            PSODesc psoDesc = new PSODesc
            {
                CullMode = CullMode.None,
                RenderTargetFormat = Format.R8G8B8A8_UNorm,
                RenderTargetCount = 1,
                PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                InputLayout = "ImGui",
                BlendState = "Alpha",
            };

            public void Init()
            {
                context.imguiContext = ImGui.CreateContext();
                ImGui.SetCurrentContext(context.imguiContext);
                var io = ImGui.GetIO();
                io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
                fontTexture = new Texture2D();
                context.renderTargets["imgui_font"] = fontTexture;

                //ImFontPtr font = io.Fonts.AddFontFromFileTTF("c:\\Windows\\Fonts\\SIMHEI.ttf", 14, null, io.Fonts.GetGlyphRangesChineseFull());

                io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);
                io.Fonts.TexID = context.GetStringId("imgui_font");

                fontTexture.width = width;
                fontTexture.height = height;
                fontTexture.mipLevels = 1;
                fontTexture.format = Format.R8G8B8A8_UNorm;
                imguiMesh = context.GetMesh("imgui_mesh");

                GPUUpload gpuUpload = new GPUUpload();
                gpuUpload.texture2D = fontTexture;
                gpuUpload.format = Format.R8G8B8A8_UNorm;
                gpuUpload.textureData = new byte[width * height * bytesPerPixel];
                new Span<byte>(pixels, gpuUpload.textureData.Length).CopyTo(gpuUpload.textureData);

                context.uploadQueue.Enqueue(gpuUpload);

            }
            public void Render()
            {
                ImGui.NewFrame();
                ImGui.ShowDemoWindow();
                ImGui.Render();
                var data = ImGui.GetDrawData();
                GraphicsContext graphicsContext = context.graphicsContext;
                float L = data.DisplayPos.X;
                float R = data.DisplayPos.X + data.DisplaySize.X;
                float T = data.DisplayPos.Y;
                float B = data.DisplayPos.Y + data.DisplaySize.Y;
                float[] mvp =
                {
                        2.0f/(R-L),   0.0f,           0.0f,       0.0f,
                        0.0f,         2.0f/(T-B),     0.0f,       0.0f,
                        0.0f,         0.0f,           0.5f,       0.0f,
                        (R+L)/(L-R),  (T+B)/(B-T),    0.5f,       1.0f,
                };
                int index1 = context.uploadBuffer.Upload<float>(mvp);
                graphicsContext.SetRootSignature(Pipeline12Util.FromString(context, "Cssss"));
                graphicsContext.SetPipelineState(context.pipelineStateObjects["ImGui"], psoDesc);
                context.uploadBuffer.SetCBV(graphicsContext, index1, 0);
                graphicsContext.commandList.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);

                Vector2 clip_off = data.DisplayPos;
                for (int i = 0; i < data.CmdListsCount; i++)
                {
                    var cmdList = data.CmdListsRange[i];
                    var vertBytes = cmdList.VtxBuffer.Size * sizeof(ImDrawVert);
                    var indexBytes = cmdList.IdxBuffer.Size * sizeof(ImDrawIdx);

                    context.uploadBuffer.UploadMeshIndex(graphicsContext, imguiMesh, new Span<byte>(cmdList.IdxBuffer.Data.ToPointer(), indexBytes), Format.R16_UInt);
                    context.uploadBuffer.UploadVertexBuffer(graphicsContext, ref imguiMesh._vertex, new Span<byte>(cmdList.VtxBuffer.Data.ToPointer(), vertBytes));
                    imguiMesh.vertices["POSITION"] = new _VertexBuffer() { offset = 0, resource = imguiMesh._vertex, sizeInByte = vertBytes, stride = sizeof(ImDrawVert) };
                    imguiMesh.vertices["TEXCOORD"] = new _VertexBuffer() { offset = 8, resource = imguiMesh._vertex, sizeInByte = vertBytes, stride = sizeof(ImDrawVert) };
                    imguiMesh.vertices["COLOR"] = new _VertexBuffer() { offset = 16, resource = imguiMesh._vertex, sizeInByte = vertBytes, stride = sizeof(ImDrawVert) };

                    graphicsContext.SetMesh(imguiMesh);

                    for (int j = 0; j < cmdList.CmdBuffer.Size; j++)
                    {
                        var cmd = cmdList.CmdBuffer[j];
                        if (cmd.UserCallback != IntPtr.Zero)
                        {
                            throw new NotImplementedException("user callbacks not implemented");
                        }
                        else
                        {
                            graphicsContext.SetSRV(context.GetTexByStrId(cmd.TextureId), 0);
                            var rect = new Vortice.RawRect((int)(cmd.ClipRect.X - clip_off.X), (int)(cmd.ClipRect.Y - clip_off.Y), (int)(cmd.ClipRect.Z - clip_off.X), (int)(cmd.ClipRect.W - clip_off.Y));
                            graphicsContext.commandList.RSSetScissorRects(new[] { rect });

                            graphicsContext.DrawIndexedInstanced((int)cmd.ElemCount, 1, (int)(cmd.IdxOffset), (int)(cmd.VtxOffset), 0);
                        }
                    }
                }
            }
        }
        */
    }
}
