using System.Runtime.InteropServices;

namespace Engine;

/// <summary>Registers the <see cref="Input"/> resource and wires up the platform input backend.</summary>
public sealed class InputPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.Input");

    public void Build(App app)
    {
        Logger.Info("InputPlugin: Registering Input resource...");
        app.World.InitResource<Input>();

        var input = app.World.Resource<Input>();

        if (app.World.TryGetResource<IInputBackend>(out var backend))
        {
            Logger.Info($"InputPlugin: Wiring input backend: {backend.GetType().Name}");
            backend.Initialize(app, input);
        }
        else
        {
            Logger.Warn("InputPlugin: No IInputBackend resource found — input events will not be forwarded.");
        }

        app.AddSystem(Stage.Last, new SystemDescriptor(static world =>
            {
                world.Resource<Input>().BeginFrame();
            }, "InputPlugin.BeginFrame")
            .Write<Input>());
        Logger.Info("InputPlugin: Input system registered to Last stage.");
    }
}

/// <summary>
/// Frame-based input state resource. Tracks keyboard, mouse, and text input.
/// <para>
/// Query methods (<c>KeyDown</c>, <c>MousePressed</c>, etc.) are public.
/// Mutation methods (<c>SetKey</c>, <c>SetMouseButton</c>, etc.) are <c>internal</c>
/// — only platform backends should call them.
/// </para>
/// </summary>
public sealed class Input
{
    private readonly HashSet<Key> _keysDown = [];
    private readonly HashSet<Key> _keysPressed = [];
    private readonly HashSet<Key> _keysReleased = [];

    private readonly HashSet<MouseButton> _mouseDown = [];
    private readonly HashSet<MouseButton> _mousePressed = [];
    private readonly HashSet<MouseButton> _mouseReleased = [];

    public int MouseX { get; private set; }
    public int MouseY { get; private set; }

    public (int X, int Y) MousePosition => (MouseX, MouseY);

    public int MouseDeltaX { get; private set; }
    public int MouseDeltaY { get; private set; }

    public (int X, int Y) MouseDelta => (MouseDeltaX, MouseDeltaY);

    public float WheelX { get; private set; }
    public float WheelY { get; private set; }

    private readonly List<char> _textInput = [];
    
    public ReadOnlySpan<char> TextInput => CollectionsMarshal.AsSpan(_textInput);

    public bool KeyDown(Key key) => _keysDown.Contains(key);
    public bool KeyPressed(Key key) => _keysPressed.Contains(key);
    public bool KeyReleased(Key key) => _keysReleased.Contains(key);
    public bool AnyKeyDown() => _keysDown.Count > 0;
    public bool AnyKeyPressed() => _keysPressed.Count > 0;

    public bool MouseDown(MouseButton button) => _mouseDown.Contains(button);
    public bool MousePressed(MouseButton button) => _mousePressed.Contains(button);
    public bool MouseReleased(MouseButton button) => _mouseReleased.Contains(button);
    public bool AnyMouseDown() => _mouseDown.Count > 0;
    public bool AnyMousePressed() => _mousePressed.Count > 0;

    public bool MouseDown(int button) => _mouseDown.Contains((MouseButton)button);
    public bool MousePressed(int button) => _mousePressed.Contains((MouseButton)button);
    public bool MouseReleased(int button) => _mouseReleased.Contains((MouseButton)button);

    // ── Mutation (internal — platform backends only) ───────────────────

    /// <summary>Clears per-frame transient state. Called once at the end of each frame.</summary>
    internal void BeginFrame()
    {
        _keysPressed.Clear();
        _keysReleased.Clear();
        _mousePressed.Clear();
        _mouseReleased.Clear();
        MouseDeltaX = 0;
        MouseDeltaY = 0;
        WheelX = 0;
        WheelY = 0;
        _textInput.Clear();
    }

    /// <summary>Updates the state of a keyboard key.</summary>
    internal void SetKey(Key key, bool isDown)
    {
        if (isDown)
        {
            if (_keysDown.Add(key)) _keysPressed.Add(key);
        }
        else
        {
            if (_keysDown.Remove(key)) _keysReleased.Add(key);
        }
    }

