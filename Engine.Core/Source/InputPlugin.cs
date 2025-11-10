using System.Runtime.InteropServices;

namespace Engine;

// Core input abstractions: independent of SDL. Window backends push events via an implementation of IInputBackend.

public interface IInputBackend
{
    void Initialize(App app, Input input);
}

public sealed class InputPlugin : IPlugin
{
    public void Build(App app)
    {
        if (!app.World.ContainsResource<Input>())
            app.World.InsertResource(new Input());
        var input = app.World.Resource<Input>();
        // Allow already-inserted backend (e.g., SDL) to initialize now.
        if (app.World.TryResource<IInputBackend>() is { } backend)
            backend.Initialize(app, input);

        // Frame begin housekeeping (clear transient state)
        app.AddSystem(Stage.First, (World world) => world.Resource<Input>().BeginFrame());
    }
}

public sealed class Input
{
    private readonly HashSet<Key> _down = new();
    private readonly HashSet<Key> _pressed = new();
    private readonly HashSet<Key> _released = new();

    private readonly HashSet<int> _mouseDown = new();
    private readonly HashSet<int> _mousePressed = new();
    private readonly HashSet<int> _mouseReleased = new();

    public int MouseX { get; private set; }
    public int MouseY { get; private set; }
    public float WheelX { get; private set; }
    public float WheelY { get; private set; }

    private readonly List<char> _textInput = new();

    public void BeginFrame()
    {
        _pressed.Clear();
        _released.Clear();
        _mousePressed.Clear();
        _mouseReleased.Clear();
        WheelX = 0; WheelY = 0;
        _textInput.Clear();
    }

    public void SetKey(Key key, bool isDown)
    {
        if (isDown)
        {
            if (_down.Add(key)) _pressed.Add(key);
        }
        else
        {
            if (_down.Remove(key)) _released.Add(key);
        }
    }

    public void SetMouseButton(int button, bool isDown)
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

    public void SetMousePosition(int x, int y)
    {
        MouseX = x; MouseY = y;
    }

    public void AddWheel(float dx, float dy)
    {
        WheelX += dx; WheelY += dy;
    }

    public void AddText(string s)
    {
        foreach (var c in s) _textInput.Add(c);
    }

    public bool KeyDown(Key k) => _down.Contains(k);
    public bool KeyPressed(Key k) => _pressed.Contains(k);
    public bool KeyReleased(Key k) => _released.Contains(k);

    public bool MouseDown(int button) => _mouseDown.Contains(button);
    public bool MousePressed(int button) => _mousePressed.Contains(button);
    public bool MouseReleased(int button) => _mouseReleased.Contains(button);

    public ReadOnlySpan<char> TextInput => CollectionsMarshal.AsSpan(_textInput);
}

public enum Key
{
    Unknown = 0,
    A = 4,
    B = 5,
    C = 6,
    D = 7,
    E = 8,
    F = 9,
    G = 10, // 0x0000000A
    H = 11, // 0x0000000B
    I = 12, // 0x0000000C
    J = 13, // 0x0000000D
    K = 14, // 0x0000000E
    L = 15, // 0x0000000F
    M = 16, // 0x00000010
    N = 17, // 0x00000011
    O = 18, // 0x00000012
    P = 19, // 0x00000013
    Q = 20, // 0x00000014
    R = 21, // 0x00000015
    S = 22, // 0x00000016
    T = 23, // 0x00000017
    U = 24, // 0x00000018
    V = 25, // 0x00000019
    W = 26, // 0x0000001A
    X = 27, // 0x0000001B
    Y = 28, // 0x0000001C
    Z = 29, // 0x0000001D
    Alpha1 = 30, // 0x0000001E
    Alpha2 = 31, // 0x0000001F
    Alpha3 = 32, // 0x00000020
    Alpha4 = 33, // 0x00000021
    Alpha5 = 34, // 0x00000022
    Alpha6 = 35, // 0x00000023
    Alpha7 = 36, // 0x00000024
    Alpha8 = 37, // 0x00000025
    Alpha9 = 38, // 0x00000026
    Alpha0 = 39, // 0x00000027
    Return = 40, // 0x00000028
    Escape = 41, // 0x00000029
    Backspace = 42, // 0x0000002A
    Tab = 43, // 0x0000002B
    Space = 44, // 0x0000002C
    Minus = 45, // 0x0000002D
    Equals = 46, // 0x0000002E
    Leftbracket = 47, // 0x0000002F
    Rightbracket = 48, // 0x00000030

