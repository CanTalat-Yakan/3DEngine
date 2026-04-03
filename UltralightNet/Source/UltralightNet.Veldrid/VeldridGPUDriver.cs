using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using UltralightNet.Enums;
using UltralightNet.GPUCommon;
using UltralightNet.Platform;
using UltralightNet.Structs;
using Veldrid;
using Veldrid.SPIRV;

namespace UltralightNet.Veldrid;

public unsafe class VeldridGpuDriver : IGpuDriver
{
	/// <summary>
	///     used for mipmaps, etc
	/// </summary>
	private readonly CommandList _commandList;

	private readonly ResourceSet emptyResourceSet;

	private readonly Texture emptyTexture;
	private readonly Dictionary<uint, GeometryEntry> GeometryEntries = new();
	private readonly GraphicsDevice graphicsDevice;

	private readonly bool IsDirectX = false;
	private readonly bool IsVulkan;
	private readonly Dictionary<uint, RenderBufferEntry> RenderBufferEntries = new();

	private readonly Sampler sampler;
	private readonly ResourceLayout samplerResourceLayout;
	private readonly ResourceSet samplerResourceSet;

	/// <summary>
	///     public only for <see cref="GetRenderTarget(View)" /> inlining
	/// </summary>
	public readonly Dictionary<uint, TextureEntry> TextureEntries = new();

	private readonly ResourceLayout textureResourceLayout;

	public CommandList CommandList;
	private Uniforms[]? FakeMappedUniformBuffer;
	private Framebuffer pipelineOutputFramebuffer;

	// todo: https://github.com/ultralight-ux/AppCore/blob/6324e85f31f815b1519b495f559f1f72717b2651/src/linux/gl/GPUDriverGL.cpp#L407

	private Texture pipelineOutputTexture;

	public TextureSampleCount SampleCount = TextureSampleCount.Count1;

	public float time = 1f;
	private Pipeline ul_scissor;

	private Pipeline ul_scissor_blend;
	private Pipeline ulPath_scissor;

	private Pipeline ulPath_scissor_blend;
	private Shader[] ultralightPathShaders;

	private Shader[] ultralightShaders;

	private DeviceBuffer uniformBuffer;
	private uint UniformBufferCommandLength;

	private ResourceSet uniformResourceSet;

	private ResourceLayout uniformsResourceLayout;

	public VeldridGpuDriver(GraphicsDevice graphicsDevice)
	{
		this.graphicsDevice = graphicsDevice;

		textureResourceLayout = graphicsDevice.ResourceFactory.CreateResourceLayout(
			new ResourceLayoutDescription(
				new ResourceLayoutElementDescription("texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)
			)
		);
		samplerResourceLayout = graphicsDevice.ResourceFactory.CreateResourceLayout(
			new ResourceLayoutDescription(
				new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment))
		);

		_commandList = graphicsDevice.ResourceFactory.CreateCommandList();

		IsVulkan = graphicsDevice.BackendType is GraphicsBackend.Vulkan;
		//IsVulkan = false;

		InitializeBuffers();
		InitShaders();
		InitFramebuffers();
		InitPipelines();

		emptyTexture = graphicsDevice.ResourceFactory.CreateTexture(
			new TextureDescription(
				2,
				2,
				1,
				1,
				1,
				PixelFormat.R8_UNorm,
				TextureUsage.Sampled,
				TextureType.Texture2D
			)
		);
		emptyResourceSet = graphicsDevice.ResourceFactory.CreateResourceSet(
			new ResourceSetDescription(
				textureResourceLayout,
				emptyTexture
			)
		);

		TextureEntries.Add(0, new TextureEntry { texture = emptyTexture, resourceSet = emptyResourceSet });

		{
			sampler = graphicsDevice.ResourceFactory.CreateSampler(new SamplerDescription(
				SamplerAddressMode.Clamp,
				SamplerAddressMode.Clamp,
				SamplerAddressMode.Clamp,
				SamplerFilter.MinLinear_MagLinear_MipLinear,
				ComparisonKind.Never,
				1,
				0,
				1,
				0,
				SamplerBorderColor.TransparentBlack
			));
			samplerResourceSet =
				graphicsDevice.ResourceFactory.CreateResourceSet(
					new ResourceSetDescription(samplerResourceLayout, sampler));
		}
	}

