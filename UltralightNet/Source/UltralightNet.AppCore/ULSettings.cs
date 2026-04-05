using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace UltralightNet.AppCore;

[NativeMarshalling(typeof(Marshaller))]
public struct ULSettings : IEquatable<ULSettings>
{
	public string DeveloperName = "MyCompany";
	public string AppName = "MyApp";
	public string FileSystemPath = "./assets/";
	public bool LoadShadersFromFileSystem = false;
	public bool ForceCPURenderer = false;

	public ULSettings()
	{
	}

	public readonly bool Equals(ULSettings settings)
	{
		return DeveloperName == settings.DeveloperName &&
		       AppName == settings.AppName &&
		       FileSystemPath == settings.FileSystemPath &&
		       LoadShadersFromFileSystem == settings.LoadShadersFromFileSystem &&
		       ForceCPURenderer == settings.ForceCPURenderer;
	}

	[StructLayout(LayoutKind.Sequential)]
	[CustomMarshaller(typeof(ULSettings), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
	internal struct Marshaller
	{
		private UlString DeveloperName;
		private UlString AppName;

		private UlString FileSystemPath;

		private byte LoadShadersFromFileSystem;

		private byte ForceCPURenderer;

		public void FromManaged(ULSettings settings)
		{
			DeveloperName = new UlString(settings.DeveloperName.AsSpan());
			AppName = new UlString(settings.AppName.AsSpan());
			FileSystemPath = new UlString(settings.FileSystemPath.AsSpan());
			LoadShadersFromFileSystem = Methods.BitCast<bool, byte>(settings.LoadShadersFromFileSystem);
			ForceCPURenderer = Methods.BitCast<bool, byte>(settings.ForceCPURenderer);
		}

		public readonly Marshaller ToUnmanaged()
		{
			return this;
		}

		public void Free()
		{
			DeveloperName.Dispose();
			AppName.Dispose();
			FileSystemPath.Dispose();
		}
	}
}
