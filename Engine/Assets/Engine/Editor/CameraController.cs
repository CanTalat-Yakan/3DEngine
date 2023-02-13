using System.Numerics;
using System;
using Windows.System;
using Engine.ECS;
using Engine.Utilities;

namespace Engine.Editor;

internal class CameraController : EditorComponent
{
    public string Profile => Entity.Transform.ToString();

    public static float MovementSpeed { get => s_movementSpeed; set => s_movementSpeed = value; }
    private static float s_movementSpeed = 2;

    private float _rotationSpeed = 5;
    private Vector3 _direction;
    private Vector3 _rotation;

    public override void OnRegister() =>
        // Register the component with the EditorScriptSystem.
        EditorScriptSystem.Register(this);

    public override void OnUpdate()
    {
        // Call the MovementSpeedCalc function to calculate the movement speed.
        MovementSpeedCalc();

        // Check if the middle mouse button is pressed. If so, call the ScreenMovement function.
        if (Input.GetButton(EMouseButton.IsMiddleButtonPressed))
            ScreenMovement();

        // Check if the right mouse button is pressed.
        // If so, call the TransformMovement, CameraMovement and HeightTransformMovement functions.
        if (Input.GetButton(EMouseButton.IsRightButtonPressed))
        {
            TransformMovement();
            CameraRotation();
            HeightTransformMovement();
        }

        // Call the ScrollMovement function to handle the movement using the scroll wheel.
        ScrollMovement();

        // Update the entity's position based on the calculated direction and movement speed.
        Entity.Transform.LocalPosition += _direction * (float)Time.Delta * s_movementSpeed;
        // Update the entity's rotation based on the calculated rotation and rotation speed.
        Entity.Transform.EulerAngles -= _rotation * (float)Time.Delta * _rotationSpeed;

        //// Limit the entity's vertical rotation between -89 and 89 degrees.
        //Entity.Transform.EulerAngles = Math.Clamp(Entity.Transform.EulerAngles.X, -89, 89);

        // Reset the rotation and direction vector.
        _rotation = new();
        _direction = new();
    }

    private void MovementSpeedCalc()
    {
        // Check if either the right or middle mouse button is pressed.
        // If so, update the movement speed based on the mouse wheel input.
        if (Input.GetButton(EMouseButton.IsRightButtonPressed)
            || Input.GetButton(EMouseButton.IsRightButtonPressed))
            s_movementSpeed += Input.GetMouseWheel();

        // Clamp the movement speed between 0.1 and 10.
        s_movementSpeed = Math.Clamp(s_movementSpeed, 0.1f, 10);
    }

    private void CameraRotation() =>
        // Create a new rotation based on the mouse X and Y axis inputs.
        _rotation = new(Input.GetMouseAxis().Y, Input.GetMouseAxis().X, 0);

    private void TransformMovement() =>
        // Calculate the direction based on the forward and right vectors of the entity's transform
        // and the X and Y axis inputs from the Input class.
        _direction = Entity.Transform.Forward * Input.GetAxis().Y + Entity.Transform.Right * Input.GetAxis().X;

    private void ScreenMovement() =>
        // Update the direction by subtracting the right vector multiplied by the mouse X axis input,
        // and the local up vector multiplied by the mouse Y axis input, both scaled by the time delta.
        _direction -= Entity.Transform.Right * Input.GetMouseAxis().X * (float)Time.Delta + Entity.Transform.Up * Input.GetMouseAxis().Y * (float)Time.Delta;

    private void ScrollMovement()
    {
        // Check if none of the right, middle, and left mouse buttons are pressed.
        // If so, update the direction based on the mouse wheel input and the forward vector of the entity's transform.
        if (!Input.GetButton(EMouseButton.IsRightButtonPressed)
            && !Input.GetButton(EMouseButton.IsMiddleButtonPressed)
            && !Input.GetButton(EMouseButton.IsRightButtonPressed))
            _direction += 5 * Entity.Transform.Forward * Input.GetMouseWheel();
    }

    private void HeightTransformMovement()
    {
        // Initialize a variable to store the input value.
        float input = 0;

        // Check if the E or Q key is pressed and update the input variable accordingly.
        if (Input.GetKey(VirtualKey.E)) input = 1;
        if (Input.GetKey(VirtualKey.Q)) input = -1;

        // Check if both the E and W or Q and W keys are pressed and update the input variable accordingly.
        if (Input.GetKey(VirtualKey.E) && Input.GetKey(VirtualKey.W)) input = 1;
        if (Input.GetKey(VirtualKey.Q) && Input.GetKey(VirtualKey.W)) input = -1;

        // Check if both the E and S or Q and S keys are pressed and update the input variable accordingly.
        if (Input.GetKey(VirtualKey.E) && Input.GetKey(VirtualKey.S)) input = 1;
        if (Input.GetKey(VirtualKey.Q) && Input.GetKey(VirtualKey.S)) input = -1;

        // Check if either the W or S key is pressed and update the direction
        // based on the local up vector of the entity's transform and the input variable.
        if (Input.GetKey(VirtualKey.W) || Input.GetKey(VirtualKey.S))
            _direction += input * Entity.Transform.Up;
        // If neither the W or S key is pressed, update the direction
        // based on the global Y unit vector and the input variable.
        else
            _direction += input * Vector3.UnitY;
    }
}
