using Godot;
using EchoesOfCommand.Enums;
using EchoesOfCommand.Interfaces;
using EchoesOfCommand.Ships;

namespace EchoesOfCommand.Weapons;

/// <summary>
/// Manages weapons for a ship. Coordinates firing and tracks projectiles.
/// Attach as child of a ShipBase.
/// </summary>
public partial class WeaponSystem : Node, IWeaponSystem
{
    private ShipBase? _ship;
    private readonly Dictionary<WeaponType, WeaponBase> _weapons = new();
    private int _nextProjectileId;

    public override void _Ready()
    {
        _ship = GetParent<ShipBase>();

        // Discover weapon children
        foreach (var child in GetChildren())
        {
            if (child is WeaponBase weapon)
            {
                weapon.OwnerId = _ship?.ShipId ?? 0;
                _weapons[weapon.WeaponType] = weapon;
            }
        }
    }

    public (bool Success, List<int> ProjectileIds) Fire(
        WeaponType weaponType,
        Vector3 origin,
        Vector3 target,
        int ownerId)
    {
        // Check if ship weapons subsystem is functional
        if (_ship != null && !_ship.Subsystems.CanFire)
            return (false, new List<int>());

        if (!_weapons.TryGetValue(weaponType, out var weapon))
            return (false, new List<int>());

        if (!weapon.TryFire(target))
            return (false, new List<int>());

        var ids = new List<int> { _nextProjectileId++ };

        // Scattershot creates multiple
        if (weaponType == WeaponType.Scattershot)
        {
            for (int i = 1; i < Core.GameConstants.ScattershotPelletCount; i++)
                ids.Add(_nextProjectileId++);
        }

        return (true, ids);
    }

    public (bool Hit, SubsystemType SubsystemDamaged, float DamageAmount) CheckHit(
        int projectileId,
        int shipId)
    {
        // Hit detection handled by Area3D signals on projectiles
        // This method is for manual/external checks
        return (false, SubsystemType.Hull, 0f);
    }

    public bool CanFire(WeaponType type)
    {
        if (_ship != null && !_ship.Subsystems.CanFire)
            return false;

        return _weapons.TryGetValue(type, out var weapon) && weapon.CanFire;
    }

    public float GetCooldownRemaining(WeaponType type)
    {
        if (!_weapons.TryGetValue(type, out var weapon))
            return 0f;

        return weapon.CanFire ? 0f : weapon.Cooldown;
    }
}
