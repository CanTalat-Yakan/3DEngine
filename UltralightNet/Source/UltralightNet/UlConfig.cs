using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.Marshalling;
using UltralightNet.Enums;

namespace UltralightNet;

/// <summary>Configuration settings for Ultralight.</summary>
[NativeMarshalling(typeof(Marshaller))]
public struct UlConfig : IEquatable<UlConfig>
{
	/// <summary>
	///     The file path to a writable directory that will be used to store cookies, cached resources, and other
	///     persistent data.
	/// </summary>
	public string CachePath = string.Empty;

	/// <summary>Path to use for filesystem resources.</summary>
	public string ResourcePathPrefix = "runtimes/";

	/// <summary>Face winding for ULGPUDriver.</summary>
	public FaceWinding FaceWinding = FaceWinding.CounterClockwise;

	/// <summary>The hinting algorithm to use when rendering fonts.</summary>
	/// <see cref="Enums.FontHinting" />
	public FontHinting FontHinting = FontHinting.Normal;

	/// <summary>
	///     The gamma to use when compositing font glyphs, change this value to adjust contrast (Adobe and Apple prefer
	///     1.8, others may prefer 2.2).
	/// </summary>
	public double FontGamma = 1.8;

	/// <summary>Default user style (CSS).</summary>
	public string UserStylesheet = string.Empty;

	/// <summary>
	///     Whether or not we should continuously repaint any Views or compositor layers, regardless if they are dirty or
	///     not.
	/// </summary>
	/// <remarks>This is mainly used to diagnose painting/shader issues.</remarks>
	public bool ForceRepaint = false;

	/// <summary>When a CSS animation is active, the amount of time (in seconds) to wait before triggering another repaint.</summary>
	public double AnimationTimerDelay = 1.0 / 60.0;

	/// <summary>
	///     When a smooth scroll animation is active, the amount of time (in seconds) to wait before triggering another
	///     repaint.
	/// </summary>
	public double ScrollTimerDelay = 1.0 / 60.0;

	/// <summary>
	///     The amount of time (in seconds) to wait before running the recycler (will attempt to return excess memory back
	///     to the system).
	/// </summary>
	public double RecycleDelay = 4.0;

	/// <summary>Size of WebCore's memory cache in bytes.</summary>
	/// <remarks>
	///     You should increase this if you anticipate handling pages with large resources, Safari typically uses 128+ MiB
	///     for its cache.
	/// </remarks>
	public uint MemoryCacheSize = 64 * 1024 * 1024;

	/// <summary>Number of pages to keep in the cache. Defaults to 0 (none).</summary>
	/// <remarks>
	///     Safari typically caches about 5 pages and maintains an on-disk cache to support typical web-browsing
	///     activities. If you increase this, you should probably increase the memory cache size as well.
	/// </remarks>
	public uint PageCacheSize = 0;

	/// <summary>
	///     JavaScriptCore tries to detect the system's physical RAM size to set reasonable allocation limits. Set this to
	///     anything other than 0 to override the detected value. Size is in bytes.
	/// </summary>
	/// <remarks>
	///     This can be used to force JavaScriptCore to be more conservative with its allocation strategy (at the cost of
	///     some performance).
	/// </remarks>
	public uint OverrideRamSize = 0;

	/// <summary>The minimum size of large VM heaps in JavaScriptCore.</summary>
	/// <remarks>Set this to a lower value to make these heaps start with a smaller initial value.</remarks>
	public uint MinLargeHeapSize = 32 * 1024 * 1024;

	/// <summary>The minimum size of small VM heaps in JavaScriptCore.</summary>
	/// <remarks>Set this to a lower value to make these heaps start with a smaller initial value.</remarks>
	public uint MinSmallHeapSize = 1 * 1024 * 1024;

	/// <summary>The number of threads to use in the Renderer (for parallel painting on the CPU, etc.).</summary>
	/// <remarks>
	///     If this value is 0 (the default), the number of threads will be determined at runtime using the following
	///     formula: <c>max(PhysicalProcessorCount() - 1, 1)</c>
	/// </remarks>
	public uint NumRendererThreads = 0;

	/// <summary>The max amount of time (in seconds) to allow <see cref="Renderer.Update()" /> to run per call.</summary>
	/// <remarks>The library will attempt to throttle timers and/or reschedule work if this time budget is exceeded.</remarks>
	public double MaxUpdateTime = 1.0 / 200.0;

	/// <summary>The alignment (in bytes) of the BitmapSurface when using the CPU renderer.</summary>
	/// <remarks>
	///     You can set this to '0' to perform no padding (row_bytes will always be width * Bpp) at a slight cost to
	///     performance.
	/// </remarks>
	public uint BitmapAlignment = 16;

	/// <summary>
	/// The quality of effects (blurs, CSS filters, SVG filters, etc.) to use when rendering.
	/// </summary>
	public EffectQuality EffectQuality = EffectQuality.Medium;

