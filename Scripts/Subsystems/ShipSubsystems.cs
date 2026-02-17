using EchoesOfCommand.Enums;
using EchoesOfCommand.Interfaces;
using EchoesOfCommand.Ships;

namespace EchoesOfCommand.Subsystems;

/// <summary>
/// Manages the four subsystems of a ship. Pure C# — no Godot dependency, fully unit-testable.
/// Shields absorb damage first for non-shield subsystems.
/// </summary>
public class ShipSubsystems : IShipSubsystems
{
    private readonly Dictionary<SubsystemType, Subsystem> _subsystems;
    public float ShieldRegenPerSecond { get; }

    public bool IsDestroyed => !_subsystems[SubsystemType.Hull].Functional;

    public event Action<SubsystemType>? SubsystemDestroyed;
    public event Action? ShipDestroyed;

    public ShipSubsystems(ShipStats stats)
    {
        ShieldRegenPerSecond = stats.ShieldRegenPerSecond;
        _subsystems = new Dictionary<SubsystemType, Subsystem>
        {
            [SubsystemType.Shield] = new(SubsystemType.Shield, stats.ShieldHp),
            [SubsystemType.Hull] = new(SubsystemType.Hull, stats.HullHp),
            [SubsystemType.Engine] = new(SubsystemType.Engine, stats.EngineHp),
            [SubsystemType.Weapons] = new(SubsystemType.Weapons, stats.WeaponsHp)
        };
    }

    public (float CurrentHealth, float MaxHealth, bool Functional) GetSubsystemHealth(SubsystemType subsystem)
    {
        var sub = _subsystems[subsystem];
        return (sub.CurrentHealth, sub.MaxHealth, sub.Functional);
    }

    /// <summary>
    /// Apply damage to a subsystem. For non-shield subsystems, shields absorb damage first.
    /// Returns true if the targeted subsystem was destroyed.
    /// </summary>
    public bool ApplyDamage(SubsystemType subsystem, float damage)
    {
        if (damage <= 0f)
            return false;

        var target = _subsystems[subsystem];

        // For non-shield targets, shields absorb damage first
        if (subsystem != SubsystemType.Shield)
        {
            var shield = _subsystems[SubsystemType.Shield];
            if (shield.Functional)
            {
                float absorbed = MathF.Min(shield.CurrentHealth, damage);
                shield.TakeDamage(absorbed);
                damage -= absorbed;

                if (shield.CurrentHealth <= 0f)
                    SubsystemDestroyed?.Invoke(SubsystemType.Shield);

                if (damage <= 0f)
                    return false;
            }
        }

        bool destroyed = target.TakeDamage(damage);

        if (destroyed)
        {
            SubsystemDestroyed?.Invoke(subsystem);

            if (subsystem == SubsystemType.Hull)
                ShipDestroyed?.Invoke();
        }

        return destroyed;
    }

    public bool Repair(SubsystemType subsystem, float amount)
    {
        return _subsystems[subsystem].Repair(amount);
    }

    /// <summary>
    /// Tick shield regeneration. Call each frame with delta time.
    /// </summary>
    public void RegenShields(float deltaTime)
    {
        var shield = _subsystems[SubsystemType.Shield];
        if (shield.Functional && shield.CurrentHealth < shield.MaxHealth)
        {
            shield.Repair(ShieldRegenPerSecond * deltaTime);
        }
    }

    public bool IsSubsystemFunctional(SubsystemType subsystem) => _subsystems[subsystem].Functional;

    /// <summary>
    /// Speed multiplier based on engine status. 0.5 if destroyed.
    /// </summary>
    public float SpeedMultiplier => _subsystems[SubsystemType.Engine].Functional ? 1f : 0.5f;

    /// <summary>
    /// Turn rate multiplier based on engine status. 0.3 if destroyed.
    /// </summary>
    public float TurnRateMultiplier => _subsystems[SubsystemType.Engine].Functional ? 1f : 0.3f;

    /// <summary>
    /// Whether weapons can fire.
    /// </summary>
    public bool CanFire => _subsystems[SubsystemType.Weapons].Functional;
}
