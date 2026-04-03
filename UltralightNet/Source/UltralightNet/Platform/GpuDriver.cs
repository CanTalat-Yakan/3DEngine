using System.Collections.Generic;
using System.Runtime.InteropServices;
using UltralightNet.Platform.HighPerformance;
using UltralightNet.Structs;

namespace UltralightNet.Platform
{
	namespace HighPerformance
	{
		/// <summary>
		///     <see cref="IGpuDriver" /> native definition.
		/// </summary>
		public unsafe struct GpuDriver
		{
			#if !NETSTANDARD
			public delegate* unmanaged[Cdecl]<void> BeginSynchronize;
			public delegate* unmanaged[Cdecl]<void> EndSynchronize;
			public delegate* unmanaged[Cdecl]<uint> NextTextureId;
			public delegate* unmanaged[Cdecl]<uint, void*, void> CreateTexture;
			public delegate* unmanaged[Cdecl]<uint, void*, void> UpdateTexture;
			public delegate* unmanaged[Cdecl]<uint, void> DestroyTexture;
			public delegate* unmanaged[Cdecl]<uint> NextRenderBufferId;
			public delegate* unmanaged[Cdecl]<uint, UlRenderBuffer, void> CreateRenderBuffer;
			public delegate* unmanaged[Cdecl]<uint, void> DestroyRenderBuffer;
			public delegate* unmanaged[Cdecl]<uint> NextGeometryId;
			public delegate* unmanaged[Cdecl]<uint, UlVertexBuffer, UlIndexBuffer, void> CreateGeometry;
			public delegate* unmanaged[Cdecl]<uint, UlVertexBuffer, UlIndexBuffer, void> UpdateGeometry;
			public delegate* unmanaged[Cdecl]<uint, void> DestroyGeometry;
			public delegate* unmanaged[Cdecl]<UlCommandList, void> UpdateCommandList;
			#else
			public void* BeginSynchronize,
				EndSynchronize,
				NextTextureId,
				CreateTexture,
				UpdateTexture,
				DestroyTexture,
				NextRenderBufferId,
				CreateRenderBuffer,
				DestroyRenderBuffer,
				NextGeometryId,
				CreateGeometry,
				UpdateGeometry,
				DestroyGeometry,
				UpdateCommandList;
			#endif
		}
	}

	/// <summary>
	/// User-defined GPU driver interface.
	/// <br/>
	/// The library uses this to optionally render Views on the GPU (see <see cref="ViewConfig.IsAccelerated"/>).
	/// <br/>
	/// You can provide the library with your own GPU driver implementation so that all rendering is
	/// performed using an existing GPU context (useful for game engines).
	/// <br/>
	/// When a View is rendered on the GPU, you can retrieve the backing texture ID via
	/// View::render_target().
	/// <br/><br/>
	/// Default Implementation
	/// <br/>
	/// A platform-specific implementation of GPUDriver is provided for you when you call App::Create(),
	/// (currently D3D11, Metal, and OpenGL). We recommend using these classes as a starting point for
	/// your own implementation (available open-source in the AppCore repository on GitHub).
	/// <br/><br/>
	/// Setting the GPU Driver
	/// <br/>
	/// When using Renderer::Create(), you can provide your own implementation of this
	/// class via Platform::set_gpu_driver().
	/// <br/><br/>
	/// State Synchronization
	/// <br/>
	/// During each call to Renderer::Render(), the library will update the state of the GPU driver
	/// (textures, render buffers, geometry, command lists, etc.) to match the current state of the
	/// library.
	/// <br/><br/>
	/// Detecting State Changes
	/// <br/>
	/// The library will call BeginSynchronize() before any state is updated and EndSynchronize() after
	/// all state is updated. All `Create` / `Update` / `Destroy` calls will be made between these two
	/// calls.
	/// <br/>
	/// This allows the GPU driver implementation to prepare the GPU for any state changes.
	/// <br/><br/>
	/// Drawing
	/// <br/>
	/// All drawing is done via command lists (UpdateCommandList()) to allow asynchronous execution
	/// of commands on the GPU.
	/// <br/>
	/// The library will dispatch a list of commands to the GPU driver during state synchronization. The
	/// GPU driver implementation should periodically consume the command list and execute the commands
	/// at an appropriate time.
	/// </summary>
	/// <seealso cref="Platform.GpuDriver"/>
	public interface IGpuDriver
	{
		/// <summary>
		/// Get the next available texture ID.
		///
		/// This is used to generate a unique texture ID for each texture created by the library. The
		/// GPU driver implementation is responsible for mapping these IDs to a native ID.
		/// </summary>
		/// <note>
		/// Numbering should start at 1, 0 is reserved for "no texture".
		/// </note>
		/// <returns>The next available texture ID.</returns>
		uint NextTextureId();

