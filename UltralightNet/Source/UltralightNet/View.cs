using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using UltralightNet.Callbacks;
using UltralightNet.Enums;
using UltralightNet.JavaScript;
using UltralightNet.Platform;
using UltralightNet.Platform.HighPerformance;
using UltralightNet.Structs;

namespace UltralightNet;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class Methods
{
	[LibraryImport(LibUltralight)]
	public static partial void* ulCreateView(Renderer renderer, uint width, uint height, in ViewConfig viewConfig,
		Session session);

	[LibraryImport(LibUltralight)]
	public static partial void ulDestroyView(View view);

	[LibraryImport(LibUltralight)]
	[return: MarshalUsing(typeof(UlString))]
	public static partial string ulViewGetURL(View view);

	[LibraryImport(LibUltralight)]
	[return: MarshalUsing(typeof(UlString))]
	public static partial string ulViewGetTitle(View view);

	[LibraryImport(LibUltralight)]
	public static partial uint ulViewGetWidth(View view);

	[LibraryImport(LibUltralight)]
	public static partial uint ulViewGetHeight(View view);

	[LibraryImport(LibUltralight)]
	public static partial uint ulViewGetDisplayId(View view);

	[LibraryImport(LibUltralight)]
	public static partial void ulViewSetDisplayId(View view, uint displayId);

	[LibraryImport(LibUltralight)]
	public static partial double ulViewGetDeviceScale(View view);

	[LibraryImport(LibUltralight)]
	public static partial void ulViewSetDeviceScale(View view, double scale);

