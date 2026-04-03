using System;
using System.IO;
using UltralightNet.Handles;
using UltralightNet.Platform;

namespace UltralightNet.Veldrid;

public class UlFileSystem : IFileSystem
{
	private static readonly string BasePath = Environment.CurrentDirectory;

	public bool FileExists(string path)
	{
		return File.Exists(Path.Combine(BasePath, path));
	}

	public string GetFileMimeType(string path)
	{
		return "application/octet-stream";
	}

	public string GetFileCharset(string path)
	{
		return "utf-8";
	}

	public UlBuffer OpenFile(string path)
	{
		byte[] bytes = File.ReadAllBytes(Path.Combine(BasePath, path));
		return UlBuffer.CreateFromDataCopy<byte>(bytes);
	}
}
