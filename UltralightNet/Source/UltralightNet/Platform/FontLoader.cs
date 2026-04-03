using System.Runtime.InteropServices;
using UltralightNet.Handles;
using UltralightNet.Platform.HighPerformance;

namespace UltralightNet.Platform
{
	namespace HighPerformance
	{
		/// <summary>
		///     <see cref="IFontLoader" /> native definition.
		/// </summary>
		public unsafe struct UlFontLoader
		{
			#if !NETSTANDARD
			public delegate* unmanaged[Cdecl]<UlString*> GetFallbackFont;
			public delegate* unmanaged[Cdecl]<UlString*, int, bool, UlString*> GetFallbackFontForCharacters;
			public delegate* unmanaged[Cdecl]<UlString*, int, bool, UlFontFile> Load;
			#else
			public void* GetFallbackFont, GetFallbackFontForCharacters, Load;
			#endif
		}
	}

	public interface IFontLoader
	{
		string GetFallbackFont();
		string GetFallbackFontForCharacters(string text, int weight, bool italic);
		UlFontFile Load(string font, int weight, bool italic);

		#if !NETSTANDARD2_0
		virtual UlFontLoader? GetNativeStruct()
		{
			return null;
		}
		#else
		ULFontLoader? GetNativeStruct();
		#endif

		internal sealed unsafe class Wrapper : IDisposable
		{
			private readonly UlFontLoader _nativeStruct;

			private readonly GCHandle[]? _handles;
			private readonly IFontLoader _instance;

			public Wrapper(IFontLoader instance)
			{
				this._instance = instance;
				var nativeStruct = instance.GetNativeStruct();
				if (nativeStruct is not null)
				{
					NativeStruct = nativeStruct.Value;
					return;
				}

				_handles = new GCHandle[3];

				NativeStruct = new UlFontLoader
				{
					GetFallbackFont = (delegate* unmanaged[Cdecl]<UlString*>)Helper.AllocateDelegate(
						() => new UlString(instance.GetFallbackFont().AsSpan()).Allocate(), out _handles[0]),
					GetFallbackFontForCharacters =
						(delegate* unmanaged[Cdecl]<UlString*, int, bool, UlString*>)Helper.AllocateDelegate(
							(UlString* text, int weight, bool italic) =>
								new UlString(instance.GetFallbackFontForCharacters(text->ToString(), weight, italic)
									.AsSpan()).Allocate(), out _handles[1]),
					Load = (delegate* unmanaged[Cdecl]<UlString*, int, bool, UlFontFile>)Helper.AllocateDelegate(
						(UlString* font, int weight, bool italic) => instance.Load(font->ToString(), weight, italic),
						out _handles[2])
				};
			}

			public UlFontLoader NativeStruct
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
		}
	}
}
