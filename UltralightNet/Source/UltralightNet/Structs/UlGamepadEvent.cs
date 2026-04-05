namespace UltralightNet.Structs;

/// <summary>The various <see cref="GamepadEvent" /> types.</summary>
public enum GamepadEventType
{
	/// <summary>This event type should be fired when a gamepad is connected.</summary>
	/// <remarks>
	///     You will need to previously declare the gamepad, its index, and details about
	///     its axis and button layout via <see cref="Renderer.SetGamepadDetails(uint, string, uint, uint)" />
	///     prior to calling
	///     <see cref="Renderer.FireGamepadEvent(GamepadEvent)" />.
	/// </remarks>
	Connected,

	/// <summary>This event type should be fired when a gamepad is disconnected.</summary>
	Disconnected
}

/// <summary>Event representing a change in gamepad connection state</summary>
/// <param name="Type">The type of this GamepadEvent.</param>
/// <param name="Index">The index of the gamepad.</param>
/// <see cref="Renderer.FireGamepadEvent(GamepadEvent)" />
public record struct GamepadEvent(GamepadEventType Type, uint Index);

/// <summary>Event representing a change in gamepad axis state (eg, pressing a stick in a certain direction).</summary>
/// <param name="Index">The index of the gamepad.</param>
/// <param name="Axis">The index of the axis whose value has changed.</param>
/// <param name="Value">The new value of the axis.<br />
///     <remarks>This value should be normalized to the range [-1.0, 1.0].</remarks>
/// </param>
/// <see cref="Renderer.FireGamepadAxisEvent(GamepadAxisEvent)" />
public record struct GamepadAxisEvent(uint Index, uint Axis, double Value);

/// <summary>Event representing a change in gamepad button state (eg, pressing a button on a gamepad).</summary>
/// <param name="Index">The index of the gamepad.</param>
/// <param name="Button">The index of the button whose value has changed.</param>
/// <param name="Value">
///     The new value of the button.<br />
///     <remarks>
///         This value should be normalized to the range [-1.0, 1.0], with any value greater than 0.0 to be considered
///         "pressed".
///     </remarks>
/// </param>
/// <see cref="Renderer.FireGamepadButtonEvent(GamepadButtonEvent)" />
public record struct GamepadButtonEvent(uint Index, uint Button, double Value);
