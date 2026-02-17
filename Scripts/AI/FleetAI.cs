using Godot;
using EchoesOfCommand.Core;
using EchoesOfCommand.Enums;
using EchoesOfCommand.Orders;
using EchoesOfCommand.Ships;
using EchoesOfCommand.Weapons;

namespace EchoesOfCommand.AI;

/// <summary>
/// AI controller for fleet ships. Handles autonomous combat, formation, and order execution.
/// Attach as child of AIShip.
/// </summary>
public partial class FleetAI : Node
{
    private enum AIState
    {
        Idle,
        Formation,
        MovingToTarget,
        Engaging,
        HoldingPosition,
        Following,
        Defending
    }

    private AIShip _ship = null!;
    private WeaponSystem? _weaponSystem;
    private AIState _state = AIState.Formation;
    private Order? _currentOrder;

    // Targeting
    private ShipBase? _combatTarget;
    private float _retargetTimer;
    private const float RetargetInterval = 1f;

    // Formation
    private ShipBase? _flagship;
    private int _formationIndex;
    public int FormationIndex { get => _formationIndex; set => _formationIndex = value; }

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

        ProcessOrder();
        ExecuteState(dt);
        AvoidCollisions(dt);
        TryFireWeapons();
    }

    public void OnOrderReceived(Order order)
    {
        _currentOrder = order;
        _state = order.Type switch
        {
            OrderType.MoveTo => AIState.MovingToTarget,
            OrderType.AttackTarget => AIState.Engaging,
            OrderType.HoldPosition => AIState.HoldingPosition,
            OrderType.FollowShip => AIState.Following,
            OrderType.Defend => AIState.Defending,
            _ => AIState.Formation
        };
    }

    public void SetFlagship(ShipBase flagship)
    {
        _flagship = flagship;
    }

    private void ProcessOrder()
    {
        if (_currentOrder == null && _combatTarget == null)
            _state = AIState.Formation;
    }

    private void ExecuteState(float dt)
    {
        switch (_state)
        {
            case AIState.Formation:
                ExecuteFormation(dt);
                break;
            case AIState.MovingToTarget:
                ExecuteMoveTo(dt);
                break;
            case AIState.Engaging:
                ExecuteEngage(dt);
                break;
            case AIState.HoldingPosition:
                ExecuteHoldPosition(dt);
                break;
            case AIState.Following:
                ExecuteFollow(dt);
                break;
            case AIState.Defending:
                ExecuteDefend(dt);
                break;
            case AIState.Idle:
            default:
                break;
        }
    }

    private void ExecuteFormation(float dt)
    {
        if (_flagship == null) return;

        // V-formation behind flagship
        var flagPos = _flagship.GlobalPosition;
        var flagForward = -_flagship.GlobalTransform.Basis.Z.Normalized();
        var flagRight = _flagship.GlobalTransform.Basis.X.Normalized();

        float spacing = GameConstants.FormationSpacing;
        int side = (_formationIndex % 2 == 0) ? -1 : 1;
        int rank = (_formationIndex / 2) + 1;

        var targetPos = flagPos
            - flagForward * spacing * rank
            + flagRight * side * spacing * rank * 0.5f;

        // If enemies nearby, break formation and engage
        if (_combatTarget != null)
        {
            _state = AIState.Engaging;
            return;
        }

        SteerToward(targetPos, dt);
    }

    private void ExecuteMoveTo(float dt)
    {
        if (_currentOrder?.TargetPosition == null)
        {
            _state = AIState.Formation;
            return;
        }

        var target = _currentOrder.TargetPosition.Value;
        float dist = _ship.GlobalPosition.DistanceTo(target);

        if (dist < 50f)
        {
            _currentOrder = null;
            _state = AIState.Formation;
            return;
        }

        SteerToward(target, dt);
    }

    private void ExecuteEngage(float dt)
    {
        if (_combatTarget == null || !IsInstanceValid(_combatTarget) || _combatTarget.Subsystems.IsDestroyed)
        {
            _combatTarget = null;
            _state = _currentOrder != null ? AIState.Engaging : AIState.Formation;
            UpdateTarget();
            if (_combatTarget == null)
            {
                _currentOrder = null;
                _state = AIState.Formation;
            }
            return;
        }

        // Determine engagement range based on weapon loadout
        float preferredRange = GetPreferredRange();
        float dist = _ship.GlobalPosition.DistanceTo(_combatTarget.GlobalPosition);

        if (dist > preferredRange * 1.2f)
            SteerToward(_combatTarget.GlobalPosition, dt);
        else if (dist < preferredRange * 0.5f)
            SteerAway(_combatTarget.GlobalPosition, dt);
        else
            OrbitTarget(_combatTarget.GlobalPosition, preferredRange, dt);
    }

    private void ExecuteHoldPosition(float dt)
    {
        if (_currentOrder?.TargetPosition == null) return;

        var holdPos = _currentOrder.TargetPosition.Value;
        float dist = _ship.GlobalPosition.DistanceTo(holdPos);

        if (dist > GameConstants.HoldPositionRadius)
            SteerToward(holdPos, dt);

        // Still engage enemies that come within range
        if (_combatTarget != null)
        {
            float enemyDist = holdPos.DistanceTo(_combatTarget.GlobalPosition);
            if (enemyDist > GameConstants.AISensorRange)
                _combatTarget = null;
        }
    }

    private void ExecuteFollow(float dt)
    {
        ShipBase? followTarget = null;
        if (_currentOrder?.TargetShipId != null)
            followTarget = FindShipById(_currentOrder.TargetShipId.Value);

        followTarget ??= _flagship;

        if (followTarget == null) return;

        var behind = followTarget.GlobalPosition
            + followTarget.GlobalTransform.Basis.Z.Normalized() * 200f;
        SteerToward(behind, dt);
    }

    private void ExecuteDefend(float dt)
    {
        // Defend = hold position + prioritize nearby enemies
        ExecuteHoldPosition(dt);
    }

    private void UpdateTarget()
    {
        ShipBase? best = null;
        float bestScore = float.MaxValue;

        foreach (var node in GetTree().GetNodesInGroup("ships"))
        {
            if (node is not ShipBase ship) continue;
            if (ship.Faction == _ship.Faction) continue;
            if (ship.Subsystems.IsDestroyed) continue;

            float dist = _ship.GlobalPosition.DistanceTo(ship.GlobalPosition);
            if (dist > GameConstants.AISensorRange) continue;

            // Score: prefer damaged enemies, then closest
            float healthRatio = 1f;
            var (hullHp, hullMax, _) = ship.Subsystems.GetSubsystemHealth(SubsystemType.Hull);
            healthRatio = hullHp / hullMax;

            float score = dist * healthRatio; // Lower = better target
            if (score < bestScore)
            {
                bestScore = score;
                best = ship;
            }
        }

        // If we have an order to attack a specific target, prefer that
        if (_currentOrder?.Type == OrderType.AttackTarget && _currentOrder.TargetShipId != null)
        {
            var ordered = FindShipById(_currentOrder.TargetShipId.Value);
            if (ordered != null && !ordered.Subsystems.IsDestroyed)
                best = ordered;
        }

        _combatTarget = best;
    }

    private void TryFireWeapons()
    {
        if (_weaponSystem == null || _combatTarget == null) return;
        if (!IsInstanceValid(_combatTarget)) return;

        float dist = _ship.GlobalPosition.DistanceTo(_combatTarget.GlobalPosition);

        // Choose weapon based on range
        WeaponType weapon;
        if (dist <= GameConstants.ScattershotEngagementRange)
            weapon = WeaponType.Scattershot;
        else if (dist <= GameConstants.LaserEngagementRange)
            weapon = WeaponType.Laser;
        else if (dist <= GameConstants.MissileEngagementRange)
            weapon = WeaponType.Missile;
        else
            return;

        _weaponSystem.Fire(weapon, _ship.GlobalPosition, _combatTarget.GlobalPosition, _ship.ShipId);
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

    private void SteerToward(Vector3 target, float dt)
    {
        var toTarget = (target - _ship.GlobalPosition).Normalized();
        var forward = -_ship.GlobalTransform.Basis.Z.Normalized();
        float angle = forward.SignedAngleTo(toTarget, Vector3.Up);

        _ship.SetSteering(
            thrust: 1f,
            rotation: Mathf.Clamp(angle * 2f, -1f, 1f)
        );
    }

    private void SteerAway(Vector3 target, float dt)
    {
        var away = (_ship.GlobalPosition - target).Normalized();
        var forward = -_ship.GlobalTransform.Basis.Z.Normalized();
        float angle = forward.SignedAngleTo(away, Vector3.Up);

        _ship.SetSteering(
            thrust: 1f,
            rotation: Mathf.Clamp(angle * 2f, -1f, 1f)
        );
    }

    private void OrbitTarget(Vector3 target, float radius, float dt)
    {
        var toTarget = (target - _ship.GlobalPosition).Normalized();
        var tangent = toTarget.Cross(Vector3.Up).Normalized();
        var forward = -_ship.GlobalTransform.Basis.Z.Normalized();
        float angle = forward.SignedAngleTo(tangent, Vector3.Up);

        _ship.SetSteering(
            thrust: 0.5f,
            rotation: Mathf.Clamp(angle * 2f, -1f, 1f)
        );
    }

    private void AvoidCollisions(float dt)
    {
        foreach (var node in GetTree().GetNodesInGroup("ships"))
        {
            if (node is not ShipBase other || other == _ship) continue;
            if (other.Faction != _ship.Faction) continue;

            float dist = _ship.GlobalPosition.DistanceTo(other.GlobalPosition);
            if (dist < GameConstants.MinAllySpacing && dist > 0.1f)
            {
                var away = (_ship.GlobalPosition - other.GlobalPosition).Normalized();
                _ship.CurrentVelocity += away * 20f * dt;
            }
        }
    }

    private ShipBase? FindShipById(int id)
    {
        foreach (var node in GetTree().GetNodesInGroup("ships"))
        {
            if (node is ShipBase ship && ship.ShipId == id)
                return ship;
        }
        return null;
    }
}
