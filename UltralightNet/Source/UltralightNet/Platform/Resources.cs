using System.Reflection;

namespace UltralightNet.Platform;

public static class Resources
{
	private static Assembly Assembly => typeof(Resources).Assembly;

	public static Stream? Cacertpem => Assembly.GetManifestResourceStream("UltralightNet.runtimes.cacert.pem");
	public static Stream? Icudt67Ldat => Assembly.GetManifestResourceStream("UltralightNet.runtimes.icudt67l.dat");
}