	[SkipLocalsInit]
	public void UpdateCommandList(UlCommandList list)
	{
		if (list.Size is 0) return;

		uint commandId = 0;

		if (IsVulkan)
		{
			if (uniformBuffer is null || uniformResourceSet is null || UniformBufferCommandLength < list.Size)
			{
				if (uniformBuffer is not null) uniformBuffer.Dispose();
				uniformBuffer = graphicsDevice.ResourceFactory.CreateBuffer(
					new BufferDescription(768 * list.Size, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
				UniformBufferCommandLength = list.Size;

				if (uniformResourceSet is not null) uniformResourceSet.Dispose();
				uniformResourceSet =
					graphicsDevice.ResourceFactory.CreateResourceSet(
						new ResourceSetDescription(uniformsResourceLayout, uniformBuffer));

				FakeMappedUniformBuffer = GC.AllocateUninitializedArray<Uniforms>((int)list.Size);
			}

			Span<Uniforms> uniformSpan = FakeMappedUniformBuffer; // implicit conversion :)

			foreach (var command in list.AsSpan())
				if (command.CommandType is CommandType.DrawGeometry)
				{
					var state = command.GpuState;
					Unsafe.SkipInit(out Uniforms uniforms);
					uniforms.State.X = state.ViewportWidth;
					uniforms.State.Y = state.ViewportHeight;
					uniforms.Transform =
						state.Transform.ApplyProjection(state.ViewportWidth, state.ViewportHeight, true);
					state.Scalar.CopyTo(new Span<float>(&uniforms.Scalar4_0.W, 8));
					state.Vector.CopyTo(new Span<Vector4>(&uniforms.Vector_0.W, 8));
					state.Clip.CopyTo(new Span<Matrix4x4>(&uniforms.Clip_0.M11, 8));
					uniforms.ClipSize = state.ClipSize;
					uniformSpan[(int)commandId++] = uniforms;
				}

			CommandList.UpdateBuffer(uniformBuffer, 0, (ReadOnlySpan<Uniforms>)uniformSpan);

			commandId = 0;
		}

		foreach (var command in list.AsSpan())
		{
			var renderBufferEntry = RenderBufferEntries[command.GpuState.RenderBufferId];

			CommandList.SetFramebuffer(renderBufferEntry.framebuffer);

			if (command.CommandType is CommandType.ClearRenderBuffer)
			{
				CommandList.SetFullScissorRect(0);
				CommandList.ClearColorTarget(0, RgbaFloat.Clear);
			}
			else if (command.CommandType is CommandType.DrawGeometry)
			{
				var state = command.GpuState;
				if (state.ShaderType is ShaderType.Fill)
				{
					if (state.EnableScissor)
					{
						if (state.EnableBlend)
							CommandList.SetPipeline(ul_scissor_blend);
						else
							CommandList.SetPipeline(ul_scissor);
						CommandList.SetScissorRect(0, (uint)state.ScissorRect.Left, (uint)state.ScissorRect.Top,
							(uint)(state.ScissorRect.Right - state.ScissorRect.Left),
							(uint)(state.ScissorRect.Bottom - state.ScissorRect.Top));
					}
					else
					{
						if (state.EnableBlend)
							CommandList.SetPipeline(ul_scissor_blend);
						else
							CommandList.SetPipeline(ul_scissor);
						CommandList.SetFullScissorRect(0);
					}

					CommandList.SetGraphicsResourceSet(1, TextureEntries[state.Texture1Id].resourceSet);
					CommandList.SetGraphicsResourceSet(2, TextureEntries[state.Texture2Id].resourceSet);
					CommandList.SetGraphicsResourceSet(3, samplerResourceSet);
				}
				else
				{
					if (state.EnableScissor)
					{
						if (state.EnableBlend)
							CommandList.SetPipeline(ulPath_scissor_blend);
						else
							CommandList.SetPipeline(ulPath_scissor);
						CommandList.SetScissorRect(0, (uint)state.ScissorRect.Left, (uint)state.ScissorRect.Top,
							(uint)(state.ScissorRect.Right - state.ScissorRect.Left),
							(uint)(state.ScissorRect.Bottom - state.ScissorRect.Top));
					}
					else
					{
						if (state.EnableBlend)
							CommandList.SetPipeline(ulPath_scissor_blend);
						else
							CommandList.SetPipeline(ulPath_scissor);
						CommandList.SetFullScissorRect(0);
					}
				}

				#region Uniforms

				if (IsVulkan)
				{
					var offset = 768 * commandId++;
					CommandList.SetGraphicsResourceSet(0, uniformResourceSet, 1,
						ref offset); // dynamic offset my beloved
				}
				else
				{
					Unsafe.SkipInit(out Uniforms uniforms);
					uniforms.State.X = state.ViewportWidth;
					uniforms.State.Y = state.ViewportHeight;
					uniforms.Transform =
						state.Transform.ApplyProjection(state.ViewportWidth, state.ViewportHeight, true);
					state.Scalar.CopyTo(new Span<float>(&uniforms.Scalar4_0.W, 8));
					state.Vector.CopyTo(new Span<Vector4>(&uniforms.Vector_0.W, 8));
					state.Clip.CopyTo(new Span<Matrix4x4>(&uniforms.Clip_0.M11, 8));
					uniforms.ClipSize = state.ClipSize;
					CommandList.UpdateBuffer(uniformBuffer, 0, ref uniforms);

					CommandList.SetGraphicsResourceSet(0, uniformResourceSet);
				}

				#endregion Uniforms

				CommandList.SetViewport(0, new Viewport(0f, 0f, state.ViewportWidth, state.ViewportHeight, 0f, 1f));

				var geometryEntry = GeometryEntries[command.GeometryId];

				CommandList.SetVertexBuffer(0, geometryEntry.vertices);
				CommandList.SetIndexBuffer(geometryEntry.indicies, IndexFormat.UInt32);

				CommandList.DrawIndexed(
					command.IndicesCount,
					1,
					command.IndicesOffset,
					0,
					0
				);
			}
		}
	}

	/// <remarks>will throw exception when view doesn't have RenderTarget</remarks>
	/// <exception cref="KeyNotFoundException">When called on view without RenderTarget</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[SkipLocalsInit]
	public ResourceSet GetRenderTarget(View view)
	{
		return TextureEntries[view.RenderTarget.TextureId].resourceSet;
	}

	private void InitializeBuffers()
	{
		if (!IsVulkan)
			uniformBuffer =
				graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(768, BufferUsage.UniformBuffer));
	}

	private void InitShaders()
	{
		var a = typeof(VeldridGpuDriver).Assembly;
		var fillVert = a.GetManifestResourceStream("UltralightNet.Veldrid.shader_fill.vert.spv");
		var fillVertBytes = new byte[fillVert.Length];
		fillVert.Read(fillVertBytes, 0, (int)fillVert.Length);
		var fillFrag = a.GetManifestResourceStream("UltralightNet.Veldrid.shader_fill.frag.spv");
		var fillFragBytes = new byte[fillFrag.Length];
		fillFrag.Read(fillFragBytes, 0, (int)fillFrag.Length);

		ultralightShaders = graphicsDevice.ResourceFactory.CreateFromSpirv(
			new ShaderDescription(ShaderStages.Vertex, fillVertBytes, "main"),
			new ShaderDescription(ShaderStages.Fragment, fillFragBytes, "main"));

		var pathVert = a.GetManifestResourceStream("UltralightNet.Veldrid.shader_fill_path.vert.spv");
		var pathVertBytes = new byte[pathVert.Length];
		pathVert.Read(pathVertBytes, 0, (int)pathVert.Length);
		var pathFrag = a.GetManifestResourceStream("UltralightNet.Veldrid.shader_fill_path.frag.spv");
		var pathFragBytes = new byte[pathFrag.Length];
		pathFrag.Read(pathFragBytes, 0, (int)pathFrag.Length);

		ultralightPathShaders = graphicsDevice.ResourceFactory.CreateFromSpirv(
			new ShaderDescription(ShaderStages.Vertex, pathVertBytes, "main"),
			new ShaderDescription(ShaderStages.Fragment, pathFragBytes, "main"));
	}

	private VertexElementSemantic HLSL_to_any(VertexElementSemantic hlsl_semantic)
	{
		return IsDirectX ? hlsl_semantic : VertexElementSemantic.TextureCoordinate;
	}

	private ShaderSetDescription FillShaderSetDescription()
	{
		return new ShaderSetDescription(
			[
				new VertexLayoutDescription(
					140,
					new VertexElementDescription(
						"in_Position",
						HLSL_to_any(VertexElementSemantic.Position),
						VertexElementFormat.Float2
					),
					new VertexElementDescription(
						"in_Color",
						HLSL_to_any(VertexElementSemantic.Color),
						VertexElementFormat.Byte4_Norm
					),
					new VertexElementDescription(
						"in_TexCoord",
						VertexElementSemantic.TextureCoordinate,
						VertexElementFormat.Float2
					),
					new VertexElementDescription(
						"in_ObjCoord",
						VertexElementSemantic.TextureCoordinate,
						VertexElementFormat.Float2
					),
					new VertexElementDescription(
						"in_Data0",
						HLSL_to_any(VertexElementSemantic.Color),
						VertexElementFormat.Float4
					),
					new VertexElementDescription(
						"in_Data1",
						HLSL_to_any(VertexElementSemantic.Color),
						VertexElementFormat.Float4
					),
					new VertexElementDescription(
						"in_Data2",
						HLSL_to_any(VertexElementSemantic.Color),
						VertexElementFormat.Float4
					),
					new VertexElementDescription(
						"in_Data3",
						HLSL_to_any(VertexElementSemantic.Color),
						VertexElementFormat.Float4
					),
					new VertexElementDescription(
						"in_Data4",
						HLSL_to_any(VertexElementSemantic.Color),
						VertexElementFormat.Float4
					),
					new VertexElementDescription(
						"in_Data5",
						HLSL_to_any(VertexElementSemantic.Color),
						VertexElementFormat.Float4
					),
					new VertexElementDescription(
						"in_Data6",
						HLSL_to_any(VertexElementSemantic.Color),
						VertexElementFormat.Float4
					)
				)
			], ultralightShaders
		);
	}

	private ShaderSetDescription FillPathShaderSetDescription()
	{
		return new ShaderSetDescription(
			[
				new VertexLayoutDescription(
					20,
					new VertexElementDescription(
						"in_Position",
						HLSL_to_any(VertexElementSemantic.Position),
						VertexElementFormat.Float2
					),
					new VertexElementDescription(
						"in_Color",
						HLSL_to_any(VertexElementSemantic.Color),
						VertexElementFormat.Byte4_Norm
					),
					new VertexElementDescription(
						"in_TexCoord",
						VertexElementSemantic.TextureCoordinate,
						VertexElementFormat.Float2
					)
				)
			], ultralightPathShaders
		);
	}

	private void InitFramebuffers()
	{
		pipelineOutputTexture = graphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(
			512,
			512,
			1,
			1,
			1,
			PixelFormat.B8_G8_R8_A8_UNorm,
			TextureUsage.RenderTarget,
			TextureType.Texture2D,
			SampleCount));

		pipelineOutputFramebuffer = graphicsDevice.ResourceFactory.CreateFramebuffer(
			new FramebufferDescription
			{
				ColorTargets =
				[
					new FramebufferAttachmentDescription(pipelineOutputTexture, 0)
				]
			}
		);
	}