    /// <summary>Updates the state of a mouse button.</summary>
    internal void SetMouseButton(MouseButton button, bool isDown)
    {
        if (isDown)
        {
            if (_mouseDown.Add(button)) _mousePressed.Add(button);
        }
        else
        {
            if (_mouseDown.Remove(button)) _mouseReleased.Add(button);
        }
    }

    /// <summary>Updates the state of a mouse button by 0-based index.</summary>
    internal void SetMouseButton(int button, bool isDown) => 
        SetMouseButton((MouseButton)button, isDown);

    /// <summary>Sets the absolute mouse position in window pixels.</summary>
    internal void SetMousePosition(int x, int y)
    {
        MouseX = x;
        MouseY = y;
    }

    /// <summary>Accumulates relative mouse motion for this frame.</summary>
    internal void AddMouseDelta(int dx, int dy)
    {
        MouseDeltaX += dx;
        MouseDeltaY += dy;
    }

    /// <summary>Accumulates scroll wheel input for this frame.</summary>
    internal void AddWheel(float dx, float dy)
    {
        WheelX += dx;
        WheelY += dy;
    }

    /// <summary>Appends typed characters for this frame.</summary>
    internal void AddText(string text)
    {
        foreach (var c in text)
            _textInput.Add(c);
    }

    // ── Diagnostics ────────────────────────────────────────────────────

    /// <summary>Human-readable snapshot for debugging.</summary>
    public override string ToString() => 
        $"Input {{ Keys={_keysDown.Count} down, Mouse=({MouseX},{MouseY}), Buttons={_mouseDown.Count} down }}";
}

/// <summary>Mouse button identifiers (0-based, matching SDL button index minus 1).</summary>
public enum MouseButton
{
    /// <summary>Left mouse button.</summary>
    Left = 0,
    /// <summary>Middle mouse button (wheel click).</summary>
    Middle = 1,
    /// <summary>Right mouse button.</summary>
    Right = 2,
    /// <summary>Extra button 1 (side button).</summary>
    X1 = 3,
    /// <summary>Extra button 2 (side button).</summary>
    X2 = 4,
}

/// <summary>
/// Keyboard scan codes (USB HID usage values).
/// Values map directly to SDL scancodes for zero-cost conversion.
/// </summary>
public enum Key
{
    Unknown = 0,
    A = 4,
    B = 5,
    C = 6,
    D = 7,
    E = 8,
    F = 9,
    G = 10,
    H = 11,
    I = 12,
    J = 13,
    K = 14,
    L = 15,
    M = 16,
    N = 17,
    O = 18,
    P = 19,
    Q = 20,
    R = 21,
    S = 22,
    T = 23,
    U = 24,
    V = 25,
    W = 26,
    X = 27,
    Y = 28,
    Z = 29,
    Alpha1 = 30,
    Alpha2 = 31,
    Alpha3 = 32,
    Alpha4 = 33,
    Alpha5 = 34,
    Alpha6 = 35,
    Alpha7 = 36,
    Alpha8 = 37,
    Alpha9 = 38,
    Alpha0 = 39,
    Return = 40,
    Escape = 41,
    Backspace = 42,
    Tab = 43,
    Space = 44,
    Minus = 45,
    Equals = 46,
    Leftbracket = 47,
    Rightbracket = 48,

    /// <summary>
    /// Located at the lower left of the return key on ISO keyboards and at the right end
    /// of the QWERTY row on ANSI keyboards. Produces backslash / vertical line in US layout.
    /// </summary>
    Backslash = 49,

    /// <summary>
    /// ISO keyboards use this code instead of 49 for the same key. Most OSes treat both
    /// identically — prefer <see cref="Backslash"/> unless your keyboard generates both.
    /// </summary>
    NonUshash = 50,
    Semicolon = 51,
    Apostrophe = 52,

    /// <summary>
    /// Top left corner key. Produces grave accent / tilde in US layout,
    /// section sign on ISO keyboards, etc.
    /// </summary>
    Grave = 53,
    Comma = 54,
    Period = 55,
    Slash = 56,
    Capslock = 57,
    F1 = 58,
    F2 = 59,
    F3 = 60,
    F4 = 61,
    F5 = 62,
    F6 = 63,
    F7 = 64,
    F8 = 65,
    F9 = 66,
    F10 = 67,
    F11 = 68,
    F12 = 69,
    Printscreen = 70,
    Scrolllock = 71,
    Pause = 72,

