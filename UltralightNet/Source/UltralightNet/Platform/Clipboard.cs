using System.Runtime.InteropServices;
using UltralightNet.Platform.HighPerformance;

namespace UltralightNet.Platform
{
	namespace HighPerformance
	{
		/// <summary>
		///     <see cref="IClipboard" /> native definition.
		/// </summary>
		public unsafe struct UlClipboard
		{
			#if !NETSTANDARD
			public delegate* unmanaged[Cdecl]<void> Clear;
			public delegate* unmanaged[Cdecl]<UlString*, void> ReadPlainText;
			public delegate* unmanaged[Cdecl]<UlString*, void> WritePlainText;
			#else
			public void* Clear, ReadPlainText, WritePlainText;
			#endif
		}
	}

	public interface IClipboard
	{
		void Clear();
		string ReadPlainText();
		void WritePlainText(string text);

		#if !NETSTANDARD2_0
		virtual UlClipboard? GetNativeStruct()
		{
			return null;
		}
		#else
		ULClipboard? GetNativeStruct();
		#endif

		internal sealed unsafe class Wrapper : IDisposable
		{
			private readonly UlClipboard _nativeStruct;

			private readonly GCHandle[]? _handles;
			private readonly IClipboard _instance;

			public Wrapper(IClipboard instance)
			{
				_instance = instance;
				var nativeStruct = instance.GetNativeStruct();
				if (nativeStruct is not null)
				{
					NativeStruct = nativeStruct.Value;
					return;
				}

				_handles = new GCHandle[3];

				NativeStruct = new UlClipboard
				{
					Clear = (delegate* unmanaged[Cdecl]<void>)Helper.AllocateDelegate(instance.Clear, out _handles[0]),
					ReadPlainText = (delegate* unmanaged[Cdecl]<UlString*, void>)Helper.AllocateDelegate(
						() => new UlString(instance.ReadPlainText().AsSpan()).Allocate(), out _handles[1]),
					WritePlainText =
						(delegate* unmanaged[Cdecl]<UlString*, void>)Helper.AllocateDelegate(
							(UlString* text) => instance.WritePlainText(text->ToString()), out _handles[2])
				};
			}

			public UlClipboard NativeStruct
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