    /// <summary>
    /// Located at the lower left of the return
    /// key on ISO keyboards and at the right end
    /// of the QWERTY row on ANSI keyboards.
    /// Produces REVERSE SOLIDUS (backslash) and
    /// VERTICAL LINE in a US layout, REVERSE
    /// SOLIDUS and VERTICAL LINE in a UK Mac
    /// layout, NUMBER SIGN and TILDE in a UK
    /// Windows layout, DOLLAR SIGN and POUND SIGN
    /// in a Swiss German layout, NUMBER SIGN and
    /// APOSTROPHE in a German layout, GRAVE
    /// ACCENT and POUND SIGN in a French Mac
    /// layout, and ASTERISK and MICRO SIGN in a
    /// French Windows layout.
    /// </summary>
    Backslash = 49, // 0x00000031

    /// <summary>
    /// ISO USB keyboards actually use this code
    /// instead of 49 for the same key, but all
    /// OSes I've seen treat the two codes
    /// identically. So, as an implementor, unless
    /// your keyboard generates both of those
    /// codes and your OS treats them differently,
    /// you should generate BACKSLASH
    /// instead of this code. As a user, you
    /// should not rely on this code because SDL
    /// will never generate it with most (all?)
    /// keyboards.
    /// </summary>
    NonUshash = 50, // 0x00000032
    Semicolon = 51, // 0x00000033
    Apostrophe = 52, // 0x00000034

    /// <summary>
    /// Located in the top left corner (on both ANSI
    /// and ISO keyboards). Produces GRAVE ACCENT and
    /// TILDE in a US Windows layout and in US and UK
    /// Mac layouts on ANSI keyboards, GRAVE ACCENT
    /// and NOT SIGN in a UK Windows layout, SECTION
    /// SIGN and PLUS-MINUS SIGN in US and UK Mac
    /// layouts on ISO keyboards, SECTION SIGN and
    /// DEGREE SIGN in a Swiss German layout (Mac:
    /// only on ISO keyboards), CIRCUMFLEX ACCENT and
    /// DEGREE SIGN in a German layout (Mac: only on
    /// ISO keyboards), SUPERSCRIPT TWO and TILDE in a
    /// French Windows layout, COMMERCIAL AT and
    /// NUMBER SIGN in a French Mac layout on ISO
    /// keyboards, and LESS-THAN SIGN and GREATER-THAN
    /// SIGN in a Swiss German, German, or French Mac
    /// layout on ANSI keyboards.
    /// </summary>
    Grave = 53, // 0x00000035
    Comma = 54, // 0x00000036
    Period = 55, // 0x00000037
    Slash = 56, // 0x00000038
    Capslock = 57, // 0x00000039
    F1 = 58, // 0x0000003A
    F2 = 59, // 0x0000003B
    F3 = 60, // 0x0000003C
    F4 = 61, // 0x0000003D
    F5 = 62, // 0x0000003E
    F6 = 63, // 0x0000003F
    F7 = 64, // 0x00000040
    F8 = 65, // 0x00000041
    F9 = 66, // 0x00000042
    F10 = 67, // 0x00000043
    F11 = 68, // 0x00000044
    F12 = 69, // 0x00000045
    Printscreen = 70, // 0x00000046
    Scrolllock = 71, // 0x00000047
    Pause = 72, // 0x00000048

    /// <summary>
    /// insert on PC, help on some Mac keyboards (but
    /// does send code 73, not 117)
    /// </summary>
    Insert = 73, // 0x00000049
    Home = 74, // 0x0000004A
    Pageup = 75, // 0x0000004B
    Delete = 76, // 0x0000004C
    End = 77, // 0x0000004D
    Pagedown = 78, // 0x0000004E
    Right = 79, // 0x0000004F
    Left = 80, // 0x00000050
    Down = 81, // 0x00000051
    Up = 82, // 0x00000052

