using Godot;
using EchoesOfCommand.Core;
using EchoesOfCommand.Enums;
using EchoesOfCommand.Projectiles;

namespace EchoesOfCommand.Weapons;

/// <summary>
/// Fires a cone of pellets (scattershot/shotgun).
/// </summary>
public partial class ScattershotWeapon : WeaponBase
{
    public override void _Ready()
    {
        WeaponType = WeaponType.Scattershot;
        Damage = GameConstants.ScattershotDamage;
        Cooldown = GameConstants.ScattershotCooldown;
    }

    protected override void OnFire(Vector3 target)
    {
        var baseDirection = (target - GlobalPosition).Normalized();
        float halfCone = Mathf.DegToRad(GameConstants.ScattershotConeAngle / 2f);

        for (int i = 0; i < GameConstants.ScattershotPelletCount; i++)
        {
            var pellet = new ScattershotPellet();
            pellet.OwnerId = OwnerId;
            pellet.GlobalPosition = GlobalPosition;

            // Spread pellets across the cone
            float t = GameConstants.ScattershotPelletCount > 1
                ? (float)i / (GameConstants.ScattershotPelletCount - 1) - 0.5f
                : 0f;
            float angle = t * 2f * halfCone;

            var direction = baseDirection.Rotated(Vector3.Up, angle);
            pellet.SetDirection(direction);

            GetTree().Root.AddChild(pellet);
        }
    }
}
