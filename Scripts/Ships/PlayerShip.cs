using Godot;
using EchoesOfCommand.Enums;
using EchoesOfCommand.Weapons;

namespace EchoesOfCommand.Ships;

/// <summary>
/// Player-controlled flagship. WASD movement, mouse fire, 1/2/3 weapon select.
/// </summary>
public partial class PlayerShip : ShipBase
{
    [Export] public NodePath CameraPath { get; set; } = "";

    private WeaponSystem? _weaponSystem;
    private WeaponType _selectedWeapon = WeaponType.Laser;
    private Camera3D? _camera;

    public WeaponType SelectedWeapon => _selectedWeapon;

    public override void _Ready()
    {
        base._Ready();
        Faction = Faction.Player;
        _weaponSystem = GetNodeOrNull<WeaponSystem>("WeaponSystem");

        // Find camera in scene
        if (!string.IsNullOrEmpty(CameraPath))
            _camera = GetNodeOrNull<Camera3D>(CameraPath);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("weapon_select_1"))
            _selectedWeapon = WeaponType.Laser;
        else if (@event.IsActionPressed("weapon_select_2"))
            _selectedWeapon = WeaponType.Missile;
        else if (@event.IsActionPressed("weapon_select_3"))
            _selectedWeapon = WeaponType.Scattershot;

        if (@event.IsActionPressed("ship_fire"))
        {
            var target = GetMouseWorldPosition();
            if (target.HasValue)
                _weaponSystem?.Fire(_selectedWeapon, GlobalPosition, target.Value, ShipId);
        }
    }

    protected override float GetThrustInput()
    {
        float thrust = 0f;
        if (Input.IsActionPressed("ship_thrust_forward"))
            thrust += 1f;
        if (Input.IsActionPressed("ship_thrust_reverse"))
            thrust -= 1f;
        return thrust;
    }

    protected override float GetRotationInput()
    {
        float rotation = 0f;
        if (Input.IsActionPressed("ship_rotate_left"))
            rotation += 1f;
        if (Input.IsActionPressed("ship_rotate_right"))
            rotation -= 1f;
        return rotation;
    }

    private Vector3? GetMouseWorldPosition()
    {
        _camera ??= GetViewport().GetCamera3D();
        if (_camera == null)
            return null;

        var mousePos = GetViewport().GetMousePosition();
        var from = _camera.ProjectRayOrigin(mousePos);
        var direction = _camera.ProjectRayNormal(mousePos);

        // Intersect with Y=0 plane (combat plane)
        if (Mathf.Abs(direction.Y) < 0.001f)
            return null;

        float t = -from.Y / direction.Y;
        if (t < 0f)
            return null;

        return from + direction * t;
    }
}
