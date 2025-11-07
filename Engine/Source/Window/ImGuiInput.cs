using System.Runtime.InteropServices;
using ImGuiNET;
using SDL3;

namespace Engine;

/// <summary>
/// Provides functionality to process SDL events and translate them into ImGui input events.
/// </summary>
public static class ImGuiInput
{
    /// <summary>
    /// Processes an SDL event and updates the ImGui input state accordingly.
    /// </summary>
    /// <param name="e">The SDL event to process.</param>
    public static void ProcessEvent(SDL.Event e)
    {
        var io = ImGui.GetIO();
        switch ((SDL.EventType)e.Type)
        {
            case SDL.EventType.MouseMotion:
                // Updates the mouse position in ImGui.
                io.AddMousePosEvent(e.Motion.X, e.Motion.Y);
                break;
            case SDL.EventType.MouseButtonDown:
            case SDL.EventType.MouseButtonUp:
                // Updates the mouse button state in ImGui.
                bool down = (SDL.EventType)e.Type == SDL.EventType.MouseButtonDown;
                int button = e.Button.Button;
                if (button >= 1 && button <= 5)
                    io.AddMouseButtonEvent(button - 1, down);
                break;
            case SDL.EventType.MouseWheel:
                // Updates the mouse wheel scroll state in ImGui.
                io.AddMouseWheelEvent(e.Wheel.X, e.Wheel.Y);
                break;
            case SDL.EventType.TextInput:
                // Adds UTF-8 text input to ImGui.
                if (e.Text.Text != IntPtr.Zero)
                {
                    string? s = Marshal.PtrToStringUTF8(e.Text.Text);
                    if (!string.IsNullOrEmpty(s))
                        io.AddInputCharactersUTF8(s);
                }
                break;
            case SDL.EventType.KeyDown:
            case SDL.EventType.KeyUp:
                // Updates the keyboard key state in ImGui.
                bool isDown = (SDL.EventType)e.Type == SDL.EventType.KeyDown;
                var sc = e.Key.Scancode;
                ImGuiKey imguiKey = SdlKeyToImGuiKey(sc);
                if (imguiKey != ImGuiKey.None)
                    io.AddKeyEvent(imguiKey, isDown);

                // Updates the modifier key states (Shift, Ctrl, Alt) in ImGui.
                var mods = SDL.GetModState();
                io.AddKeyEvent(ImGuiKey.ModShift, mods.HasFlag(SDL.Keymod.Shift));
                io.AddKeyEvent(ImGuiKey.ModCtrl, mods.HasFlag(SDL.Keymod.Ctrl));
                io.AddKeyEvent(ImGuiKey.ModAlt, mods.HasFlag(SDL.Keymod.Alt));
                break;
        }
    }

    /// <summary>
    /// Maps SDL scancodes to ImGui keys.
    /// </summary>
    /// <param name="sc">The SDL scancode to map.</param>
    /// <returns>The corresponding ImGui key, or <see cref="ImGuiKey.None"/> if no mapping exists.</returns>
    private static ImGuiKey SdlKeyToImGuiKey(SDL.Scancode sc)
    {
        return sc switch
        {
            SDL.Scancode.Tab => ImGuiKey.Tab,
            SDL.Scancode.Left => ImGuiKey.LeftArrow,
            SDL.Scancode.Right => ImGuiKey.RightArrow,
            SDL.Scancode.Up => ImGuiKey.UpArrow,
            SDL.Scancode.Down => ImGuiKey.DownArrow,
            SDL.Scancode.Home => ImGuiKey.Home,
            SDL.Scancode.End => ImGuiKey.End,
            SDL.Scancode.Insert => ImGuiKey.Insert,
            SDL.Scancode.Delete => ImGuiKey.Delete,
            SDL.Scancode.Backspace => ImGuiKey.Backspace,
            SDL.Scancode.Space => ImGuiKey.Space,
            SDL.Scancode.Return => ImGuiKey.Enter,
            SDL.Scancode.Escape => ImGuiKey.Escape,
            SDL.Scancode.A => ImGuiKey.A,
            SDL.Scancode.C => ImGuiKey.C,
            SDL.Scancode.V => ImGuiKey.V,
            SDL.Scancode.X => ImGuiKey.X,
            SDL.Scancode.Y => ImGuiKey.Y,
            SDL.Scancode.Z => ImGuiKey.Z,
            _ => ImGuiKey.None
        };
    }
}