	[LibraryImport(LibUltralight)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool ulViewIsAccelerated(View view);

	[LibraryImport(LibUltralight)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool ulViewIsTransparent(View view);

	[LibraryImport(LibUltralight)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool ulViewIsLoading(View view);

	[LibraryImport(LibUltralight)]
	public static partial RenderTarget ulViewGetRenderTarget(View view);

	[LibraryImport(LibUltralight)]
	public static partial nuint ulViewGetSurface(View view);

	[LibraryImport(LibUltralight)]
	public static partial void ulViewLoadHTML(View view, [MarshalUsing(typeof(UlString))] string html_string);

	[LibraryImport(LibUltralight)]
	public static partial void ulViewLoadURL(View view, [MarshalUsing(typeof(UlString))] string url_string);

	[LibraryImport(LibUltralight)]
	public static partial void ulViewResize(View view, uint width, uint height);

	[LibraryImport(LibUltralight)]
	public static partial JsContextRef ulViewLockJSContext(View view);

	[LibraryImport(LibUltralight)]
	public static partial void ulViewUnlockJSContext(View view);

	[LibraryImport(LibUltralight)]
	[return: MarshalUsing(typeof(UlString))]
	public static partial string ulViewEvaluateScript(View view, [MarshalUsing(typeof(UlString))] string js,
		[MarshalUsing(typeof(UlString))] out string exception);

	/// <summary>Check if you can navigate backwards in history.</summary>
	[LibraryImport(LibUltralight)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool ulViewCanGoBack(View view);

	/// <summary>Check if you can navigate forwards in history.</summary>
	[LibraryImport(LibUltralight)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool ulViewCanGoForward(View view);

	/// <summary>Navigate backwards in history.</summary>
	[LibraryImport(LibUltralight)]
	public static partial void ulViewGoBack(View view);

	/// <summary>Navigate forwards in history.</summary>
	[LibraryImport(LibUltralight)]
	public static partial void ulViewGoForward(View view);

	/// <summary>Navigate to arbitrary offset in history.</summary>
	[LibraryImport(LibUltralight)]
	public static partial void ulViewGoToHistoryOffset(View view, int offset);

	/// <summary>Reload current page.</summary>
	[LibraryImport(LibUltralight)]
	public static partial void ulViewReload(View view);

	/// <summary>Stop all page loads.</summary>
	[LibraryImport(LibUltralight)]
	public static partial void ulViewStop(View view);

	/// <summary>Give focus to the View.</summary>
	/// <remarks>
	///     You should call this to give visual indication that the View has input
	///     focus (changes active text selection colors, for example).
	/// </remarks>
	[LibraryImport(LibUltralight)]
	public static partial void ulViewFocus(View view);

	/// <summary>Remove focus from the View and unfocus any focused input elements.</summary>
	/// <remarks>
	///     You should call this to give visual indication that the View has lost
	///     input focus.
	/// </remarks>
	[LibraryImport(LibUltralight)]
	public static partial void ulViewUnfocus(View view);

	/// <summary>Whether the View has focus.</summary>
	[LibraryImport(LibUltralight)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool ulViewHasFocus(View view);

	/// <summary>Whether the View has an input element with visible keyboard focus (indicated by a blinking caret).</summary>
	/// <remarks>
	///     You can use this to decide whether the View should consume
	///     keyboard input events (useful in games with mixed UI and key handling).
	/// </remarks>
	[LibraryImport(LibUltralight)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool ulViewHasInputFocus(View view);

	[LibraryImport(LibUltralight)]
	public static partial void ulViewFireKeyEvent(View view, KeyEvent keyEvent);

	[LibraryImport(LibUltralight)]
	public static partial void ulViewFireMouseEvent(View view, MouseEvent* mouseEvent);

	[LibraryImport(LibUltralight)]
	public static partial void ulViewFireScrollEvent(View view, ScrollEvent* scrollEvent);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetChangeTitleCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, UlString*, void> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetChangeURLCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, UlString*, void> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetChangeTooltipCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, UlString*, void> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetChangeCursorCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, Cursor, void> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetAddConsoleMessageCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, MessageSource, MessageLevel, UlString*, uint, uint, UlString*, void
			> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetCreateChildViewCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, UlString*, UlString*, bool, UlIntRect, void*> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetCreateChildViewCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, UlString*, UlString*, byte, UlIntRect, void*> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetCreateInspectorViewCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, byte, UlString*, void*> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetBeginLoadingCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, ulong, bool, UlString*, void> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetBeginLoadingCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, ulong, byte, UlString*, void> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetFinishLoadingCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, ulong, bool, UlString*, void> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetFinishLoadingCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, ulong, byte, UlString*, void> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetFailLoadingCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, ulong, bool, UlString*, UlString*, UlString*, int, void> callback,
		nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetFailLoadingCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, ulong, byte, UlString*, UlString*, UlString*, int, void> callback,
		nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetWindowObjectReadyCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, ulong, bool, UlString*, void> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetWindowObjectReadyCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, ulong, byte, UlString*, void> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetDOMReadyCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, ulong, bool, UlString*, void> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetDOMReadyCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, ulong, byte, UlString*, void> callback, nuint id);

	[LibraryImport(LibUltralight)]
	internal static partial void ulViewSetUpdateHistoryCallback(View view,
		delegate* unmanaged[Cdecl]<nuint, void*, void> callback, nuint id);

	[LibraryImport(LibUltralight)]
	public static partial void ulViewSetNeedsPaint(View view, [MarshalAs(UnmanagedType.U1)] bool needs_paint);

	[LibraryImport(LibUltralight)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool ulViewGetNeedsPaint(View view);

	[LibraryImport(LibUltralight)]
	public static partial void ulViewCreateLocalInspectorView(View view);
}

/// <summary>
///     Web-page container rendered to an offscreen surface.
///     The View class is responsible for loading and rendering web-pages to an offscreen surface. It
///     is completely isolated from the OS windowing system, you must forward all input events to it
///     from your application.
///     <br /><br />
///     Creating a View
///     <br />
///     You can create a View using <see cref="Renderer.CreateView"> Renderer::CreateView</see>.
///     <code>
///   // Create a ViewConfig with the desired settings
///   var viewConfig = new ViewConfig();
///
///   // Create a View, 500 by 500 pixels in size, using the default Session
///   var view = renderer.CreateView(500, 500, viewConfig, null);
///  </code>
///     <br />
///     Loading Content into a View
///     <br />
///     You can load content asynchronously into a View by setting <see cref="View.Url">View.Url</see>.
///     <code>
///   // Load a URL into the View
///   view.LoadURL("https://en.wikipedia.org/wiki/Main_Page");
///  </code>
///     Local File URLs
///     <br />
///     Local file URLs (eg, <c>file:///page.html</c>) will be loaded via FileSystem. You can provide your
///     own FileSystem implementation so these files can be loaded from your application's resources.
/// </summary>
/// <br />
/// <br />
/// Displaying Views in Your Application
/// <br />
/// Views are rendered either to a pixel-buffer (View::surface) or a GPU texture
/// (View::render_target) depending on whether CPU or GPU rendering is used (see
/// ViewConfig::is_accelerated).
/// <br />
/// You can use the Surface or RenderTarget to display the View in your application.
/// <code>
///   // Get the Surface for the View (assuming CPU rendering)
///   var surface = view.Surface;
///
///   // Check if the Surface is dirty (pixels have changed)
///   if (!surface.DirtyBounds.IsEmpty) {
///      // Get the underlying Bitmap.
///      var bitmap = surface.Bitmap;
///
///      // Use the bitmap pixels here...
///
///      // Clear the dirty bounds after you're done displaying the pixels
///      surface.ClearDirtyBounds();
///   }
///  </code>
/// <br />
/// Input Events
/// <br />
/// You must forward all input events to the View from your application. This includes keyboard,
/// mouse, and scroll events.
/// <code>
///   // Forward a mouse-move event to the View
///   var evt = new MouseEvent();
///   evt.Type = MouseEventType.MouseMoved;
///   evt.X = 100;
///   evt.Y = 100;
///   evt.Button = MouseEventButton.None;
///   view.FireMouseEvent(evt);
///  </code>
/// <note>
///     When using App::Create, the library will automatically create a View for you when you
///     call Overlay::Create.
///     <br /><br />
///     The View API is not thread-safe, all calls must be made on the same thread that the
///     Renderer or App was created on.
/// </note>
[NativeMarshalling(typeof(Marshaller))]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
public sealed unsafe class View : NativeContainer
{
	/// <summary>
	/// Set callback for when the page wants to create a new View.
	///
	/// This is usually the result of a user clicking a link with target="_blank" or by JavaScript
	/// calling window.open(url).
	///
	/// To allow creation of these new Views, you should create a new View in this callback, resize it
	/// to your container, and return it. You are responsible for displaying the returned View.
	///
	/// You should return NULL if you want to block the action.
	/// </summary>
	public CreateChildViewCallback? OnCreateChildView;

	/// <summary>
	/// Called when the page wants to create a new View to display the local inspector in.
	///
	/// You should create a new View in this callback (eg, Renderer::CreateView()), resize it to your
	/// container, and return it. You are responsible for displaying the returned View.
	/// </summary>
	/// <returns>
	/// Returns a View to use to satisfy the request (or return
	///          null if you want to block the action).
	/// </returns>
	public CreateInspectorViewCallback? OnCreateInspectorView;

	protected override void* Handle
	{
		get
		{
			Renderer?.AssertNotWrongThread();
			return base.Handle;
		}
	}

	internal Renderer? Renderer { get; set; }

	/// <summary>
	/// Get and set the URL of the View.
	/// </summary>
	public string Url
	{
		get => Methods.ulViewGetURL(this);
		set => Methods.ulViewLoadURL(this, value);
	}

	/// <summary>
	/// Load a raw string of HTML, the View will navigate to it as a new page.
	/// </summary>
	/// <param name="html">The raw HTML string to load.</param>
	public void LoadHtml(string html) => Methods.ulViewLoadHTML(this, html);

	/// <summary>
	/// Get the title of the current page loaded into this View, if any.
	/// </summary>
	public string Title => Methods.ulViewGetTitle(this);

	/// <summary>
	/// Get the width of the View, in pixels.
	/// </summary>
	public uint Width => Methods.ulViewGetWidth(this);

	/// <summary>
	///  Get the height of the View, in pixels.
	/// </summary>
	public uint Height => Methods.ulViewGetHeight(this);

	/// <summary>
	/// Get or set the device scale, i.e. the amount to scale page units to screen pixels.
	///
	/// For example, a value of 1.0 is equivalent to 100% zoom. A value of 2.0 is 200% zoom.
	/// </summary>
	public double DeviceScale
	{
		get => Methods.ulViewGetDeviceScale(this);
		set => Methods.ulViewSetDeviceScale(this, value);
	}

	/// <summary>
	/// Get or set the display id of the View.
	/// </summary>
	/// <seealso cref="ViewConfig.DisplayId">ViewConfig.DisplayId</seealso>
	public uint DisplayId
	{
		get => Methods.ulViewGetDisplayId(this);
		set => Methods.ulViewSetDisplayId(this, value);
	}

	/// <summary>
	/// Whether the View is GPU-accelerated. If this is false, the page will be rendered
	/// via the CPU renderer.
	/// </summary>
	public bool IsAccelerated => Methods.ulViewIsAccelerated(this);

	/// <summary>
	/// Whether the View supports transparent backgrounds.
	/// </summary>
	public bool IsTransparent => Methods.ulViewIsTransparent(this);

	/// <summary>
	/// Check if the main frame of the page is currently loading.
	/// </summary>
	public bool IsLoading => Methods.ulViewIsLoading(this);

	/// <summary>
	///     Get the RenderTarget for the View.
	/// </summary>
	/// <remarks>Only valid if this View is using the GPU renderer (see <see cref="ViewConfig.IsAccelerated"/>).</remarks>
	/// <note>
	/// You can use this with your <see cref="GpuDriver"/> implementation to bind and display the
	/// corresponding texture in your application.
	/// </note>
	public RenderTarget RenderTarget
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Methods.ulViewGetRenderTarget(this);
	}

	/// <summary>
	/// Get the Surface for the View (native pixel buffer that the CPU renderer draws into).
	/// </summary>
	/// <remarks>
	/// This operation is only valid if the View is using the CPU renderer, (eg, it is
	/// <b>not</b> GPU accelerated, see <see cref="ViewConfig.IsAccelerated"/>). This function will
	/// return null if the View is using the GPU renderer.
	/// </remarks>
	/// <note>
	/// The default Surface is BitmapSurface, but you can provide your own Surface
	/// implementation via <see cref="UltralightNet.Platform.Platform.SurfaceDefinition"/> Platform::set_surface_factory().
	/// </note>
	public Surface? Surface
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			UIntPtr surfaceHandle = Methods.ulViewGetSurface(this);
			if (surfaceHandle is 0) return null;
			return UltralightNet.Surface.FromHandle(surfaceHandle);
		}
	}

	/// <summary>
	/// Whether we can navigate backwards in history
	/// </summary>
	public bool CanGoBack => Methods.ulViewCanGoBack(this);

	/// <summary>
	/// Whether we can navigate forwards in history
	/// </summary>
	public bool CanGoForward => Methods.ulViewCanGoForward(this);

	/// <summary>
	/// Whether the View has focus.
	/// </summary>
	public bool HasFocus => Methods.ulViewHasFocus(this);

	/// <summary>
	/// Whether the View has an input element with visible keyboard focus (indicated by a
	/// blinking caret).
	///
	/// You can use this to decide whether the View should consume keyboard input events
	/// (useful in games with mixed UI and key handling).
	/// </summary>
	public bool HasInputFocus => Methods.ulViewHasInputFocus(this);

	/// <summary>
	/// Get or set whether this View should be repainted during the next call to Renderer::Render
	/// </summary>
	/// <note>
	/// This flag is automatically set whenever the page content changes, but you can set it
	/// directly in case you need to force a repaint.
	/// </note>
	public bool NeedsPaint
	{
		get => Methods.ulViewGetNeedsPaint(this);
		set => Methods.ulViewSetNeedsPaint(this, value);
	}

	/// <summary>
	/// Resize View to a certain size.
	/// </summary>
	/// <param name="width">The initial width, in pixels.</param>
	/// <param name="height">The initial height, in pixels.</param>
	public void Resize(in uint width, in uint height)
	{
		Methods.ulViewResize(this, width, height);
	}

	/// <summary>
	/// Acquire the page's JSContext for use with the JavaScriptCore API
	/// </summary>
	/// <returns>The view's JSContext</returns>
	/// <note>
	/// You can use the underlying JSContextRef with the JavaScriptCore C API. This allows you
	/// to marshall C# objects to/from JavaScript, bind callbacks, and call JS functions
	/// directly.
	/// <br/><br/>
	/// The JSContextRef gets reset after each page navigation. You should initialize your
	/// JavaScript state within the OnWindowObjectReady and OnDomReady events.
	/// <br/><br/>
	/// This call locks the internal context for the current thread. It will be unlocked when
	/// the returned JSContext's ref-count goes to zero. The lock is recursive, you can call
	/// this multiple times.
	/// </note>
	public JsContextRef LockJsContext()
	{
		return Methods.ulViewLockJSContext(this);
	}

	/// <summary>
	/// Unlock the JsContext
	/// </summary>
	public void UnlockJsContext()
	{
		Methods.ulViewUnlockJSContext(this);
	}

	/// <summary>
	/// Helper function to evaluate a raw string of JavaScript and return the result as a String.
	/// </summary>
	/// <param name="jsString">A string of JavaScript to evaluate in the main frame.</param>
	/// <param name="exception">A string to store the exception in, if any. Pass null if you don't care about exceptions.</param>
	/// <returns>Returns the JavaScript result typecast to a String.</returns>
	/// <note>
	/// You do not need to lock the JS context, it is done automatically.
	/// <br/>
	/// If you need lower-level access to native JavaScript values, you should instead lock
	/// the JS context and call <see cref="JsBase.EvaluateScript"/> in the JavaScriptCore API.
	/// </note>
	public string EvaluateScript(string jsString, out string exception)
	{
		return Methods.ulViewEvaluateScript(this, jsString, out exception);
	}

	/// <summary>
	/// Navigate backwards in history
	/// </summary>
	public void GoBack()
	{
		Methods.ulViewGoBack(this);
	}

	/// <summary>
	/// Navigate forwards in history
	/// </summary>
	public void GoForward()
	{
		Methods.ulViewGoForward(this);
	}

	/// <summary>
	/// Navigate to an arbitrary offset in history
	/// </summary>
	/// <param name="offset"></param>
	public void GoToHistoryOffset(in int offset)
	{
		Methods.ulViewGoToHistoryOffset(this, offset);
	}

	/// <summary>
	/// Reload current page
	/// </summary>
	public void Reload()
	{
		Methods.ulViewReload(this);
	}

	/// <summary>
	///  Stop all page loads
	/// </summary>
	public void Stop()
	{
		Methods.ulViewStop(this);
	}

	/// <summary>
	/// Give focus to the View.
	///
	/// You should call this to give visual indication that the View has input focus (changes active
	/// text selection colors, for example).
	/// </summary>
	public void Focus()
	{
		Methods.ulViewFocus(this);
	}

	/// <summary>
	/// Remove focus from the View and unfocus any focused input elements.
	///
	/// You should call this to give visual indication that the View has lost input focus.
	/// </summary>
	public void Unfocus()
	{
		Methods.ulViewUnfocus(this);
	}

	/// <summary>
	/// Fire a keyboard event.
	/// </summary>
	/// <param name="keyEvent"></param>
	public void FireKeyEvent(KeyEvent keyEvent)
	{
		Methods.ulViewFireKeyEvent(this, keyEvent);
	}

	/// <summary>
	/// Fire a mouse event.
	/// </summary>
	/// <param name="mouseEvent"></param>
	public void FireMouseEvent(MouseEvent mouseEvent)
	{
		Methods.ulViewFireMouseEvent(this, &mouseEvent);
	}

	/// <summary>
	/// Fire a scroll event.
	/// </summary>
	/// <param name="scrollEvent"></param>
	public void FireScrollEvent(ScrollEvent scrollEvent)
	{
		Methods.ulViewFireScrollEvent(this, &scrollEvent);
	}

	/// <summary>
	/// Create an Inspector View to inspect / debug this View locally.
	///
	/// This will only succeed if you have the inspector assets in your filesystem-- the inspector
	/// will look for file:///inspector/Main.html when it first loads.
	///
	/// You must handle ViewListener::OnCreateInspectorView so that the library has a View to display
	/// the inspector in. This function will call this event only if an inspector view is not
	/// currently active.
	/// </summary>
	public void CreateLocalInspectorView() => Methods.ulViewCreateLocalInspectorView(this);

	/// <summary>
	/// Set callback for when the page title changes.
	/// </summary>
	public event Action<string>? OnChangeTitle;

	/// <summary>
	///  Set callback for when the page URL changes.
	/// </summary>
	public event Action<string>? OnChangeUrl;

	/// <summary>
	/// Set callback for when the tooltip changes (usually result of a mouse hover).
	/// </summary>
	public event Action<string>? OnChangeTooltip;

	/// <summary>
	/// Set callback for when the mouse cursor changes.
	/// </summary>
	public event Action<Cursor>? OnChangeCursor;

	/// <summary>
	/// Set callback for when a message is added to the console (useful for JavaScript / network errors
	/// and debugging).
	/// </summary>
	public event AddConsoleMessageCallback? OnAddConsoleMessage;

	/// <summary>
	/// Set callback for when the page begins loading a new URL into a frame.
	/// </summary>
	public event BeginLoadingCallback? OnBeginLoading;

	/// <summary>
	/// Set callback for when the page finishes loading a URL into a frame.
	/// </summary>
	public event FinishLoadingCallback? OnFinishLoading;

	/// <summary>
	/// Set callback for when an error occurs while loading a URL into a frame.
	/// </summary>
	public event FailLoadingCallback? OnFailLoading;

	/// <summary>
	/// Set callback for when the JavaScript window object is reset for a new page load.
	///
	/// This is called before any scripts are executed on the page and is the earliest time to set up any
	/// initial JavaScript state or bindings.
	///
	/// The document is not guaranteed to be loaded/parsed at this point. If you need to make any
	/// JavaScript calls that are dependent on DOM elements or scripts on the page, use DOMReady
	/// instead.
	///
	/// The window object is lazily initialized (this will not be called on pages with no scripts).
	/// </summary>
	public event WindowObjectReadyCallback? OnWindowObjectReady;

	/// <summary>
	/// Set callback for when all JavaScript has been parsed and the document is ready.
	///
	/// This is the best time to make any JavaScript calls that are dependent on DOM elements or scripts
	/// on the page.
	/// </summary>
	public event DomReadyCallback? OnDomReady;

	/// <summary>
	/// Set callback for when the history (back/forward state) is modified.
	/// </summary>
	public event Action? OnUpdateHistory;

	internal void SetUpCallbacks()
	{
		UIntPtr data = Renderer!.GetCallbackData();
		Methods.ulViewSetChangeTitleCallback(this, &NativeOnChangeTitle, data);
		Methods.ulViewSetChangeURLCallback(this, &NativeOnChangeURL, data);
		Methods.ulViewSetChangeTooltipCallback(this, &NativeOnChangeTooltip, data);
		Methods.ulViewSetChangeCursorCallback(this, &NativeOnChangeCursor, data);
		Methods.ulViewSetAddConsoleMessageCallback(this, &NativeOnAddConsoleMessage, data);
		Methods.ulViewSetCreateChildViewCallback(this, &NativeOnCreateChildView, data);
		Methods.ulViewSetCreateInspectorViewCallback(this, &NativeOnCreateInspectorView, data);
		Methods.ulViewSetBeginLoadingCallback(this, &NativeOnBeginLoading, data);
		Methods.ulViewSetFinishLoadingCallback(this, &NativeOnFinishLoading, data);
		Methods.ulViewSetFailLoadingCallback(this, &NativeOnFailLoading, data);
		Methods.ulViewSetWindowObjectReadyCallback(this, &NativeOnWindowObjectReady, data);
		Methods.ulViewSetDOMReadyCallback(this, &NativeOnDOMReady, data);
		Methods.ulViewSetUpdateHistoryCallback(this, &NativeOnUpdateHistory, data);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void NativeOnChangeTitle(nuint userData, void* caller, UlString* title)
	{
		GetView(userData, caller).OnChangeTitle?.Invoke(title->ToString());
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void NativeOnChangeURL(nuint userData, void* caller, UlString* url)
	{
		GetView(userData, caller).OnChangeUrl?.Invoke(url->ToString());
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void NativeOnChangeTooltip(nuint userData, void* caller, UlString* tooltip)
	{
		GetView(userData, caller).OnChangeTooltip?.Invoke(tooltip->ToString());
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void NativeOnChangeCursor(nuint userData, void* caller, Cursor cursor)
	{
		GetView(userData, caller).OnChangeCursor?.Invoke(cursor);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void NativeOnAddConsoleMessage(nuint userData, void* caller, MessageSource source,
		MessageLevel level, UlString* message, uint lineNumber, uint columnNumber, UlString* sourceId)
	{
		GetView(userData, caller).OnAddConsoleMessage?.Invoke(source, level, message->ToString(), lineNumber,
			columnNumber, sourceId->ToString());
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void* NativeOnCreateChildView(nuint userData, void* caller, UlString* openerUrl, UlString* targetUrl,
		byte isPopup, UlIntRect popupRect)
	{
		#if DEBUG
		throw new NotImplementedException("NativeOnCreateChildView");
		#else
		var view = GetView(userData, caller).OnCreateChildView
			?.Invoke(openerUrl->ToString(), targetUrl->ToString(), isPopup != 0 ? popupRect : null);
		return view is null ? null : view.Handle;
		#endif
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void* NativeOnCreateInspectorView(nuint userData, void* caller, byte isLocal, UlString* inspectedUrl)
	{
		var view = GetView(userData, caller).OnCreateInspectorView?.Invoke(isLocal != 0, inspectedUrl->ToString());
		return view is null ? null : view.Handle;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void NativeOnBeginLoading(nuint userData, void* caller, ulong frameId, byte isMainFrame,
		UlString* url)
	{
		GetView(userData, caller).OnBeginLoading?.Invoke(frameId, isMainFrame != 0, url->ToString());
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void NativeOnFinishLoading(nuint userData, void* caller, ulong frameId, byte isMainFrame,
		UlString* url)
	{
		GetView(userData, caller).OnFinishLoading?.Invoke(frameId, isMainFrame != 0, url->ToString());
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void NativeOnFailLoading(nuint userData, void* caller, ulong frameId, byte isMainFrame,
		UlString* url, UlString* description, UlString* errorDomain, int errorCode)
	{
		GetView(userData, caller).OnFailLoading?.Invoke(frameId, isMainFrame != 0, url->ToString(),
			description->ToString(), errorDomain->ToString(), errorCode);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void NativeOnWindowObjectReady(nuint userData, void* caller, ulong frameId, byte isMainFrame,
		UlString* url)
	{
		GetView(userData, caller).OnWindowObjectReady?.Invoke(frameId, isMainFrame != 0, url->ToString());
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void NativeOnDOMReady(nuint userData, void* caller, ulong frameId, byte isMainFrame, UlString* url)
	{
		GetView(userData, caller).OnDomReady?.Invoke(frameId, isMainFrame != 0, url->ToString());
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void NativeOnUpdateHistory(nuint userData, void* caller)
	{
		GetView(userData, caller).OnUpdateHistory?.Invoke();
	}


	public override void Dispose()
	{
		if (!IsDisposed && Owns) Methods.ulDestroyView(this);
		GC.KeepAlive(Renderer);
		base.Dispose();
	}

	internal static View FromHandle(void* handle, bool dispose = true)
	{
		return new View { Handle = handle, Owns = dispose };
	}

	private static View GetView(nuint userData, void* caller)
	{
		if (Renderer.Renderers[userData].TryGetTarget(out var renderer))
		{
			if (renderer.Views[(nuint)caller].TryGetTarget(out var view)) return view;

			throw new ObjectDisposedException(nameof(View));
		}

		throw new ObjectDisposedException(nameof(UltralightNet.Renderer));
	}

	internal nuint GetUserData()
	{
		return (nuint)Handle;
	}

	[CustomMarshaller(typeof(View), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
	internal ref struct Marshaller
	{
		private View _view;

		public void FromManaged(View view)
		{
			_view = view;
		}

		public readonly void* ToUnmanaged()
		{
			return _view.Handle;
		}

		public readonly void Free()
		{
			GC.KeepAlive(_view);
		}
	}
}
