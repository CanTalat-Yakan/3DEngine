namespace Engine.Editor;

public sealed class ViewportController : EditorComponent
{
    public static ViewportController Instance { get; private set; }

    public static Camera Camera => s_camera;
    private static Camera s_camera;

    public static bool ViewportFocused { get => s_viewportFocused; set => s_viewportFocused = value; }
    private static bool s_viewportFocused = true;

    public float MovementSpeed { get => _movementSpeed; set => _movementSpeed = value; }
    private float _movementSpeed = 2;
    private float _movementSpeedScaled = 2;

    public float RotationSpeed { get => _rotationSpeed; set => _rotationSpeed = value; }
    private float _rotationSpeed = 5;

    private Vector3 _direction;
    private Vector3 _euler;

    private Vector2 _mousePosition;

    public override void OnAwake()
    {
        if (Instance is null)
            Instance = this;
    }

    public void SetCamera(Camera camera) =>
        s_camera = camera;

    public override void OnUpdate()
    {
        // Call the MovementSpeedCalculation function to calculate the movement speed.
        MovementSpeedCalculation();

        if (Input.GetButton(MouseButton.Right, InputState.Down)
            || Input.GetButton(MouseButton.Middle, InputState.Down))
        {
            Vortice.Win32.User32.GetCursorPos(out var point);
            _mousePosition.X = point.X;
            _mousePosition.Y = point.Y;
        }

        // Check if the right mouse button is pressed.
        // If so, call the TransformMovement, CameraMovement and HeightTransformMovement functions.
        // Then update the Rotation of this Entity and clamp its vertical rotation.
        if (Input.GetButton(MouseButton.Right) && ViewportFocused)
        {
            TransformMovement();
            HeightTransformMovement();

            //User32.SetCursor(User32.LoadCursor(IntPtr.Zero, SystemCursor.IDC_CROSS));
            Vortice.Win32.User32.SetCursorPos((int)_mousePosition.X, (int)_mousePosition.Y);

            _euler.X = Input.GetMouseDelta().Y;
            _euler.Y = Input.GetMouseDelta().X;

            // Update the entity rotation based on the calculated rotation and rotation speed.
            Entity.Transform.EulerAngles -= _euler * Time.FixedDelta * _rotationSpeed;

            // Clamp Vertical Rotation to 90 degrees up and down.
            var clampedEuler = Entity.Transform.EulerAngles;
            clampedEuler.X = Math.Clamp(clampedEuler.X, -89, 89);
            Entity.Transform.EulerAngles = clampedEuler;
        }

        // Check if the middle mouse button is pressed. If so, call the ScreenMovement function.
        if (Input.GetButton(MouseButton.Middle) && ViewportFocused)
        {
            Vortice.Win32.User32.SetCursorPos((int)_mousePosition.X, (int)_mousePosition.Y);

            ScreenMovement();
        }

        if (ViewportFocused)
            // Call the ScrollMovement function to handle the movement using the scroll wheel.
            ScrollMovement();

        // Update the entity's position based on the calculated direction and movement speed.
        Entity.Transform.LocalPosition += _direction * Time.DeltaF * _movementSpeedScaled;

        // Reset the direction vector.
        _direction = Vector3.Zero;
    }

    public override void OnLateUpdate() =>
        Camera.Projection = Renderer.Instance.Config.CameraProjection;

    private void MovementSpeedCalculation()
    {
        // Check if either the right or middle mouse button is pressed.
        // If so, update the movement speed based on the mouse wheel input.
        if (Input.GetButton(MouseButton.Right))
            _movementSpeed += Input.GetMouseWheel();

        // Clamp the movement speed between 0.1 and 10.
        _movementSpeed = Math.Clamp(_movementSpeed, 0.1f, 10);

        _movementSpeedScaled = _movementSpeed;

        if (Input.GetKey(Key.LeftShift))
            _movementSpeedScaled *= 3;
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

