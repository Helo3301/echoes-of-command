using Godot;
using EchoesOfCommand.Core;
using EchoesOfCommand.Ships;

namespace EchoesOfCommand.Projectiles;

/// <summary>
/// Homing missile projectile. Tracks target within turn rate limit.
/// </summary>
public partial class MissileProjectile : ProjectileBase
{
    [Export] public float TurnRate { get; set; } = GameConstants.MissileTurnRate;

    public ShipBase? Target { get; set; }

    public override void _Ready()
    {
        Speed = GameConstants.MissileSpeed;
        Lifetime = GameConstants.MissileLifetime;
        Damage = GameConstants.MissileDamage;

        // Set up collision
        var shape = new SphereShape3D();
        shape.Radius = 2f;
        var collider = new CollisionShape3D();
        collider.Shape = shape;
        AddChild(collider);

        CollisionLayer = 0b100; // Layer 3 = projectiles
        CollisionMask = 0b10;   // Detect layer 2 = ships

        BodyEntered += OnBodyEntered;
    }

    protected override void OnMove(float delta)
    {
        if (Target != null && IsInstanceValid(Target))
        {
            // Steer toward target within turn rate
            var toTarget = (Target.GlobalPosition - GlobalPosition).Normalized();
            var forward = -GlobalTransform.Basis.Z.Normalized();

            float maxTurn = Mathf.DegToRad(TurnRate) * delta;
            var newForward = forward.Slerp(toTarget, Mathf.Min(1f, maxTurn / forward.AngleTo(toTarget)));

            if (newForward != Vector3.Zero)
                LookAt(GlobalPosition + newForward, Vector3.Up);
        }

        // Move forward
        var direction = -GlobalTransform.Basis.Z.Normalized();
        GlobalPosition += direction * Speed * delta;
    }

    private void OnBodyEntered(Node body)
    {
        if (body is ShipBase ship && ship.ShipId != OwnerId)
        {
            // Apply damage — target a random non-hull subsystem, or hull
            ship.Subsystems.ApplyDamage(Enums.SubsystemType.Hull, Damage);
            QueueFree();
        }
    }
}