    /// <summary>num lock on PC, clear on Mac keyboards</summary>
    NumLockClear = 83, // 0x00000053
    KpDivide = 84, // 0x00000054
    KpMultiply = 85, // 0x00000055
    KpMinus = 86, // 0x00000056
    KpPlus = 87, // 0x00000057
    KpEnter = 88, // 0x00000058
    Kp1 = 89, // 0x00000059
    Kp2 = 90, // 0x0000005A
    Kp3 = 91, // 0x0000005B
    Kp4 = 92, // 0x0000005C
    Kp5 = 93, // 0x0000005D
    Kp6 = 94, // 0x0000005E
    Kp7 = 95, // 0x0000005F
    Kp8 = 96, // 0x00000060
    Kp9 = 97, // 0x00000061
    Kp0 = 98, // 0x00000062
    KpPeriod = 99, // 0x00000063

    /// <summary>
    /// This is the additional key that ISO
    /// keyboards have over ANSI ones,
    /// located between left shift and Z.
    /// Produces GRAVE ACCENT and TILDE in a
    /// US or UK Mac layout, REVERSE SOLIDUS
    /// (backslash) and VERTICAL LINE in a
    /// US or UK Windows layout, and
    /// LESS-THAN SIGN and GREATER-THAN SIGN
    /// in a Swiss German, German, or French
    /// layout.
    /// </summary>
    NonUsBackSlash = 100, // 0x00000064

    /// <summary>windows contextual menu, compose</summary>
    Application = 101, // 0x00000065

    /// <summary>
    /// The USB document says this is a status flag,
    /// not a physical key - but some Mac keyboards
    /// do have a power key.
    /// </summary>
    Power = 102, // 0x00000066
    KpEquals = 103, // 0x00000067
    F13 = 104, // 0x00000068
    F14 = 105, // 0x00000069
    F15 = 106, // 0x0000006A
    F16 = 107, // 0x0000006B
    F17 = 108, // 0x0000006C
    F18 = 109, // 0x0000006D
    F19 = 110, // 0x0000006E
    F20 = 111, // 0x0000006F
    F21 = 112, // 0x00000070
    F22 = 113, // 0x00000071
    F23 = 114, // 0x00000072
    F24 = 115, // 0x00000073
    Execute = 116, // 0x00000074

    /// <summary>AL Integrated Help Center</summary>
    Help = 117, // 0x00000075

    /// <summary>Menu (show menu)</summary>
    Menu = 118, // 0x00000076
    Select = 119, // 0x00000077

    /// <summary>AC Stop</summary>
    Stop = 120, // 0x00000078

    /// <summary>AC Redo/Repeat</summary>
    Again = 121, // 0x00000079

    /// <summary>AC Undo</summary>
    Undo = 122, // 0x0000007A

    /// <summary>AC Cut</summary>
    Cut = 123, // 0x0000007B

    /// <summary>AC Copy</summary>
    Copy = 124, // 0x0000007C

    /// <summary>AC Paste</summary>
    Paste = 125, // 0x0000007D

    /// <summary>AC Find</summary>
    Find = 126, // 0x0000007E
    Mute = 127, // 0x0000007F
    VolumeUp = 128, // 0x00000080
    VolumeDown = 129, // 0x00000081
    KpComma = 133, // 0x00000085
    KpEqualsAs400 = 134, // 0x00000086

    /// <summary>
    /// used on Asian keyboards, see
    /// footnotes in USB doc
    /// </summary>
    International1 = 135, // 0x00000087
    International2 = 136, // 0x00000088

    /// <summary>Yen</summary>
    International3 = 137, // 0x00000089
    International4 = 138, // 0x0000008A
    International5 = 139, // 0x0000008B
    International6 = 140, // 0x0000008C
    International7 = 141, // 0x0000008D
    International8 = 142, // 0x0000008E
    International9 = 143, // 0x0000008F