    /// <summary>Insert on PC, Help on some Mac keyboards.</summary>
    Insert = 73,
    Home = 74,
    Pageup = 75,
    Delete = 76,
    End = 77,
    Pagedown = 78,
    Right = 79,
    Left = 80,
    Down = 81,
    Up = 82,

    /// <summary>Num Lock on PC, Clear on Mac keyboards.</summary>
    NumLockClear = 83,
    KpDivide = 84,
    KpMultiply = 85,
    KpMinus = 86,
    KpPlus = 87,
    KpEnter = 88,
    Kp1 = 89,
    Kp2 = 90,
    Kp3 = 91,
    Kp4 = 92,
    Kp5 = 93,
    Kp6 = 94,
    Kp7 = 95,
    Kp8 = 96,
    Kp9 = 97,
    Kp0 = 98,
    KpPeriod = 99,

    /// <summary>
    /// Additional key on ISO keyboards between left shift and Z.
    /// Produces backslash in US/UK layout, less-than sign in German/French layout.
    /// </summary>
    NonUsBackSlash = 100,

    /// <summary>Windows contextual menu / Compose key.</summary>
    Application = 101,

    /// <summary>Power key (status flag on USB spec, physical key on some Mac keyboards).</summary>
    Power = 102,
    KpEquals = 103,
    F13 = 104,
    F14 = 105,
    F15 = 106,
    F16 = 107,
    F17 = 108,
    F18 = 109,
    F19 = 110,
    F20 = 111,
    F21 = 112,
    F22 = 113,
    F23 = 114,
    F24 = 115,
    Execute = 116,

    /// <summary>AL Integrated Help Center.</summary>
    Help = 117,

    /// <summary>Show menu.</summary>
    Menu = 118,
    Select = 119,

    /// <summary>AC Stop.</summary>
    Stop = 120,

    /// <summary>AC Redo / Repeat.</summary>
    Again = 121,

    /// <summary>AC Undo.</summary>
    Undo = 122,

    /// <summary>AC Cut.</summary>
    Cut = 123,

    /// <summary>AC Copy.</summary>
    Copy = 124,

    /// <summary>AC Paste.</summary>
    Paste = 125,

    /// <summary>AC Find.</summary>
    Find = 126,
    Mute = 127,
    VolumeUp = 128,
    VolumeDown = 129,
    KpComma = 133,
    KpEqualsAs400 = 134,

    /// <summary>Used on Asian keyboards.</summary>
    International1 = 135,
    International2 = 136,

    /// <summary>Yen key.</summary>
    International3 = 137,
    International4 = 138,
    International5 = 139,
    International6 = 140,
    International7 = 141,
    International8 = 142,
    International9 = 143,

    /// <summary>Hangul / English toggle.</summary>
    Lang1 = 144,

    /// <summary>Hanja conversion.</summary>
    Lang2 = 145,

    /// <summary>Katakana.</summary>
    Lang3 = 146,

    /// <summary>Hiragana.</summary>
    Lang4 = 147,

    /// <summary>Zenkaku / Hankaku.</summary>
    Lang5 = 148,
    Lang6 = 149,
    Lang7 = 150,
    Lang8 = 151,
    Lang9 = 152,

    /// <summary>Erase-Eaze.</summary>
    AltErase = 153,
    SysReq = 154,

