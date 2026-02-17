using Godot;
using EchoesOfCommand.Enums;

namespace EchoesOfCommand.Interfaces;

public interface IWeaponSystem
{
    (bool Success, List<int> ProjectileIds) Fire(
        WeaponType weaponType,
        Vector3 origin,
        Vector3 target,
        int ownerId);

    (bool Hit, SubsystemType SubsystemDamaged, float DamageAmount) CheckHit(
        int projectileId,
        int shipId);
}
