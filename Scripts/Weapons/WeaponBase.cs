using Godot;
using EchoesOfCommand.Enums;

namespace EchoesOfCommand.Weapons;

/// <summary>
/// Abstract weapon mounted on a ship. Manages cooldown and firing.
/// Concrete implementations (MissileWeapon, etc.) in WP-04.
/// </summary>
public abstract partial class WeaponBase : Node3D
{
    [Export] public WeaponType WeaponType { get; set; }
    [Export] public float Damage { get; set; }
    [Export] public float Cooldown { get; set; }

    public int OwnerId { get; set; }

    private float _cooldownTimer;

    public bool CanFire => _cooldownTimer <= 0f;

    public override void _Process(double delta)
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= (float)delta;
    }

    public bool TryFire(Vector3 target)
    {
        if (!CanFire)
            return false;

        _cooldownTimer = Cooldown;
        OnFire(target);
        return true;
    }

    protected abstract void OnFire(Vector3 target);
}