	private static void DisableBlend(ref GraphicsPipelineDescription pipa)
	{
		pipa.BlendState.AttachmentStates =
		[
			new BlendAttachmentDescription
			{
				SourceColorFactor = BlendFactor.One,
				SourceAlphaFactor = BlendFactor.One,
				DestinationColorFactor = BlendFactor.InverseSourceAlpha,
				DestinationAlphaFactor = BlendFactor.InverseSourceAlpha,

				BlendEnabled = false
			}
		];
	}

	private void InitPipelines()
	{
		var fillShaderSetDescription = FillShaderSetDescription();

		uniformsResourceLayout = graphicsDevice.ResourceFactory.CreateResourceLayout(
			new ResourceLayoutDescription(
				new ResourceLayoutElementDescription(
					"Uniforms",
					ResourceKind.UniformBuffer,
					ShaderStages.Vertex | ShaderStages.Fragment,
					IsVulkan ? ResourceLayoutElementOptions.DynamicBinding : ResourceLayoutElementOptions.None
				)
			)
		);
		if (!IsVulkan)
			uniformResourceSet =
				graphicsDevice.ResourceFactory.CreateResourceSet(
					new ResourceSetDescription(uniformsResourceLayout, uniformBuffer));

		GraphicsPipelineDescription _ultralightPipelineDescription = new()
		{
			BlendState = new BlendStateDescription
			{
				AttachmentStates =
				[
					// glBlendFunc(GL_ONE, GL_ONE_MINUS_SRC_ALPHA);
					new BlendAttachmentDescription
					{
						/*SourceColorFactor = BlendFactor.One,
						SourceAlphaFactor = BlendFactor.InverseDestinationAlpha,
						DestinationColorFactor = BlendFactor.InverseSourceAlpha,
						DestinationAlphaFactor = BlendFactor.One,*/
						SourceColorFactor = BlendFactor.One,
						SourceAlphaFactor = BlendFactor.One,
						DestinationColorFactor = BlendFactor.InverseSourceAlpha,
						DestinationAlphaFactor = BlendFactor.InverseSourceAlpha,
						BlendEnabled = true,
						ColorFunction = BlendFunction.Add,
						AlphaFunction = BlendFunction.Add
					}
				],
				AlphaToCoverageEnabled = false
			},
			DepthStencilState = new DepthStencilStateDescription
			{
				DepthTestEnabled = false, // glDisable(GL_DEPTH_TEST)
				DepthWriteEnabled = false,
				StencilTestEnabled = false,
				DepthComparison = ComparisonKind.Never // glDepthFunc(GL_NEVER)
			},
			RasterizerState = new RasterizerStateDescription
			{
				CullMode = FaceCullMode.None,
				FrontFace = FrontFace.CounterClockwise,
				FillMode = PolygonFillMode.Solid,
				ScissorTestEnabled = true,
				DepthClipEnabled = false
			},
			PrimitiveTopology = PrimitiveTopology.TriangleList,
			ResourceBindingModel = ResourceBindingModel.Default,
			ShaderSet = fillShaderSetDescription,
			ResourceLayouts =
			[
				uniformsResourceLayout,
				textureResourceLayout,
				textureResourceLayout,
				samplerResourceLayout
			],
			Outputs = pipelineOutputFramebuffer.OutputDescription
		};

		var ultralight_pd__SCISSOR_TRUE__ENALBE_BLEND = _ultralightPipelineDescription;

		var ultralight_pd__SCISSOR_TRUE__DISALBE_BLEND = _ultralightPipelineDescription;
		DisableBlend(ref ultralight_pd__SCISSOR_TRUE__DISALBE_BLEND);

		ul_scissor_blend =
			graphicsDevice.ResourceFactory.CreateGraphicsPipeline(ref ultralight_pd__SCISSOR_TRUE__ENALBE_BLEND);
		ul_scissor =
			graphicsDevice.ResourceFactory.CreateGraphicsPipeline(ref ultralight_pd__SCISSOR_TRUE__DISALBE_BLEND);


		var _ultralightPathPipelineDescription = _ultralightPipelineDescription;
		_ultralightPathPipelineDescription.ShaderSet = FillPathShaderSetDescription();
		_ultralightPathPipelineDescription.ResourceLayouts = [uniformsResourceLayout];

		var ultralightPath_pd__SCISSOR_TRUE__ENALBE_BLEND = _ultralightPathPipelineDescription;

		var ultralightPath_pd__SCISSOR_TRUE__DISABLE_BLEND = _ultralightPathPipelineDescription;
		DisableBlend(ref ultralightPath_pd__SCISSOR_TRUE__DISABLE_BLEND);

		ulPath_scissor_blend =
			graphicsDevice.ResourceFactory.CreateGraphicsPipeline(ref ultralightPath_pd__SCISSOR_TRUE__ENALBE_BLEND);
		ulPath_scissor =
			graphicsDevice.ResourceFactory.CreateGraphicsPipeline(ref ultralightPath_pd__SCISSOR_TRUE__DISABLE_BLEND);
	}