    /// <summary>Hangul/English toggle</summary>
    Lang1 = 144, // 0x00000090

    /// <summary>Hanja conversion</summary>
    Lang2 = 145, // 0x00000091

    /// <summary>Katakana</summary>
    Lang3 = 146, // 0x00000092

    /// <summary>Hiragana</summary>
    Lang4 = 147, // 0x00000093

    /// <summary>Zenkaku/Hankaku</summary>
    Lang5 = 148, // 0x00000094

    /// <summary>reserved</summary>
    Lang6 = 149, // 0x00000095

    /// <summary>reserved</summary>
    Lang7 = 150, // 0x00000096

    /// <summary>reserved</summary>
    Lang8 = 151, // 0x00000097

    /// <summary>reserved</summary>
    Lang9 = 152, // 0x00000098

    /// <summary>Erase-Eaze</summary>
    AltErase = 153, // 0x00000099
    SysReq = 154, // 0x0000009A

    /// <summary>AC Cancel</summary>
    Cancel = 155, // 0x0000009B
    Clear = 156, // 0x0000009C
    Prior = 157, // 0x0000009D
    Return2 = 158, // 0x0000009E
    Separator = 159, // 0x0000009F
    Out = 160, // 0x000000A0
    Oper = 161, // 0x000000A1
    ClearAgain = 162, // 0x000000A2
    CrSel = 163, // 0x000000A3
    ExSel = 164, // 0x000000A4
    Kp00 = 176, // 0x000000B0
    Kp000 = 177, // 0x000000B1
    ThousandsSeparator = 178, // 0x000000B2
    DecimalSeparator = 179, // 0x000000B3
    CurrencyUnit = 180, // 0x000000B4
    CurrencySubunit = 181, // 0x000000B5
    KpLeftParen = 182, // 0x000000B6
    KpRightParen = 183, // 0x000000B7
    KpLeftBrace = 184, // 0x000000B8
    KpRightBrace = 185, // 0x000000B9
    KpTab = 186, // 0x000000BA
    KpBackspace = 187, // 0x000000BB
    KpA = 188, // 0x000000BC
    KpB = 189, // 0x000000BD
    KpC = 190, // 0x000000BE
    KpD = 191, // 0x000000BF
    KpE = 192, // 0x000000C0
    KpF = 193, // 0x000000C1
    KpXor = 194, // 0x000000C2
    KpPower = 195, // 0x000000C3
    KpPercent = 196, // 0x000000C4
    KpLess = 197, // 0x000000C5
    KpGreater = 198, // 0x000000C6
    KpAmpersand = 199, // 0x000000C7
    KpDblAmpersand = 200, // 0x000000C8
    KpVerticalBar = 201, // 0x000000C9
    KpDblVerticalBar = 202, // 0x000000CA
    KpColon = 203, // 0x000000CB
    KpHash = 204, // 0x000000CC
    KpSpace = 205, // 0x000000CD
    KpAt = 206, // 0x000000CE
    KpExClam = 207, // 0x000000CF
    KpMemStore = 208, // 0x000000D0
    KpMemRecall = 209, // 0x000000D1
    KpMemClear = 210, // 0x000000D2
    KpMemAdd = 211, // 0x000000D3
    KpMemSubtract = 212, // 0x000000D4
    KpMemMultiply = 213, // 0x000000D5
    KpMemDivide = 214, // 0x000000D6
    KpPlusMinus = 215, // 0x000000D7
    KpClear = 216, // 0x000000D8
    KpClearEntry = 217, // 0x000000D9
    KpBinary = 218, // 0x000000DA
    KpOctal = 219, // 0x000000DB
    KpDecimal = 220, // 0x000000DC
    KpHexadecimal = 221, // 0x000000DD
    LCtrl = 224, // 0x000000E0
    LShift = 225, // 0x000000E1

    /// <summary>alt, option</summary>
    LAlt = 226, // 0x000000E2

