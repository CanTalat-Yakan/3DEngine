using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.Marshalling;

namespace UltralightNet;

[NativeMarshalling(typeof(Marshaller))]
public struct ViewConfig : IEquatable<ViewConfig>
{
	/// <summary>
	///     A user-generated id for the display (monitor, TV, or screen) that this View will be shown on.
	///     <br /><br />
	///     Animations are driven based on the physical refresh rate of the display. Multiple Views can
	///     share the same display.
	/// </summary>
	/// <note>
	///     This is automatically managed for you when App::Create() is used.
	/// </note>
	/// <seealso cref="Renderer.RefreshDisplay" />
	public uint DisplayId = 0;

	/// <summary>
	///     Whether to render using the GPU renderer (accelerated) or the CPU renderer (un-accelerated).
	///     <br /><br />
	///     When true, the View will be rendered to an offscreen GPU texture using the GPU driver set in
	///     Platform::set_gpu_driver. You can fetch details for the texture via View::render_target.
	///     <br /><br />
	///     When false (the default), the View will be rendered to an offscreen pixel buffer using the
	///     multithreaded CPU renderer. This pixel buffer can optionally be provided by the user--
	///     for more info see Platform::set_surface_factory and View::surface.
	/// </summary>
	/// <note>
	///     This is automatically managed for you when App::Create() is used.
	/// </note>
	public bool IsAccelerated = false;

	/// <summary>
	///     The initial device scale, i.e. the amount to scale page units to screen pixels. This should
	///     be set to the scaling factor of the device that the View is displayed on.
	/// </summary>
	/// <note>
	///     1.0 is equal to 100% zoom (no scaling), 2.0 is equal to 200% zoom (2x scaling)
	///     <br /><br />
	///     This is automatically managed for you when App::Create() is used.
	/// </note>
	public double InitialDeviceScale = 1.0;

	/// <summary>Whether this View should support transparency.</summary>
	/// <note>
	///     Make sure to also set the following CSS on the page:
	///     <code>
	/// 		html, body { background: transparent; }
	/// 		</code>
	/// </note>
	public bool IsTransparent = false;

	/// <summary>
	///     Whether the View should initially have input focus.
	/// </summary>
	/// <seealso cref="View.Focus" />
	public bool InitialFocus = true;

	/// <summary>Whether images should be enabled.</summary>
	public bool EnableImages = true;

	/// <summary>Whether JavaScript should be enabled.</summary>
	public bool EnableJavaScript = true;

	/// <summary>
	///     Whether compositing should be enabled.
	/// </summary>
	public bool EnableCompositor = false;

	/// <summary>Default font-family to use.</summary>
	public string FontFamilyStandard = "Times New Roman";

	/// <summary>Default font-family to use for fixed fonts. (pre/code)</summary>
	public string FontFamilyFixed = "Courier New";

	/// <summary>Default font-family to use for serif fonts.</summary>
	public string FontFamilySerif = "Times New Roman";

	/// <summary>Default font-family to use for sans-serif fonts.</summary>
	public string FontFamilySansSerif = "Arial";

	/// <summary>
	///     Custom user-agent string. You can use this to override the default user-agent string.
	/// </summary>
	/// <remarks>This feature is only available in Ultralight Pro edition and above.</remarks>
	public string UserAgent =
		@"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/615.1.18.100.1 (KHTML, like Gecko) Ultralight/1.4.0 Version/16.4.1 Safari/615.1.18.100.1\";

	public ViewConfig()
	{
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return obj is ViewConfig config && Equals(config);
	}

	public override int GetHashCode()
	{
		var hashCode = new HashCode();
		hashCode.Add(DisplayId);
		hashCode.Add(IsAccelerated);
		hashCode.Add(InitialDeviceScale);
		hashCode.Add(IsTransparent);
		hashCode.Add(InitialFocus);
		hashCode.Add(EnableImages);
		hashCode.Add(EnableJavaScript);
		hashCode.Add(EnableCompositor);
		hashCode.Add(FontFamilyStandard);
		hashCode.Add(FontFamilyFixed);
		hashCode.Add(FontFamilySerif);
		hashCode.Add(FontFamilySansSerif);
		hashCode.Add(UserAgent);
		return hashCode.ToHashCode();
	}

	[CustomMarshaller(typeof(ViewConfig), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
	internal struct Marshaller
	{
		public uint DisplayId;

		public byte IsAccelerated;
		public double InitialDeviceScale;
		public byte IsTransparent;

		public byte InitialFocus;

		public byte EnableImages;
		public byte EnableJavaScript;
		public byte EnableCompositor;

		public UlString FontFamilyStandard;
		public UlString FontFamilyFixed;
		public UlString FontFamilySerif;
		public UlString FontFamilySansSerif;

		public UlString UserAgent;

		public void FromManaged(ViewConfig config)
		{
			DisplayId = config.DisplayId;
			IsAccelerated = Methods.BitCast<bool, byte>(config.IsAccelerated);
			InitialDeviceScale = config.InitialDeviceScale;
			IsTransparent = Methods.BitCast<bool, byte>(config.IsTransparent);
			InitialFocus = Methods.BitCast<bool, byte>(config.InitialFocus);
			EnableImages = Methods.BitCast<bool, byte>(config.EnableImages);
			EnableJavaScript = Methods.BitCast<bool, byte>(config.EnableJavaScript);
			EnableCompositor = Methods.BitCast<bool, byte>(config.EnableCompositor);
			FontFamilyStandard = new UlString(config.FontFamilyStandard.AsSpan());
			FontFamilyFixed = new UlString(config.FontFamilyFixed.AsSpan());
			FontFamilySerif = new UlString(config.FontFamilySerif.AsSpan());
			FontFamilySansSerif = new UlString(config.FontFamilySansSerif.AsSpan());
			UserAgent = new UlString(config.UserAgent.AsSpan());
		}

		public readonly Marshaller ToUnmanaged()
		{
			return this;
		}

		public void Free()
		{
			FontFamilyStandard.Dispose();
			FontFamilyFixed.Dispose();
			FontFamilySerif.Dispose();
			FontFamilySansSerif.Dispose();
			UserAgent.Dispose();
		}
	}

	public readonly bool Equals(ViewConfig other)
	{
		return DisplayId == other.DisplayId && IsAccelerated == other.IsAccelerated && InitialDeviceScale.Equals(other.InitialDeviceScale) && IsTransparent == other.IsTransparent && InitialFocus == other.InitialFocus && EnableImages == other.EnableImages && EnableJavaScript == other.EnableJavaScript && EnableCompositor == other.EnableCompositor && FontFamilyStandard == other.FontFamilyStandard && FontFamilyFixed == other.FontFamilyFixed && FontFamilySerif == other.FontFamilySerif && FontFamilySansSerif == other.FontFamilySansSerif && UserAgent == other.UserAgent;
	}
}