	public class TextureEntry
	{
		public ResourceSet resourceSet;
		public Texture texture;
		public byte[]? unstride;
	}

	private class GeometryEntry
	{
		public DeviceBuffer indicies;
		public DeviceBuffer vertices;
	}

	private class RenderBufferEntry
	{
		public Framebuffer framebuffer;
		public TextureEntry textureEntry;
	}

	#region NextId

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[SkipLocalsInit]
	private static uint GetKey<TValue>(Dictionary<uint, TValue> dictionary)
	{
		for (uint i = 1;; i++)
			if (!dictionary.ContainsKey(i))
				return i;
	}

	[SkipLocalsInit]
	public uint NextTextureId()
	{
		uint id = GetKey(TextureEntries);
		TextureEntries.Add(id, new TextureEntry());
		#if DEBUG
			Console.WriteLine($"NextTextureId() = {id}");
		#endif
		return id;
	}

	[SkipLocalsInit]
	public uint NextGeometryId()
	{
		uint id = GetKey(GeometryEntries);
		GeometryEntries.Add(id, new GeometryEntry());
		#if DEBUG
			Console.WriteLine($"NextGeometryId() = {id}");
		#endif
		return id;
	}

	[SkipLocalsInit]
	public uint NextRenderBufferId()
	{
		uint id = GetKey(RenderBufferEntries);
		RenderBufferEntries.Add(id, new RenderBufferEntry());
		#if DEBUG
			Console.WriteLine($"NextRenderBufferId() = {id}");
		#endif
		return id;
	}