	public UlConfig()
	{
	}

	public readonly override bool Equals([NotNullWhen(true)] object? obj)
	{
		return obj is UlConfig config && Equals(config);
	}

	public static bool operator ==(UlConfig left, UlConfig right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(UlConfig left, UlConfig right)
	{
		return !(left == right);
	}

	[CustomMarshaller(typeof(UlConfig), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
	internal struct Marshaller
	{
		public UlString CachePath;
		public UlString ResourcePathPrefix;

		public byte FaceWinding;

		public byte FontHinting;
		public double FontGamma;

		public UlString UserStylesheet;

		public byte ForceRepaint;

		public double AnimationTimerDelay;
		public double ScrollTimerDelay;
		public double RecycleDelay;

		public uint MemoryCacheSize;
		public uint PageCacheSize;
		public uint OverrideRamSize;
		public uint MinLargeHeapSize;
		public uint MinSmallHeapSize;

		public uint NumRendererThreads;

		public double MaxUpdateTime;

		public uint BitmapAlignment;
		public byte EffectQuality;

		public void FromManaged(UlConfig config)
		{
			CachePath = new UlString(config.CachePath.AsSpan());
			ResourcePathPrefix = new UlString(config.ResourcePathPrefix.AsSpan());
			FaceWinding = Methods.BitCast<FaceWinding, byte>(config.FaceWinding);
			FontHinting = Methods.BitCast<FontHinting, byte>(config.FontHinting);
			FontGamma = config.FontGamma;
			UserStylesheet = new UlString(config.UserStylesheet.AsSpan());
			ForceRepaint = Methods.BitCast<bool, byte>(config.ForceRepaint);
			AnimationTimerDelay = config.AnimationTimerDelay;
			ScrollTimerDelay = config.ScrollTimerDelay;
			RecycleDelay = config.RecycleDelay;
			MemoryCacheSize = config.MemoryCacheSize;
			PageCacheSize = config.PageCacheSize;
			OverrideRamSize = config.OverrideRamSize;
			MinLargeHeapSize = config.MinLargeHeapSize;
			MinSmallHeapSize = config.MinSmallHeapSize;
			NumRendererThreads = config.NumRendererThreads;
			MaxUpdateTime = config.MaxUpdateTime;
			BitmapAlignment = config.BitmapAlignment;
			EffectQuality = Methods.BitCast<EffectQuality, byte>(config.EffectQuality);
		}

		public readonly Marshaller ToUnmanaged()
		{
			return this;
		}

		public void Free()
		{
			CachePath.Dispose();
			ResourcePathPrefix.Dispose();
			UserStylesheet.Dispose();
		}
	}

	public readonly bool Equals(UlConfig other)
	{
		return CachePath == other.CachePath && ResourcePathPrefix == other.ResourcePathPrefix && FaceWinding == other.FaceWinding && FontHinting == other.FontHinting && FontGamma.Equals(other.FontGamma) && UserStylesheet == other.UserStylesheet && ForceRepaint == other.ForceRepaint && AnimationTimerDelay.Equals(other.AnimationTimerDelay) && ScrollTimerDelay.Equals(other.ScrollTimerDelay) && RecycleDelay.Equals(other.RecycleDelay) && MemoryCacheSize == other.MemoryCacheSize && PageCacheSize == other.PageCacheSize && OverrideRamSize == other.OverrideRamSize && MinLargeHeapSize == other.MinLargeHeapSize && MinSmallHeapSize == other.MinSmallHeapSize && NumRendererThreads == other.NumRendererThreads && MaxUpdateTime.Equals(other.MaxUpdateTime) && BitmapAlignment == other.BitmapAlignment && EffectQuality == other.EffectQuality;
	}

	public override int GetHashCode()
	{
		var hashCode = new HashCode();
		hashCode.Add(CachePath);
		hashCode.Add(ResourcePathPrefix);
		hashCode.Add((int)FaceWinding);
		hashCode.Add((int)FontHinting);
		hashCode.Add(FontGamma);
		hashCode.Add(UserStylesheet);
		hashCode.Add(ForceRepaint);
		hashCode.Add(AnimationTimerDelay);
		hashCode.Add(ScrollTimerDelay);
		hashCode.Add(RecycleDelay);
		hashCode.Add(MemoryCacheSize);
		hashCode.Add(PageCacheSize);
		hashCode.Add(OverrideRamSize);
		hashCode.Add(MinLargeHeapSize);
		hashCode.Add(MinSmallHeapSize);
		hashCode.Add(NumRendererThreads);
		hashCode.Add(MaxUpdateTime);
		hashCode.Add(BitmapAlignment);
		hashCode.Add((int)EffectQuality);
		return hashCode.ToHashCode();
	}
}
