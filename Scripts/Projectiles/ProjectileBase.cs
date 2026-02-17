using Godot;

namespace EchoesOfCommand.Projectiles;

/// <summary>
/// Abstract projectile using Area3D for collision detection.
/// Concrete implementations (Missile, ScattershotPellet) in WP-04.
/// </summary>
public abstract partial class ProjectileBase : Area3D
{
    [Export] public float Speed { get; set; }
    [Export] public float Lifetime { get; set; }
    [Export] public float Damage { get; set; }

    public int OwnerId { get; set; }
    public int ProjectileId { get; set; }

    private float _age;

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _age += dt;

        if (_age >= Lifetime)
        {
            QueueFree();
            return;
        }

        OnMove(dt);
    }

    protected abstract void OnMove(float delta);
}
