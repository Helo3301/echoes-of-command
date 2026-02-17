using Godot;

namespace EchoesOfCommand.Camera;

/// <summary>
/// Top-down strategic camera with pan (WASD/arrows), zoom (scroll), rotate (Q/E/middle-mouse drag).
/// Attached to a Node3D that acts as the pivot point.
/// </summary>
public partial class StrategicCamera : Node3D
{
    [ExportGroup("Movement")]
    [Export] public float PanSpeed { get; set; } = 50f;
    [Export] public float PanAcceleration { get; set; } = 8f;

    [ExportGroup("Zoom")]
    [Export] public float MinZoom { get; set; } = 100f;
    [Export] public float MaxZoom { get; set; } = 5000f;
    [Export] public float ZoomSpeed { get; set; } = 50f;
    [Export] public float ZoomSmoothness { get; set; } = 8f;

    [ExportGroup("Rotation")]
    [Export] public float RotateSpeed { get; set; } = 60f;
    [Export] public float RotateSmoothness { get; set; } = 8f;
    [Export] public float MouseRotateSensitivity { get; set; } = 0.3f;

    private Camera3D _camera = null!;
    private float _targetZoom;
    private float _currentZoom;
    private Vector3 _panVelocity;
    private float _targetRotationY;
    private bool _middleMouseDragging;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        _currentZoom = _camera.Position.Y;
        _targetZoom = _currentZoom;
        _targetRotationY = Rotation.Y;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Zoom with mouse wheel
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                _targetZoom = Mathf.Max(MinZoom, _targetZoom - ZoomSpeed);
                GetViewport().SetInputAsHandled();
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                _targetZoom = Mathf.Min(MaxZoom, _targetZoom + ZoomSpeed);
                GetViewport().SetInputAsHandled();
            }
            else if (mouseButton.ButtonIndex == MouseButton.Middle)
            {
                _middleMouseDragging = mouseButton.Pressed;
                GetViewport().SetInputAsHandled();
            }
        }

        // Rotate with middle mouse drag
        if (@event is InputEventMouseMotion mouseMotion && _middleMouseDragging)
        {
            _targetRotationY -= Mathf.DegToRad(mouseMotion.Relative.X * MouseRotateSensitivity);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        HandlePan(dt);
        HandleKeyboardRotation(dt);
        SmoothZoom(dt);
        SmoothRotation(dt);
    }

    private void HandlePan(float dt)
    {
        var input = Vector3.Zero;

        if (Input.IsActionPressed("camera_pan_forward"))
            input.Z -= 1f;
        if (Input.IsActionPressed("camera_pan_back"))
            input.Z += 1f;
        if (Input.IsActionPressed("camera_pan_left"))
            input.X -= 1f;
        if (Input.IsActionPressed("camera_pan_right"))
            input.X += 1f;

        if (input != Vector3.Zero)
            input = input.Normalized();

        // Scale pan speed with zoom level so it feels consistent
        float zoomFactor = _currentZoom / 500f;
        Vector3 targetVelocity = input * PanSpeed * zoomFactor;

        _panVelocity = _panVelocity.Lerp(targetVelocity, PanAcceleration * dt);

        // Pan relative to camera rotation
        var rotatedVelocity = _panVelocity.Rotated(Vector3.Up, Rotation.Y);
        GlobalPosition += rotatedVelocity * dt;
    }

    private void HandleKeyboardRotation(float dt)
    {
        if (Input.IsActionPressed("camera_rotate_left"))
            _targetRotationY += Mathf.DegToRad(RotateSpeed) * dt;
        if (Input.IsActionPressed("camera_rotate_right"))
            _targetRotationY -= Mathf.DegToRad(RotateSpeed) * dt;
    }

    private void SmoothZoom(float dt)
    {
        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, ZoomSmoothness * dt);
        var camPos = _camera.Position;
        camPos.Y = _currentZoom;
        // Pull back Z proportionally for a slight angle
        camPos.Z = _currentZoom * 0.3f;
        _camera.Position = camPos;
    }

    private void SmoothRotation(float dt)
    {
        var rot = Rotation;
        rot.Y = Mathf.Lerp(rot.Y, _targetRotationY, RotateSmoothness * dt);
        Rotation = rot;
    }
}