		/// <summary>
		/// Create a texture with a certain ID and optional bitmap.
		/// </summary>
		/// <param name="textureId">The texture ID to use for the new texture.</param>
		/// <param name="bitmap">The bitmap to initialize the texture with (can be empty).</param>
		/// <note>
		/// If the Bitmap is empty (Bitmap::IsEmpty), then an RTT Texture should be created instead.
		/// This will be used as a backing texture for a new RenderBuffer.
		/// </note>
		/// <warning>
		/// A deep copy of the bitmap data should be made if you are uploading it to the GPU
		/// asynchronously, it will not persist beyond this call.
		/// </warning>
		void CreateTexture(uint textureId, UlBitmap bitmap);

		/// <summary>
		/// Update an existing non-RTT texture with new bitmap data.
		/// </summary>
		/// <param name="textureId">The texture to update.</param>
		/// <param name="bitmap">The new bitmap data.</param>
		/// <warning>
		/// A deep copy of the bitmap data should be made if you are uploading it to the GPU
		/// asynchronously, it will not persist beyond this call.
		/// </warning>
		void UpdateTexture(uint textureId, UlBitmap bitmap);

		/// <summary>
		/// Destroy a texture.
		/// </summary>
		/// <param name="textureId">The texture to destroy.</param>
		void DestroyTexture(uint textureId);

		/// <summary>
		/// Get the next available render buffer ID.
		/// <br/>
		/// This is used to generate a unique render buffer ID for each render buffer created by the
		/// library. The GPU driver implementation is responsible for mapping these IDs to a native ID.
		/// </summary>
		/// <note>
		/// Numbering should start at 1, 0 is reserved for "no render buffer".
		/// </note>
		/// <returns>Returns the next available render buffer ID.</returns>
		uint NextRenderBufferId();

		/// <summary>
		/// Create a render buffer with certain ID and buffer description.
		/// </summary>
		/// <param name="renderBufferId">The render buffer ID to use for the new render buffer.</param>
		/// <param name="renderBuffer">The render buffer description.</param>
		void CreateRenderBuffer(uint renderBufferId, UlRenderBuffer renderBuffer);

		/// <summary>
		/// Destroy a render buffer.
		/// </summary>
		/// <param name="renderBufferId">The render buffer to destroy.</param>
		void DestroyRenderBuffer(uint renderBufferId);

		/// <summary>
		/// Get the next available geometry ID.
		///
		/// This is used to generate a unique geometry ID for each geometry created by the library. The
		/// GPU driver implementation is responsible for mapping these IDs to a native ID.
		/// </summary>
		/// <note>
		/// Numbering should start at 1, 0 is reserved for "no geometry".
		/// </note>
		/// <returns>
		/// Returns the next available geometry ID.
		/// </returns>
		uint NextGeometryId();

		/// <summary>
		/// Create geometry with certain ID and vertex/index data.
		/// </summary>
		/// <param name="geometryId">The geometry ID to use for the new geometry.</param>
		/// <param name="vertexBuffer">The vertex buffer data.</param>
		/// <param name="indexBuffer">The index buffer data.</param>
		/// <warning>
		/// A deep copy of the bitmap data should be made if you are uploading it to the GPU
		/// asynchronously, it will not persist beyond this call.
		/// </warning>
		void CreateGeometry(uint geometryId, UlVertexBuffer vertexBuffer, UlIndexBuffer indexBuffer);