	#endregion NextId

	#region Texture

	private void UploadTexture(TextureEntry texture, UlBitmap bitmap, uint width, uint height, uint bpp, uint rowBytes)
	{
		byte* pixelsPTR = bitmap.LockPixels();
		var pixels = new ReadOnlySpan<byte>(pixelsPTR, (int)(rowBytes * height));

		if (rowBytes == width * bpp)
		{
			graphicsDevice.UpdateTexture(texture.texture, (IntPtr)pixelsPTR, width * height * bpp, 0, 0, 0, width,
				height, 1, 0, 0);
		}
		else
		{
			Span<byte> unstridedPixels = texture.unstride;
			var widthXbpp = (int)(width * bpp);
			for (uint y = 0; y < height; y++)
			{
				var row = pixels.Slice((int)(rowBytes * y), widthXbpp);
				row.CopyTo(unstridedPixels.Slice(widthXbpp * (int)y, widthXbpp));
			}

			graphicsDevice.UpdateTexture(texture.texture, unstridedPixels, 0, 0, 0, width, height, 1, 0, 0);
		}

		bitmap.UnlockPixels();
	}

	[SkipLocalsInit]
	public void CreateTexture(uint texture_id, UlBitmap bitmap)
	{
		#if DEBUG
			Console.WriteLine($"CreateTexture({texture_id})");
		#endif
		bool isRT = bitmap.IsEmpty;
		var entry = TextureEntries[texture_id];

		uint width = bitmap.Width;
		uint height = bitmap.Height;
		uint bpp = bitmap.Bpp;

		TextureDescription textureDescription = new()
		{
			Type = TextureType.Texture2D,
			Usage = TextureUsage.Sampled,
			Width = width,
			Height = height,
			MipLevels = 1,
			SampleCount = isRT ? SampleCount : TextureSampleCount.Count1,
			ArrayLayers = 1,
			Depth = 1
		};

		textureDescription.Format = bpp is 1 ? PixelFormat.R8_UNorm : PixelFormat.B8_G8_R8_A8_UNorm;

		if (isRT) textureDescription.Usage |= TextureUsage.RenderTarget;

		entry.texture = graphicsDevice.ResourceFactory.CreateTexture(textureDescription);

		if (!isRT)
		{
			uint rowBytes = bitmap.RowBytes;
			if (bpp * width != rowBytes) entry.unstride = ArrayPool<byte>.Shared.Rent((int)(width * height * bpp));
			UploadTexture(entry, bitmap, width, height, bpp, rowBytes);
		}

		entry.resourceSet = graphicsDevice.ResourceFactory.CreateResourceSet(
			new ResourceSetDescription(
				textureResourceLayout,
				entry.texture
			)
		);
	}

