using System.Runtime.InteropServices;

namespace UltralightNet.Handles;

[StructLayout(LayoutKind.Sequential)]
public unsafe partial struct UlFontFile : IDisposable // TODO: INativeContainer ?
{
	private nuint handle;

	private UlFontFile(nuint handle)
	{
		this.handle = handle;
	}

	public static UlFontFile CreateFromFile(UlString* path)
	{
		return new UlFontFile(ulFontFileCreateFromFilePath(path));
	}

	public static UlFontFile CreateFromFile(ReadOnlySpan<char> path)
	{
		using UlString pathUl = new(path);
		return CreateFromFile(&pathUl);
	}

	public static UlFontFile Create(UlBuffer buffer)
	{
		return new UlFontFile(ulFontFileCreateFromBuffer(buffer));
	}

	public void Dispose()
	{
		if (handle is 0) return;

		ulDestroyFontFile(handle);
		handle = 0;
	}

	[LibraryImport(Methods.LibUltralight)]
	private static partial nuint ulFontFileCreateFromFilePath(UlString* path);

	[LibraryImport(Methods.LibUltralight)]
	private static partial nuint ulFontFileCreateFromBuffer(UlBuffer buffer);

	[LibraryImport(Methods.LibUltralight)]
	private static partial void ulDestroyFontFile(nuint handle);
}