		/// <summary>
		/// Update existing geometry with new vertex/index data.
		/// </summary>
		/// <param name="geometryId">The geometry to update.</param>
		/// <param name="vertexBuffer">The new vertex buffer data.</param>
		/// <param name="indexBuffer">The new index buffer data.</param>
		/// <warning>
		/// A deep copy of the bitmap data should be made if you are uploading it to the GPU
		/// asynchronously, it will not persist beyond this call.
		/// </warning>
		void UpdateGeometry(uint geometryId, UlVertexBuffer vertexBuffer, UlIndexBuffer indexBuffer);

		/// <summary>
		/// Destroy geometry.
		/// </summary>
		/// <param name="geometryId">The geometry to destroy.</param>
		void DestroyGeometry(uint geometryId);

		/// <summary>
		/// Update the pending command list with commands to execute on the GPU.
		///
		/// Commands are dispatched to the GPU driver asynchronously via this method. The GPU driver
		/// implementation should consume these commands and execute them at an appropriate time.
		/// </summary>
		/// <param name="commandList">The list of commands to execute.</param>
		/// <warning>
		/// Implementations should make a deep copy of the command list, it will not persist
		/// beyond this call.
		/// </warning>
		void UpdateCommandList(UlCommandList commandList);

		GpuDriver? GetNativeStruct()
		{
			return null;
		}

		internal sealed unsafe class Wrapper : IDisposable
		{
			private readonly GpuDriver _nativeStruct;

			private readonly Dictionary<nint, WeakReference<UlBitmap>>? _bitmapCache;

			private readonly GCHandle[]? _handles;

			private readonly IGpuDriver _instance;
			private uint _newCachedInstanceCount;

			public Wrapper(IGpuDriver instance)
			{
				_instance = instance;
				var nativeStruct = instance.GetNativeStruct();
				if (nativeStruct is not null)
				{
					NativeStruct = nativeStruct.Value;
					return;
				}

				if (instance is IGpuDriverSynchronized sync)
				{
					_handles = new GCHandle[14];

					NativeStruct = NativeStruct with
					{
						BeginSynchronize =
						(delegate* unmanaged[Cdecl]<void>)Helper.AllocateDelegate(sync.BeginSynchronize,
							out _handles[12]),
						EndSynchronize =
						(delegate* unmanaged[Cdecl]<void>)Helper.AllocateDelegate(sync.EndSynchronize, out _handles[13])
					};
				}
				else
				{
					_handles = new GCHandle[12];
				}

				_bitmapCache = new Dictionary<nint, WeakReference<UlBitmap>>(32);

				NativeStruct = NativeStruct with
				{
					NextTextureId =
					(delegate* unmanaged[Cdecl]<uint>)Helper.AllocateDelegate<IdCallback>(instance.NextTextureId,
						out _handles[0]),
					CreateTexture = (delegate* unmanaged[Cdecl]<uint, void*, void>)Helper.AllocateDelegate(
						(uint id, void* bitmap) => instance.CreateTexture(id, BitmapFromHandleCached(bitmap)),
						out _handles[1]),
					UpdateTexture = (delegate* unmanaged[Cdecl]<uint, void*, void>)Helper.AllocateDelegate(
						(uint id, void* bitmap) => instance.UpdateTexture(id, BitmapFromHandleCached(bitmap)),
						out _handles[2]),
					DestroyTexture =
					(delegate* unmanaged[Cdecl]<uint, void>)Helper.AllocateDelegate<DestroyIdCallback>(
						instance.DestroyTexture, out _handles[3]),
					NextRenderBufferId =
					(delegate* unmanaged[Cdecl]<uint>)Helper.AllocateDelegate<IdCallback>(instance.NextRenderBufferId,
						out _handles[4]),
					CreateRenderBuffer =
					(delegate* unmanaged[Cdecl]<uint, UlRenderBuffer, void>)Helper
						.AllocateDelegate<RenderBufferCallback>(instance.CreateRenderBuffer, out _handles[5]),
					DestroyRenderBuffer =
					(delegate* unmanaged[Cdecl]<uint, void>)Helper.AllocateDelegate<DestroyIdCallback>(
						instance.DestroyRenderBuffer, out _handles[6]),
					NextGeometryId =
					(delegate* unmanaged[Cdecl]<uint>)Helper.AllocateDelegate<IdCallback>(instance.NextGeometryId,
						out _handles[7]),
					CreateGeometry =
					(delegate* unmanaged[Cdecl]<uint, UlVertexBuffer, UlIndexBuffer, void>)Helper
						.AllocateDelegate<GeometryCallback>(instance.CreateGeometry, out _handles[8]),
					UpdateGeometry =
					(delegate* unmanaged[Cdecl]<uint, UlVertexBuffer, UlIndexBuffer, void>)Helper
						.AllocateDelegate<GeometryCallback>(instance.UpdateGeometry, out _handles[9]),
					DestroyGeometry =
					(delegate* unmanaged[Cdecl]<uint, void>)Helper.AllocateDelegate<DestroyIdCallback>(
						instance.DestroyGeometry, out _handles[10]),
					UpdateCommandList =
					(delegate* unmanaged[Cdecl]<UlCommandList, void>)Helper.AllocateDelegate<CommandListCallback>(
						instance.UpdateCommandList, out _handles[11])
				};
			}

