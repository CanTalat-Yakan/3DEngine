namespace Engine.Essentials;

public sealed class ViewportController : EditorComponent, IHide
{
    public static ViewportController Instance { get; private set; }

    public static Camera Camera => s_camera;
    private static Camera s_camera;

    public static bool ViewportFocused { get => s_viewportFocused; set => s_viewportFocused = value; }
    private static bool s_viewportFocused = true;

    public float MovementSpeed { get => _movementSpeed; set => _movementSpeed = value; }
    private float _movementSpeed = 2;
    private float _movementSpeedScaled = 2;

    private float _movementSpeedScaleFactor = 5;
    private float _scrollWheelSpeedScaleFactor = 50;

    public float RotationSpeed { get => _rotationSpeed; set => _rotationSpeed = value; }
    private float _rotationSpeed = 25;

    public bool LockCursor { get; set; } = true;

    private Vector3 _direction;
    private Vector3 _euler;

    private Vector2 _mousePosition;

    public override void OnAwake() =>
        Instance ??= this;

    public void SetCamera(Camera camera) =>
        s_camera = camera;

    public override void OnUpdate()
    {
        Input.SetLockMouse(false);

        MovementSpeedCalculation();

        if (Input.GetButton(MouseButton.Right, InputState.Down)
         || Input.GetButton(MouseButton.Middle, InputState.Down))
        {
            Interoperation.User32.GetCursorPos(out var point);
            _mousePosition.X = point.X;
            _mousePosition.Y = point.Y;
        }

        if (Input.GetButton(MouseButton.Right) && ViewportFocused)
        {
            Input.SetLockMouse(LockCursor);

            CalculateMovementDirection();
            HeightTransformMovement();

            CameraRotation();
        }

        if (Input.GetButton(MouseButton.Middle) && ViewportFocused)
        {
            Input.SetLockMouse(LockCursor);

            ScreenSpaceMovement();
        }

        if (ViewportFocused)
            ScrollWheelMovement();

        Entity.Transform.LocalPosition += _direction * Time.DeltaF * _movementSpeedScaled;

        _direction = Vector3.Zero;
    }

    public override void OnLateUpdate()
    {
        Camera.Projection = Kernel.Instance.Config.CameraProjection;
        Camera.RenderMode = Kernel.Instance.Config.RenderMode;
    }

    private void CameraRotation()
    {
        Vector2 mouseDelta = Input.GetMouseDelta();

        _euler.X = mouseDelta.Y;
        _euler.Y = mouseDelta.X;

        Entity.Transform.EulerAngles -= _euler * Time.DeltaF * _rotationSpeed;

        // Clamp Vertical Rotation to ~90 degrees up and down.
        var clampedEuler = Entity.Transform.EulerAngles;
        clampedEuler.X = Math.Clamp(clampedEuler.X, -89, 89);
        Entity.Transform.EulerAngles = clampedEuler;
    }

    private void MovementSpeedCalculation()
    {
        if (Input.GetButton(MouseButton.Right))
            _movementSpeed += Input.GetMouseWheel();

        // Clamp the movement speed between 0.1 and 10.
        _movementSpeed = Math.Clamp(_movementSpeed, 0.1f, 10);

        _movementSpeedScaled = _movementSpeed;

        if (Input.GetKey(Key.LeftShift))
            _movementSpeedScaled *= _movementSpeedScaleFactor;

        if (Input.GetKey(Key.LeftControl))
            _movementSpeedScaled /= _movementSpeedScaleFactor;
    }

    private void CalculateMovementDirection() =>
        // Calculate the direction based on the forward and right vectors of the entity's transform
        // and the X and Y axis inputs from the Input class.
        _direction = Entity.Transform.Forward * Input.GetAxis().Y + Entity.Transform.Right * Input.GetAxis().X;

    private void ScreenSpaceMovement() =>
        // Update the direction by subtracting the right vector multiplied by the mouse X axis input,
        // and the local up vector multiplied by the mouse Y axis input, both scaled by the time delta.
        _direction -= Entity.Transform.Right * Input.GetMouseDelta().X * 0.5f + Entity.Transform.Up * Input.GetMouseDelta().Y * 0.5f;

    private void ScrollWheelMovement()
    {
        // Check if none of the right, middle, and left mouse buttons are pressed.
        // If so, update the direction based on the mouse wheel input and the forward vector of the entity's transform.
        if (!Input.GetButton(MouseButton.Right)
         && !Input.GetButton(MouseButton.Middle)
         && !Input.GetButton(MouseButton.Left))
            _direction += _scrollWheelSpeedScaleFactor * Entity.Transform.Forward * Input.GetMouseWheel();
    }

    private void HeightTransformMovement()
    {
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

