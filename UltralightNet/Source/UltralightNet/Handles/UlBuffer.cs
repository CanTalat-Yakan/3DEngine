using System.Runtime.InteropServices;

namespace UltralightNet.Handles;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void DestroyBufferCallback(void* userData, void* data);

public unsafe struct UlBuffer : IDisposable, IEquatable<UlBuffer> // TODO: INativeContainer
{
	private nuint _handle;

	public static UlBuffer CreateFromOwnedData(void* data, nuint length,
		delegate* unmanaged[Cdecl]<void*, void*, void> destroyCallback = null, void* userData = null)
	{
		// ReSharper disable once ConditionIsAlwaysTrueOrFalse
		return data is null && destroyCallback is not null
			? throw new ArgumentException("Callback will not be called, if data is null", nameof(data))
			: ulCreateBuffer(data, length, userData, destroyCallback);

		[DllImport(Methods.LibUltralight)]
		static extern UlBuffer ulCreateBuffer(void* data, nuint length, void* userData,
			delegate* unmanaged[Cdecl]<void*, void*, void> destroyCallback);
	}

	public static UlBuffer CreateFromOwnedData(void* data, nuint size, DestroyBufferCallback? destroyCallback,
		void* userData = null)
	{
		return CreateFromOwnedData(
			data is null && destroyCallback is not null
				? throw new ArgumentException("Callback will not be called, if data is null", nameof(data))
				: data, size,
			destroyCallback is not null
				? (delegate* unmanaged[Cdecl]<void*, void*, void>)Marshal.GetFunctionPointerForDelegate(destroyCallback)
				: null, userData);
	}

	public static UlBuffer CreateFromOwnedData<T>(ReadOnlySpan<T> data,
		delegate* unmanaged[Cdecl]<void*, void*, void> destroyCallback = null, void* userData = null)
		where T : unmanaged
	{
		fixed (T* dataPointer = data)
		{
			return CreateFromOwnedData(dataPointer, (nuint)data.Length * (nuint)sizeof(T), destroyCallback, userData);
		}
	}

	public static UlBuffer CreateFromOwnedData<T>(ReadOnlySpan<T> data, DestroyBufferCallback? destroyCallback,
		void* userData = null) where T : unmanaged
	{
		fixed (T* dataPointer = data)
		{
			return CreateFromOwnedData(dataPointer, (nuint)data.Length * (nuint)sizeof(T), destroyCallback, userData);
		}
	}

	public static UlBuffer CreateFromDataCopy(void* data, nuint length)
	{
		if (data is null && length is not 0) throw new ArgumentNullException(nameof(data));

		return ulCreateBufferFromCopy(data, length);

		[DllImport(Methods.LibUltralight)]
		static extern UlBuffer ulCreateBufferFromCopy(void* data, nuint length);
	}

	public static UlBuffer CreateFromDataCopy<T>(ReadOnlySpan<T> data) where T : unmanaged
	{
		fixed (T* dataPointer = data)
		{
			return CreateFromDataCopy(dataPointer, (nuint)data.Length * (nuint)sizeof(T));
		}
	}

	public void Dispose()
	{
		if (_handle is 0) return;
		ulDestroyBuffer(_handle);
		_handle = 0;
		return;

		[DllImport(Methods.LibUltralight)]
		static extern void* ulDestroyBuffer(nuint buffer);
	}

	public readonly bool IsDisposed => _handle is 0;
	private readonly nuint Handle => !IsDisposed ? _handle : throw new ObjectDisposedException(nameof(UlBuffer));

	/// <summary>Use <see cref="MemoryMarshal.Cast{TFrom, TTo}(Span{TFrom})" /> to convert it to type of your choice</summary>
	public readonly Span<byte> DataSpan => new(Data, checked((int)Size));

	public readonly byte* Data
	{
		get
		{
			return ulBufferGetData(Handle);

			[DllImport(Methods.LibUltralight)]
			static extern byte* ulBufferGetData(nuint buffer);
		}
	}

	public readonly nuint Size
	{
		get
		{
			return ulBufferGetSize(Handle);

			[DllImport(Methods.LibUltralight)]
			static extern nuint ulBufferGetSize(nuint buffer);
		}
	}

	public readonly void* UserData
	{
		get
		{
			return ulBufferGetUserData(Handle);

			[DllImport(Methods.LibUltralight)]
			static extern void* ulBufferGetUserData(nuint buffer);
		}
	}

	public readonly bool OwnsData
	{
		get
		{
			return ulBufferOwnsData(Handle) != 0;

			[DllImport(Methods.LibUltralight)]
			static extern byte ulBufferOwnsData(nuint buffer);
		}
	}

	public readonly bool Equals(UlBuffer buffer) // so called "premature" optimization...
	{
		if (_handle == buffer._handle) return true;
		if (_handle is 0 || buffer._handle is 0) return false;
		var size = Size;
		if (size != buffer.Size) return false;
		byte* data = Data, bufferData = buffer.Data;
		if (data == bufferData) return true;
		if (size < int.MaxValue)
			return new ReadOnlySpan<byte>(data, unchecked((int)size)).SequenceEqual(
				new ReadOnlySpan<byte>(bufferData, unchecked((int)size)));
		for (nuint i = 0; i < Size; i++)
			if (data[i] != bufferData[i])
				return false;
		return true;
	}

	public readonly override bool Equals(object? obj)
	{
		return obj is UlBuffer buffer && Equals(buffer);
	}

	public override int GetHashCode()
	{
		return unchecked((int)_handle);
	}

	public static bool operator ==(UlBuffer left, UlBuffer right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(UlBuffer left, UlBuffer right)
	{
		return !(left == right);
	}
}