			public GpuDriver NativeStruct
			{
				get
				{
					if (IsDisposed) throw new ObjectDisposedException(nameof(Wrapper));
					return _nativeStruct;
				}
				private init => _nativeStruct = value;
			}

			public bool IsDisposed { get; private set; }

			public void Dispose()
			{
				if (IsDisposed) return;
				if (_handles is not null)
					foreach (var handle in _handles)
						if (handle.IsAllocated)
							handle.Free();

				GC.SuppressFinalize(this);
				IsDisposed = true;
			}

			private UlBitmap BitmapFromHandleCached(void* ptr)
			{
				if (!_bitmapCache!.TryGetValue((nint)ptr, out var weakBitmap) ||
				    !weakBitmap.TryGetTarget(out var bitmap))
				{
					bitmap = UlBitmap.FromHandle(ptr, false);
					_bitmapCache[(nint)ptr] = new WeakReference<UlBitmap>(bitmap);
					_newCachedInstanceCount++;
				}

				if (_newCachedInstanceCount > 256)
				{
					foreach (var keyValuePair in _bitmapCache)
						if (!keyValuePair.Value.TryGetTarget(out _))
							_bitmapCache.Remove(keyValuePair.Key);
					_newCachedInstanceCount = 0;
				}

				return bitmap;
			}

			~Wrapper()
			{
				Dispose();
			}

			private delegate uint IdCallback();

			private delegate void RenderBufferCallback(uint id, UlRenderBuffer renderBuffer);

			private delegate void GeometryCallback(uint id, UlVertexBuffer vertexBuffer, UlIndexBuffer indexBuffer);

			private delegate void DestroyIdCallback(uint id);

			private delegate void CommandListCallback(UlCommandList commandList);
		}
	}

	/// <inheritdoc cref="IGpuDriver"/>
	public interface IGpuDriverSynchronized : IGpuDriver
	{
		/// <summary>
		/// Called before any state (eg, CreateTexture(), UpdateTexture(), DestroyTexture(), etc.) is
		/// updated during a call to Renderer::Render().
		///
		/// This is a good time to prepare the GPU for any state updates.
		/// </summary>
		void BeginSynchronize();

		/// <summary>
		/// Called after all state has been updated during a call to Renderer::Render().
		/// </summary>
		void EndSynchronize();
	}
}