	[SkipLocalsInit]
	public void UpdateTexture(uint texture_id, UlBitmap bitmap)
	{
		var entry = TextureEntries[texture_id];

		uint height = bitmap.Height;
		uint width = bitmap.Width;
		uint bpp = bitmap.Bpp;

		UploadTexture(entry, bitmap, width, height, bpp, bitmap.RowBytes);
	}

	[SkipLocalsInit]
	public void DestroyTexture(uint texture_id)
	{
		#if DEBUG
			Console.WriteLine($"DestroyTexture({texture_id})");
		#endif
		TextureEntries.Remove(texture_id, out var entry);
		entry.resourceSet.Dispose();
		entry.resourceSet = null;
		entry.texture.Dispose();
		entry.texture = null;
		if (entry.unstride is not null) ArrayPool<byte>.Shared.Return(entry.unstride);
	}

	#endregion Texture

	#region Geometry

	[SkipLocalsInit]
	public void CreateGeometry(uint geometry_id, UlVertexBuffer vertices, UlIndexBuffer indices)
	{
		#if DEBUG
			Console.WriteLine($"CreateGeometry({geometry_id})");
		#endif
		var entry = GeometryEntries[geometry_id];

		BufferDescription vertexDescription = new(vertices.Size, BufferUsage.VertexBuffer);
		entry.vertices = graphicsDevice.ResourceFactory.CreateBuffer(ref vertexDescription);
		BufferDescription indexDescription = new(indices.Size, BufferUsage.IndexBuffer);
		entry.indicies = graphicsDevice.ResourceFactory.CreateBuffer(ref indexDescription);

		graphicsDevice.UpdateBuffer(entry.vertices, 0, (IntPtr)vertices.Data, vertices.Size);
		graphicsDevice.UpdateBuffer(entry.indicies, 0, (IntPtr)indices.Data, indices.Size);
	}

