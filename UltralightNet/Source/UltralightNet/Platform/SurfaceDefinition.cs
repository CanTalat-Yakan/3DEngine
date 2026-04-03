using System.Runtime.InteropServices;
using UltralightNet.Platform.HighPerformance;

namespace UltralightNet.Platform
{
	namespace HighPerformance
	{
		/// <summary>
		///     <see cref="ISurfaceDefinition" /> native definition.
		/// </summary>
		public unsafe struct UlSurfaceDefinition
		{
			#if !NETSTANDARD
			public delegate* unmanaged[Cdecl]<uint, uint, nint> Create;
			public delegate* unmanaged[Cdecl]<nint, void> Destroy;
			public delegate* unmanaged[Cdecl]<nint, uint> GetWidth;
			public delegate* unmanaged[Cdecl]<nint, uint> GetHeight;
			public delegate* unmanaged[Cdecl]<nint, uint> GetRowBytes;
			public delegate* unmanaged[Cdecl]<nint, nuint> GetSize;
			public delegate* unmanaged[Cdecl]<nint, byte*> LockPixels;
			public delegate* unmanaged[Cdecl]<nint, void> UnlockPixels;
			public delegate* unmanaged[Cdecl]<nint, uint, uint, void> Resize;
			#else
			public void* Create, Destroy, GetWidth, GetHeight, GetRowBytes, GetSize, LockPixels, UnlockPixels, Resize;
			#endif
		}
	}

	public interface ISurfaceDefinition
	{
		nint Create(uint width, uint height);
		void Destroy(nint id);
		uint GetWidth(nint id);
		uint GetHeight(nint id);
		uint GetRowBytes(nint id);
		nuint GetSize(nint id);
		unsafe byte* LockPixels(nint id);
		void UnlockPixels(nint id);
		void Resize(nint id, uint width, uint height);

		#if !NETSTANDARD2_0
		virtual UlSurfaceDefinition? GetNativeStruct()
		{
			return null;
		}
		#else
		ULSurfaceDefinition? GetNativeStruct();
		#endif

		internal sealed unsafe class Wrapper : IDisposable
		{
			private readonly UlSurfaceDefinition _nativeStruct;

			private readonly GCHandle[]? _handles;

			private readonly ISurfaceDefinition _instance;

			public Wrapper(ISurfaceDefinition instance)
			{
				this._instance = instance;
				var nativeStruct = instance.GetNativeStruct();
				if (nativeStruct is not null)
				{
					NativeStruct = nativeStruct.Value;
					return;
				}

				_handles = new GCHandle[9];

				NativeStruct = new UlSurfaceDefinition
				{
					Create = (delegate* unmanaged[Cdecl]<uint, uint, nint>)Helper.AllocateDelegate<CreateCallback>(
						instance.Create, out _handles[0]),
					Destroy = (delegate* unmanaged[Cdecl]<nint, void>)Helper.AllocateDelegate<VoidIdCallback>(
						instance.Destroy, out _handles[1]),
					GetWidth =
						(delegate* unmanaged[Cdecl]<nint, uint>)Helper.AllocateDelegate<UintIdCallback>(
							instance.GetWidth, out _handles[2]),
					GetHeight =
						(delegate* unmanaged[Cdecl]<nint, uint>)Helper.AllocateDelegate<UintIdCallback>(
							instance.GetHeight, out _handles[3]),
					GetRowBytes =
						(delegate* unmanaged[Cdecl]<nint, uint>)Helper.AllocateDelegate<UintIdCallback>(
							instance.GetRowBytes, out _handles[4]),
					GetSize = (delegate* unmanaged[Cdecl]<nint, nuint>)Helper.AllocateDelegate<NUintIdCallback>(
						instance.GetSize, out _handles[5]),
					LockPixels =
						(delegate* unmanaged[Cdecl]<nint, byte*>)Helper.AllocateDelegate<BytePtrIdCallback>(
							instance.LockPixels, out _handles[6]),
					UnlockPixels =
						(delegate* unmanaged[Cdecl]<nint, void>)Helper.AllocateDelegate<VoidIdCallback>(
							instance.UnlockPixels, out _handles[7]),
					Resize =
						(delegate* unmanaged[Cdecl]<nint, uint, uint, void>)Helper.AllocateDelegate<ResizeCallback>(
							instance.Resize, out _handles[8])
				};
			}

			public UlSurfaceDefinition NativeStruct
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

			~Wrapper()
			{
				Dispose();
			}

			private delegate nint CreateCallback(uint width, uint height);

			private delegate void VoidIdCallback(nint id);

			private delegate uint UintIdCallback(nint id);

			private delegate nuint NUintIdCallback(nint id);

			private delegate byte* BytePtrIdCallback(nint id);

			private delegate void ResizeCallback(nint id, uint width, uint height);
		}
	}
}
