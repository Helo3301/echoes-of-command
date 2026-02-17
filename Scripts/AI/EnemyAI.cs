using Godot;
using EchoesOfCommand.Core;
using EchoesOfCommand.Enums;
using EchoesOfCommand.Ships;
using EchoesOfCommand.Weapons;

namespace EchoesOfCommand.AI;

/// <summary>
/// Simple enemy AI: approach nearest player ship, maintain engagement range, fire weapons.
/// Attach as child of AIShip with Faction = Enemy.
/// </summary>
public partial class EnemyAI : Node
{
    private AIShip _ship = null!;
    private WeaponSystem? _weaponSystem;
    private ShipBase? _target;
    private float _retargetTimer;
    private const float RetargetInterval = 1.5f;

    public override void _Ready()
    {
        _ship = GetParent<AIShip>();
        _weaponSystem = _ship.GetNodeOrNull<WeaponSystem>("WeaponSystem");
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (_ship.Subsystems.IsDestroyed)
            return;

        _retargetTimer -= dt;
        if (_retargetTimer <= 0f)
        {
            _retargetTimer = RetargetInterval;
            UpdateTarget();
        }

        if (_target != null && IsInstanceValid(_target) && !_target.Subsystems.IsDestroyed)
        {
            float dist = _ship.GlobalPosition.DistanceTo(_target.GlobalPosition);
            float preferredRange = GetPreferredRange();

            if (dist > preferredRange * 1.1f)
                SteerToward(_target.GlobalPosition);
            else if (dist < preferredRange * 0.4f)
                SteerAway(_target.GlobalPosition);
            else
                OrbitTarget(_target.GlobalPosition);

            TryFire(dist);
        }
        else
        {
            _ship.SetSteering(0f, 0f);
        }

        AvoidAllies(dt);
    }

    private void UpdateTarget()
    {
        ShipBase? best = null;
        float bestDist = float.MaxValue;

        foreach (var node in GetTree().GetNodesInGroup("ships"))
        {
            if (node is not ShipBase ship) continue;
            if (ship.Faction == _ship.Faction) continue;
            if (ship.Subsystems.IsDestroyed) continue;

            float dist = _ship.GlobalPosition.DistanceTo(ship.GlobalPosition);
            if (dist < bestDist && dist <= GameConstants.AISensorRange)
            {
                bestDist = dist;
                best = ship;
            }
        }

        _target = best;
    }

    private void TryFire(float distance)
    {
        if (_weaponSystem == null || _target == null) return;

        WeaponType weapon;
        if (distance <= GameConstants.ScattershotEngagementRange)
            weapon = WeaponType.Scattershot;
        else if (distance <= GameConstants.LaserEngagementRange)
            weapon = WeaponType.Laser;
        else if (distance <= GameConstants.MissileEngagementRange)
            weapon = WeaponType.Missile;
        else
            return;

        _weaponSystem.Fire(weapon, _ship.GlobalPosition, _target.GlobalPosition, _ship.ShipId);
    }

    private float GetPreferredRange()
    {
        return _ship.Stats.PrimaryWeapon switch
        {
            WeaponType.Missile => GameConstants.MissileEngagementRange,
            WeaponType.Scattershot => GameConstants.ScattershotEngagementRange,
            WeaponType.Laser => GameConstants.LaserEngagementRange,
            _ => 1000f
        };
    }

    private void SteerToward(Vector3 target)
    {
        var toTarget = (target - _ship.GlobalPosition).Normalized();
        var forward = -_ship.GlobalTransform.Basis.Z.Normalized();
        float angle = forward.SignedAngleTo(toTarget, Vector3.Up);
        _ship.SetSteering(1f, Mathf.Clamp(angle * 2f, -1f, 1f));
    }

    private void SteerAway(Vector3 target)
    {
        var away = (_ship.GlobalPosition - target).Normalized();
        var forward = -_ship.GlobalTransform.Basis.Z.Normalized();
        float angle = forward.SignedAngleTo(away, Vector3.Up);
        _ship.SetSteering(1f, Mathf.Clamp(angle * 2f, -1f, 1f));
    }

    private void OrbitTarget(Vector3 target)
    {
        var toTarget = (target - _ship.GlobalPosition).Normalized();
        var tangent = toTarget.Cross(Vector3.Up).Normalized();
        var forward = -_ship.GlobalTransform.Basis.Z.Normalized();
        float angle = forward.SignedAngleTo(tangent, Vector3.Up);
        _ship.SetSteering(0.5f, Mathf.Clamp(angle * 2f, -1f, 1f));
    }

    private void AvoidAllies(float dt)
    {
        foreach (var node in GetTree().GetNodesInGroup("ships"))
        {
            if (node is not ShipBase other || other == _ship) continue;
            if (other.Faction != _ship.Faction) continue;

            float dist = _ship.GlobalPosition.DistanceTo(other.GlobalPosition);
            if (dist < GameConstants.MinEnemySpacing && dist > 0.1f)
            {
                var away = (_ship.GlobalPosition - other.GlobalPosition).Normalized();
                _ship.CurrentVelocity += away * 15f * dt;
            }
        }
    }
}
