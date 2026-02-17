using EchoesOfCommand.Enums;

namespace EchoesOfCommand.Subsystems;

public class Subsystem
{
    public SubsystemType Type { get; }
    public float MaxHealth { get; }
    public float CurrentHealth { get; private set; }
    public bool Functional => CurrentHealth > 0f;

    public Subsystem(SubsystemType type, float maxHealth)
    {
        Type = type;
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }

    /// <summary>
    /// Apply damage. Returns true if this subsystem was destroyed by this hit.
    /// </summary>
    public bool TakeDamage(float damage)
    {
        if (damage <= 0f || !Functional)
            return false;

        bool wasFunctional = Functional;
        CurrentHealth = MathF.Max(0f, CurrentHealth - damage);
        return wasFunctional && !Functional;
    }

    /// <summary>
    /// Repair this subsystem by the given amount. Returns true if repair was applied.
    /// </summary>
    public bool Repair(float amount)
    {
        if (amount <= 0f || CurrentHealth >= MaxHealth)
            return false;

        CurrentHealth = MathF.Min(MaxHealth, CurrentHealth + amount);
        return true;
    }

    public void Reset()
    {
        CurrentHealth = MaxHealth;
    }
}
