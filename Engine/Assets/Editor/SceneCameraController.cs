using Vortice.Win32;

namespace Engine.Editor;

public sealed class SceneCameraController : EditorComponent
{
    public string Profile => Entity.Transform.ToString();

    public static bool ViewportFocused { get => s_viewportFocused; set => s_viewportFocused = value; }
    private static bool s_viewportFocused = true;

    public static float MovementSpeed { get => s_movementSpeed; set => s_movementSpeed = value; }
    private static float s_movementSpeed = 2;

    public static float RotationSpeed { get => s_rotationSpeed; set => s_rotationSpeed = value; }
    private static float s_rotationSpeed = 5;

    private Vector3 _direction;
    private Vector3 _euler;

    private Vector2 _mousePosition;

    public override void OnRegister() =>
        // Register the component with the EditorScriptSystem.
        EditorScriptSystem.Register(this);

    public override void OnUpdate()
    {
        // Call the MovementSpeedCalc function to calculate the movement speed.
        MovementSpeedCalc();

        if (Input.GetButton(MouseButton.Right, InputState.Down)
            || Input.GetButton(MouseButton.Middle, InputState.Down))
        {
            User32.GetCursorPos(out var point);
            _mousePosition.X = point.X;
            _mousePosition.Y = point.Y;
        }

        // Check if the right mouse button is pressed.
        // If so, call the TransformMovement, CameraMovement and HeightTransformMovement functions.
        if (Input.GetButton(MouseButton.Right) && ViewportFocused)
        {
            TransformMovement();
            HeightTransformMovement();

            //User32.SetCursor(User32.LoadCursor(IntPtr.Zero, SystemCursor.IDC_CROSS));
            User32.SetCursorPos((int)_mousePosition.X, (int)_mousePosition.Y);
        }

        if (ViewportFocused)
            // Call the ScrollMovement function to handle the movement using the scroll wheel.
            ScrollMovement();

        // Update the entity's position based on the calculated direction and movement speed.
        Entity.Transform.LocalPosition += _direction * Time.DeltaF * s_movementSpeed;

        // Reset the direction vector.
        _direction = Vector3.Zero;
    }

    public override void OnFixedUpdate()
    {
        // Check if the middle mouse button is pressed. If so, call the ScreenMovement function.
        if (Input.GetButton(MouseButton.Middle) && ViewportFocused)
        {
            User32.SetCursorPos((int)_mousePosition.X, (int)_mousePosition.Y);

            ScreenMovement();
        }

        if (Input.GetButton(MouseButton.Right) && ViewportFocused)
        {
            //User32.SetCursor(User32.LoadCursor(IntPtr.Zero, SystemCursor.IDC_CROSS));
            User32.SetCursorPos((int)_mousePosition.X, (int)_mousePosition.Y);

            _euler.X = Input.GetMouseDelta().Y;
            _euler.Y = Input.GetMouseDelta().X;

            // Update the entity's rotation based on the calculated rotation and rotation speed.
            Entity.Transform.EulerAngles -= _euler * Time.DeltaF * s_rotationSpeed;

            // Clamp Vertical Rotation to 90 degrees up and down.
            var clampedEuler = Entity.Transform.EulerAngles;
            clampedEuler.X = Math.Clamp(clampedEuler.X, -89, 89);
            Entity.Transform.EulerAngles = clampedEuler;
        }
    }

    private void MovementSpeedCalc()
    {
        // Check if either the right or middle mouse button is pressed.
        // If so, update the movement speed based on the mouse wheel input.
        if (Input.GetButton(MouseButton.Right))
            s_movementSpeed += Input.GetMouseWheel();

        // Clamp the movement speed between 0.1 and 10.
        s_movementSpeed = Math.Clamp(s_movementSpeed, 0.1f, 10);
    }

    private void TransformMovement() =>
        // Calculate the direction based on the forward and right vectors of the entity's transform
        // and the X and Y axis inputs from the Input class.
        _direction = Entity.Transform.Forward * Input.GetAxis().Y + Entity.Transform.Right * Input.GetAxis().X;

    private void ScreenMovement() =>
        // Update the direction by subtracting the right vector multiplied by the mouse X axis input,
        // and the local up vector multiplied by the mouse Y axis input, both scaled by the time delta.
        _direction -= Entity.Transform.Right * Input.GetMouseDelta().X * 0.5f + Entity.Transform.Up * Input.GetMouseDelta().Y * 0.5f;

    private void ScrollMovement()
    {
        // Check if none of the right, middle, and left mouse buttons are pressed.
        // If so, update the direction based on the mouse wheel input and the forward vector of the entity's transform.
        if (!Input.GetButton(MouseButton.Right)
            && !Input.GetButton(MouseButton.Middle)
            && !Input.GetButton(MouseButton.Right))
            _direction += 25 * Entity.Transform.Forward * Input.GetMouseWheel();
    }

    private void HeightTransformMovement()
    {
        // Initialize a variable to store the input value.
        float input = 0;

        // Check if the E or Q key is pressed and update the input variable accordingly.
        if (Input.GetKey(Key.E)) input = 1;
        if (Input.GetKey(Key.Q)) input = -1;

        // Check if both the E and W or Q and W keys are pressed and update the input variable accordingly.
        if (Input.GetKey(Key.E) && Input.GetKey(Key.W)) input = 1;
        if (Input.GetKey(Key.Q) && Input.GetKey(Key.W)) input = -1;

        // Check if both the E and S or Q and S keys are pressed and update the input variable accordingly.
        if (Input.GetKey(Key.E) && Input.GetKey(Key.S)) input = 1;
        if (Input.GetKey(Key.Q) && Input.GetKey(Key.S)) input = -1;

        // Check if either the W or S key is pressed and update the direction
        // based on the local up vector of the entity's transform and the input variable.
        if (Input.GetKey(Key.W) || Input.GetKey(Key.S))
            _direction += input * Entity.Transform.Up;
        // If neither the W or S key is pressed, update the direction
        // based on the global Y unit vector and the input variable.
        else
            _direction += input * Vector3.UnitY;
    }
}

