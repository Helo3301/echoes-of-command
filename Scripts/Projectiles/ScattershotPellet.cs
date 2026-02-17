using Godot;
using EchoesOfCommand.Core;
using EchoesOfCommand.Ships;

namespace EchoesOfCommand.Projectiles;

/// <summary>
/// Single scattershot pellet. Travels straight, despawns after range/time limit.
/// </summary>
public partial class ScattershotPellet : ProjectileBase
{
    private Vector3 _direction;
    private float _distanceTraveled;

    public void SetDirection(Vector3 direction)
    {
        _direction = direction.Normalized();
    }

    public override void _Ready()
    {
        Speed = GameConstants.ScattershotSpeed;
        Lifetime = GameConstants.ScattershotLifetime;
        Damage = GameConstants.ScattershotDamage;

        var shape = new SphereShape3D();
        shape.Radius = 1f;
        var collider = new CollisionShape3D();
        collider.Shape = shape;
        AddChild(collider);

        CollisionLayer = 0b100;
        CollisionMask = 0b10;

        BodyEntered += OnBodyEntered;
    }

    protected override void OnMove(float delta)
    {
        float step = Speed * delta;
        GlobalPosition += _direction * step;
        _distanceTraveled += step;

        if (_distanceTraveled >= GameConstants.ScattershotRange)
            QueueFree();
    }

    private void OnBodyEntered(Node body)
    {
        if (body is ShipBase ship && ship.ShipId != OwnerId)
        {
            ship.Subsystems.ApplyDamage(Enums.SubsystemType.Hull, Damage);
            QueueFree();
        }
    }
}
