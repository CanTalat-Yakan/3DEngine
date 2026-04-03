using System.Runtime.InteropServices;
using UltralightNet.Handles;
using UltralightNet.Platform.HighPerformance;

namespace UltralightNet.Platform
{
	namespace HighPerformance
	{
		/// <summary>
		///     <see cref="IFileSystem" /> native definition.
		/// </summary>
		public unsafe struct UlFileSystem
		{
			#if !NETSTANDARD
			public delegate* unmanaged[Cdecl]<UlString*, bool> FileExists;
			public delegate* unmanaged[Cdecl]<UlString*, UlString*> GetFileMimeType;
			public delegate* unmanaged[Cdecl]<UlString*, UlString*> GetFileCharset;
			public delegate* unmanaged[Cdecl]<UlString*, UlBuffer> OpenFile;
			#else
			public void* FileExists, GetFileMimeType, GetFileCharset, OpenFile;
			#endif
		}
	}

	public interface IFileSystem
	{
		bool FileExists(string path);
		string GetFileMimeType(string path);
		string GetFileCharset(string path);
		UlBuffer OpenFile(string path);

		#if !NETSTANDARD2_0
		virtual UlFileSystem? GetNativeStruct()
		{
			return null;
		}
		#else
		ULFileSystem? GetNativeStruct();
		#endif

		internal sealed unsafe class Wrapper : IDisposable
		{
			private readonly UlFileSystem _NativeStruct;

			private readonly GCHandle[]? handles;
			private readonly IFileSystem instance;

			public Wrapper(IFileSystem instance)
			{
				this.instance = instance;
				var nativeStruct = instance.GetNativeStruct();
				if (nativeStruct is not null)
				{
					NativeStruct = nativeStruct.Value;
					return;
				}

				handles = new GCHandle[4];

				NativeStruct = new UlFileSystem
				{
					FileExists =
						(delegate* unmanaged[Cdecl]<UlString*, bool>)Helper.AllocateDelegate(
							(UlString* path) => instance.FileExists(path->ToString()), out handles[0]),
					GetFileMimeType = (delegate* unmanaged[Cdecl]<UlString*, UlString*>)Helper.AllocateDelegate(
						(UlString* path) =>
							new UlString(instance.GetFileMimeType(path->ToString()).AsSpan()).Allocate(),
						out handles[1]),
					GetFileCharset = (delegate* unmanaged[Cdecl]<UlString*, UlString*>)Helper.AllocateDelegate(
						(UlString* path) => new UlString(instance.GetFileCharset(path->ToString()).AsSpan()).Allocate(),
						out handles[2]),
					OpenFile = (delegate* unmanaged[Cdecl]<UlString*, UlBuffer>)Helper.AllocateDelegate(
						(UlString* path) => instance.OpenFile(path->ToString()), out handles[3])
				};
			}

			public UlFileSystem NativeStruct
			{
				get
				{
					if (IsDisposed) throw new ObjectDisposedException(nameof(Wrapper));
					return _NativeStruct;
				}
				private init => _NativeStruct = value;
			}

			public bool IsDisposed { get; private set; }

			public void Dispose()
			{
				if (IsDisposed) return;
				if (handles is not null)
					foreach (var handle in handles)
						if (handle.IsAllocated)
							handle.Free();

				GC.SuppressFinalize(this);
				IsDisposed = true;
			}

			~Wrapper()
			{
				Dispose();
			}
		}
	}
}