	[SkipLocalsInit]
	public void UpdateGeometry(uint geometry_id, UlVertexBuffer vertices, UlIndexBuffer indices)
	{
		//Console.WriteLine($"UpdateGeometry({geometry_id})");
		var entry = GeometryEntries[geometry_id];

		graphicsDevice.UpdateBuffer(entry.vertices, 0, (IntPtr)vertices.Data, vertices.Size);
		graphicsDevice.UpdateBuffer(entry.indicies, 0, (IntPtr)indices.Data, indices.Size);
	}

	[SkipLocalsInit]
	public void DestroyGeometry(uint geometry_id)
	{
		#if DEBUG
			Console.WriteLine($"DestroyGeometry({geometry_id})");
		#endif
		GeometryEntries.Remove(geometry_id, out var entry);

		entry.vertices.Dispose();
		entry.indicies.Dispose();
	}

	#endregion

	#region RenderBuffer

	[SkipLocalsInit]
	public void CreateRenderBuffer(uint render_buffer_id, UlRenderBuffer buffer)
	{
		#if DEBUG
			Console.WriteLine($"CreateRenderBuffer({render_buffer_id})");
		#endif
		var entry = RenderBufferEntries[render_buffer_id];
		var textureEntry = TextureEntries[buffer.TextureId];

		entry.textureEntry = textureEntry;

		FramebufferDescription fd = new()
		{
			ColorTargets =
			[
				new FramebufferAttachmentDescription(textureEntry.texture, 0)
			]
		};

		entry.framebuffer = graphicsDevice.ResourceFactory.CreateFramebuffer(ref fd);
	}

	[SkipLocalsInit]
	public void DestroyRenderBuffer(uint render_buffer_id)
	{
		#if DEBUG
			Console.WriteLine($"DestroyRenderBuffer({render_buffer_id})");
		#endif
		RenderBufferEntries.Remove(render_buffer_id, out var entry);
		entry.textureEntry = null;
		entry.framebuffer.Dispose();
		entry.framebuffer = null;
	}

	#endregion RenderBuffer
}
