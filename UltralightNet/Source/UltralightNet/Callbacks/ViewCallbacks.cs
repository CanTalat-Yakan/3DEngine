using UltralightNet.Enums;
using UltralightNet.Structs;

namespace UltralightNet.Callbacks;

/// <summary>
/// Callback for when a message is added to the console (useful for JavaScript / network errors
/// and debugging).
/// </summary>
public delegate void AddConsoleMessageCallback(
	MessageSource source,
	MessageLevel level,
	string message,
	uint lineNumber,
	uint columnNumber,
	string sourceId
);

/// <summary>
/// Callback for when the page wants to create a new View.
///
/// This is usually the result of a user clicking a link with target="_blank" or by JavaScript
/// calling window.open(url).
///
/// To allow creation of these new Views, you should create a new View in this callback, resize it
/// to your container, and return it. You are responsible for displaying the returned View.
///
/// You should return NULL if you want to block the action.
/// </summary>
public delegate View? CreateChildViewCallback(
	string openerUrl,
	string targetUrl,
	UlIntRect? popupRect
);

/// <summary>
/// Callback for when the page wants to create a new View to display the local inspector in.
///
/// You should create a new View in this callback, resize it to your
/// container, and return it. You are responsible for displaying the returned View.
/// </summary>
public delegate View? CreateInspectorViewCallback(
	bool isLocal,
	string inspectedUrl
);

/// <summary>
/// Callback for when the page begins loading a new URL into a frame.
/// </summary>
public delegate void BeginLoadingCallback(
	ulong frameId,
	bool isMainFrame,
	string url
);

/// <summary>
/// Callback for when the page finishes loading a URL into a frame.
/// </summary>
public delegate void FinishLoadingCallback(
	ulong frameId,
	bool isMainFrame,
	string url
);

/// <summary>
/// Callback for when an error occurs while loading a URL into a frame.
/// </summary>
public delegate void FailLoadingCallback(
	ulong frameId,
	bool isMainFrame,
	string url,
	string description,
	string errorDomain,
	int errorCode
);

/// <summary>
/// Callback for when the JavaScript window object is reset for a new page load.
///
/// This is called before any scripts are executed on the page and is the earliest time to set up any
/// initial JavaScript state or bindings.
///
/// The document is not guaranteed to be loaded/parsed at this point. If you need to make any
/// JavaScript calls that are dependent on DOM elements or scripts on the page, use DOMReady
/// instead.
///
/// The window object is lazily initialized (this will not be called on pages with no scripts).
///
/// </summary>
public delegate void WindowObjectReadyCallback(
	ulong frameId,
	bool isMainFrame,
	string url
);

/// <summary>
/// Callback for when all JavaScript has been parsed and the document is ready.
///
/// This is the best time to make any JavaScript calls that are dependent on DOM elements or scripts
/// on the page.
///
/// </summary>
public delegate void DomReadyCallback(
	ulong frameId,
	bool isMainFrame,
	string url
);
