using System.Diagnostics.CodeAnalysis;

namespace UltralightNet;

public abstract unsafe class NativeContainer : IDisposable, IEquatable<NativeContainer>
{
	private readonly bool _owns = true;
	private void* _handle;

	protected virtual void* Handle
	{
		get => !IsDisposed ? _handle : throw new ObjectDisposedException(nameof(NativeContainer));
		init => _handle = value;
	}

	public bool IsDisposed { get; private set; }

	protected bool Owns
	{
		get => _owns;
		[SuppressMessage("Usage", "CA1816: Call GC.SuppressFinalize correctly")]
		init
		{
			if (value is false) GC.SuppressFinalize(this);
			_owns = value;
		}
	}

	public virtual void Dispose()
	{
		IsDisposed = true;
		_handle = default;
		GC.SuppressFinalize(this);
	}

	public bool Equals(NativeContainer? other)
	{
		return other is not null && Handle == other.Handle;
	}

	~NativeContainer()
	{
		Dispose();
		// it does work (tested on MODiX)
	}

	public override bool Equals(object? other)
	{
		return other is NativeContainer container && Equals(container);
	}

	public override int GetHashCode()
	{
		throw new NotSupportedException(
			$"Instances of {nameof(NativeContainer)} do not support {nameof(GetHashCode)}.");
	}
}
