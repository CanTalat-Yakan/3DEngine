using System.Runtime.InteropServices;
using UltralightNet.Enums;
using UltralightNet.Platform.HighPerformance;

namespace UltralightNet.Platform
{
	namespace HighPerformance
	{
		/// <summary>
		///     <see cref="ILogger" /> native definition.
		/// </summary>
		public unsafe struct UlLogger
		{
			#if !NETSTANDARD
			public delegate* unmanaged[Cdecl]<LogLevel, UlString*, void> LogMessage;
			#else
			public void* LogMessage;
			#endif
		}
	}

	public interface ILogger
	{
		void LogMessage(LogLevel logLevel, string message);

		#if !NETSTANDARD2_0
		virtual UlLogger? GetNativeStruct()
		{
			return null;
		}
		#else
		ULLogger? GetNativeStruct();
		#endif
		internal sealed unsafe class Wrapper
		{
			private readonly UlLogger _nativeStruct;

			private GCHandle _handle;
			private readonly ILogger _instance;

			public Wrapper(ILogger instance)
			{
				_instance = instance;
				var nativeStruct = instance.GetNativeStruct();
				if (nativeStruct is not null)
				{
					NativeStruct = nativeStruct.Value;
					return;
				}

				NativeStruct = new UlLogger
				{
					LogMessage = (delegate* unmanaged[Cdecl]<LogLevel, UlString*, void>)Helper.AllocateDelegate(
						(LogLevel logLevel, UlString* message) => instance.LogMessage(logLevel, message->ToString()),
						out _handle)
				};
			}

			public UlLogger NativeStruct
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
				if (_handle.IsAllocated) _handle.Free();

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