    /// <summary>windows, command (apple), meta</summary>
    LGUI = 227, // 0x000000E3
    RCtrl = 228, // 0x000000E4
    RShift = 229, // 0x000000E5

    /// <summary>alt gr, option</summary>
    RAlt = 230, // 0x000000E6

    /// <summary>windows, command (apple), meta</summary>
    RGUI = 231, // 0x000000E7

    /// <summary>
    /// I'm not sure if this is really not covered
    /// by any of the above, but since there's a
    /// special SDL_KMOD_MODE for it I'm adding it here
    /// </summary>
    Mode = 257, // 0x00000101

    /// <summary>Sleep</summary>
    Sleep = 258, // 0x00000102

    /// <summary>Wake</summary>
    Wake = 259, // 0x00000103

    /// <summary>Channel Increment</summary>
    ChannelIncrement = 260, // 0x00000104

    /// <summary>Channel Decrement</summary>
    ChannelDecrement = 261, // 0x00000105

    /// <summary>Play</summary>
    MediaPlay = 262, // 0x00000106

    /// <summary>Pause</summary>
    MediaPause = 263, // 0x00000107

    /// <summary>Record</summary>
    MediaRecord = 264, // 0x00000108

    /// <summary>Fast Forward</summary>
    MediaFastForward = 265, // 0x00000109

    /// <summary>Rewind</summary>
    MediaRewind = 266, // 0x0000010A

    /// <summary>Next Track</summary>
    MediaNextTrack = 267, // 0x0000010B

    /// <summary>Previous Track</summary>
    MediaPreviousTrack = 268, // 0x0000010C

    /// <summary>Stop</summary>
    MediaStop = 269, // 0x0000010D

    /// <summary>Eject</summary>
    MediaEject = 270, // 0x0000010E

    /// <summary>Play / Pause</summary>
    MediaPlayPause = 271, // 0x0000010F

    /// <summary>Media Select</summary>
    MediaSelect = 272, // 0x00000110

    /// <summary>AC New</summary>
    ACNew = 273, // 0x00000111

    /// <summary>AC Open</summary>
    ACOpen = 274, // 0x00000112

    /// <summary>AC Close</summary>
    ACClose = 275, // 0x00000113

    /// <summary>AC Exit</summary>
    ACExit = 276, // 0x00000114

    /// <summary>AC Save</summary>
    ACSave = 277, // 0x00000115

    /// <summary>AC Print</summary>
    ACPrint = 278, // 0x00000116

    /// <summary>AC Properties</summary>
    ACProperties = 279, // 0x00000117

    /// <summary>AC Search</summary>
    ACSearch = 280, // 0x00000118

    /// <summary>AC Home</summary>
    ACHome = 281, // 0x00000119

    /// <summary>AC Back</summary>
    ACBack = 282, // 0x0000011A

    /// <summary>AC Forward</summary>
    ACForward = 283, // 0x0000011B

    /// <summary>AC Stop</summary>
    ACStop = 284, // 0x0000011C

    /// <summary>AC Refresh</summary>
    ACRefresh = 285, // 0x0000011D

    /// <summary>AC Bookmarks</summary>
    ACBookmarks = 286, // 0x0000011E

    /// <summary>
    /// Usually situated below the display on phones and
    /// used as a multi-function feature key for selecting
    /// a software defined function shown on the bottom left
    /// of the display.
    /// </summary>
    SoftLeft = 287, // 0x0000011F

    /// <summary>
    /// Usually situated below the display on phones and
    /// used as a multi-function feature key for selecting
    /// a software defined function shown on the bottom right
    /// of the display.
    /// </summary>
    SoftRight = 288, // 0x00000120

    /// <summary>Used for accepting phone calls.</summary>
    Call = 289, // 0x00000121

    /// <summary>Used for rejecting phone calls.</summary>
    EndCall = 290, // 0x00000122

    /// <summary>400-500 reserved for dynamic keycodes</summary>
    Reserved = 400, // 0x00000190

    /// <summary>
    /// not a key, just marks the number of scancodes for array bounds
    /// </summary>
    Count = 512, // 0x00000200
}