    /// <summary>AC Cancel.</summary>
    Cancel = 155,
    Clear = 156,
    Prior = 157,
    Return2 = 158,
    Separator = 159,
    Out = 160,
    Oper = 161,
    ClearAgain = 162,
    CrSel = 163,
    ExSel = 164,
    Kp00 = 176,
    Kp000 = 177,
    ThousandsSeparator = 178,
    DecimalSeparator = 179,
    CurrencyUnit = 180,
    CurrencySubunit = 181,
    KpLeftParen = 182,
    KpRightParen = 183,
    KpLeftBrace = 184,
    KpRightBrace = 185,
    KpTab = 186,
    KpBackspace = 187,
    KpA = 188,
    KpB = 189,
    KpC = 190,
    KpD = 191,
    KpE = 192,
    KpF = 193,
    KpXor = 194,
    KpPower = 195,
    KpPercent = 196,
    KpLess = 197,
    KpGreater = 198,
    KpAmpersand = 199,
    KpDblAmpersand = 200,
    KpVerticalBar = 201,
    KpDblVerticalBar = 202,
    KpColon = 203,
    KpHash = 204,
    KpSpace = 205,
    KpAt = 206,
    KpExClam = 207,
    KpMemStore = 208,
    KpMemRecall = 209,
    KpMemClear = 210,
    KpMemAdd = 211,
    KpMemSubtract = 212,
    KpMemMultiply = 213,
    KpMemDivide = 214,
    KpPlusMinus = 215,
    KpClear = 216,
    KpClearEntry = 217,
    KpBinary = 218,
    KpOctal = 219,
    KpDecimal = 220,
    KpHexadecimal = 221,
    LCtrl = 224,
    LShift = 225,

    /// <summary>Left Alt / Option.</summary>
    LAlt = 226,

    /// <summary>Left GUI — Windows / Command (Apple) / Meta.</summary>
    LGUI = 227,
    RCtrl = 228,
    RShift = 229,

    /// <summary>Right Alt / AltGr / Option.</summary>
    RAlt = 230,

    /// <summary>Right GUI — Windows / Command (Apple) / Meta.</summary>
    RGUI = 231,

    /// <summary>Mode key (SDL_KMOD_MODE).</summary>
    Mode = 257,

    /// <summary>Sleep.</summary>
    Sleep = 258,

    /// <summary>Wake.</summary>
    Wake = 259,

    /// <summary>Channel Increment.</summary>
    ChannelIncrement = 260,

    /// <summary>Channel Decrement.</summary>
    ChannelDecrement = 261,

    /// <summary>Media Play.</summary>
    MediaPlay = 262,

    /// <summary>Media Pause.</summary>
    MediaPause = 263,

    /// <summary>Media Record.</summary>
    MediaRecord = 264,

    /// <summary>Media Fast Forward.</summary>
    MediaFastForward = 265,

    /// <summary>Media Rewind.</summary>
    MediaRewind = 266,

    /// <summary>Media Next Track.</summary>
    MediaNextTrack = 267,

    /// <summary>Media Previous Track.</summary>
    MediaPreviousTrack = 268,

    /// <summary>Media Stop.</summary>
    MediaStop = 269,

    /// <summary>Media Eject.</summary>
    MediaEject = 270,

    /// <summary>Media Play / Pause toggle.</summary>
    MediaPlayPause = 271,

    /// <summary>Media Select.</summary>
    MediaSelect = 272,

    /// <summary>AC New.</summary>
    ACNew = 273,

    /// <summary>AC Open.</summary>
    ACOpen = 274,

    /// <summary>AC Close.</summary>
    ACClose = 275,

    /// <summary>AC Exit.</summary>
    ACExit = 276,

    /// <summary>AC Save.</summary>
    ACSave = 277,

    /// <summary>AC Print.</summary>
    ACPrint = 278,

    /// <summary>AC Properties.</summary>
    ACProperties = 279,

    /// <summary>AC Search.</summary>
    ACSearch = 280,

    /// <summary>AC Home.</summary>
    ACHome = 281,

    /// <summary>AC Back.</summary>
    ACBack = 282,

    /// <summary>AC Forward.</summary>
    ACForward = 283,

    /// <summary>AC Stop.</summary>
    ACStop = 284,

    /// <summary>AC Refresh.</summary>
    ACRefresh = 285,

    /// <summary>AC Bookmarks.</summary>
    ACBookmarks = 286,

    /// <summary>Soft-key left (phones).</summary>
    SoftLeft = 287,

    /// <summary>Soft-key right (phones).</summary>
    SoftRight = 288,

    /// <summary>Accept phone call.</summary>
    Call = 289,

    /// <summary>Reject phone call.</summary>
    EndCall = 290,

    /// <summary>400–500 reserved for dynamic keycodes.</summary>
    Reserved = 400,

    /// <summary>Not a key — marks the scancode array upper bound.</summary>
    Count = 512,
}

