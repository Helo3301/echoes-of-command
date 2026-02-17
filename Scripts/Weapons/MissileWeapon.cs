using Godot;
using EchoesOfCommand.Core;
using EchoesOfCommand.Enums;
using EchoesOfCommand.Projectiles;
using EchoesOfCommand.Ships;

namespace EchoesOfCommand.Weapons;

/// <summary>
/// Fires homing missile projectiles.
/// </summary>
public partial class MissileWeapon : WeaponBase
{
    public override void _Ready()
    {
        WeaponType = WeaponType.Missile;
        Damage = GameConstants.MissileDamage;
        Cooldown = GameConstants.MissileCooldown;
    }

    protected override void OnFire(Vector3 target)
    {
        var missile = new MissileProjectile();
        missile.OwnerId = OwnerId;
        missile.GlobalPosition = GlobalPosition;

        // Orient toward target
        var direction = (target - GlobalPosition).Normalized();
        if (direction != Vector3.Zero)
            missile.LookAt(GlobalPosition + direction, Vector3.Up);

        // Try to find a ship at/near the target for homing
        missile.Target = FindNearestEnemyNear(target);

        GetTree().Root.AddChild(missile);
    }

    private ShipBase? FindNearestEnemyNear(Vector3 position)
    {
        ShipBase? closest = null;
        float closestDist = 200f; // Lock-on radius

        foreach (var node in GetTree().GetNodesInGroup("ships"))
        {
            if (node is ShipBase ship && ship.ShipId != OwnerId)
            {
                float dist = ship.GlobalPosition.DistanceTo(position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = ship;
                }
            }
        }
        return closest;
    }
}
