using Godot;
using EchoesOfCommand.Core;
using EchoesOfCommand.Enums;

namespace EchoesOfCommand.Weapons;

/// <summary>
/// Hitscan laser — instant hit via raycast, beam visual for 0.2s.
/// </summary>
public partial class LaserWeapon : WeaponBase
{
    private RayCast3D? _ray;
    private MeshInstance3D? _beamVisual;
    private float _beamTimer;

    public override void _Ready()
    {
        WeaponType = WeaponType.Laser;
        Damage = GameConstants.LaserDamage;
        Cooldown = GameConstants.LaserCooldown;

        _ray = new RayCast3D();
        _ray.Enabled = false;
        _ray.CollideWithAreas = true;
        _ray.CollideWithBodies = true;
        _ray.CollisionMask = 0b10; // Layer 2 = ships
        AddChild(_ray);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Fade beam visual
        if (_beamVisual != null && _beamTimer > 0f)
        {
            _beamTimer -= (float)delta;
            if (_beamTimer <= 0f)
            {
                _beamVisual.QueueFree();
                _beamVisual = null;
            }
        }
    }

    protected override void OnFire(Vector3 target)
    {
        // Raycast to target
        var direction = (target - GlobalPosition).Normalized();
        _ray!.TargetPosition = direction * 10000f;
        _ray.Enabled = true;
        _ray.ForceRaycastUpdate();

        Vector3 hitPoint = target;
        if (_ray.IsColliding())
            hitPoint = _ray.GetCollisionPoint();

        _ray.Enabled = false;

        // Create beam visual
        CreateBeamVisual(GlobalPosition, hitPoint);
    }

    private void CreateBeamVisual(Vector3 from, Vector3 to)
    {
        _beamVisual?.QueueFree();

        var midpoint = (from + to) / 2f;
        float length = from.DistanceTo(to);

        _beamVisual = new MeshInstance3D();
        var cylinder = new CylinderMesh();
        cylinder.TopRadius = 0.3f;
        cylinder.BottomRadius = 0.3f;
        cylinder.Height = length;

        var mat = new StandardMaterial3D();
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        mat.AlbedoColor = new Color(0.2f, 0.8f, 1.0f, 0.8f);
        mat.EmissionEnabled = true;
        mat.Emission = new Color(0.2f, 0.8f, 1.0f);
        mat.EmissionEnergyMultiplier = 3f;
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        cylinder.Material = mat;

        _beamVisual.Mesh = cylinder;
        _beamVisual.GlobalPosition = midpoint;

        // Orient beam toward target
        var direction = (to - from).Normalized();
        if (direction != Vector3.Zero)
            _beamVisual.LookAt(to, Vector3.Up);
        _beamVisual.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2f);

        GetTree().Root.AddChild(_beamVisual);
        _beamTimer = GameConstants.LaserBeamDuration;
    }
}
