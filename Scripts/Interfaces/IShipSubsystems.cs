using EchoesOfCommand.Enums;

namespace EchoesOfCommand.Interfaces;

public interface IShipSubsystems
{
    (float CurrentHealth, float MaxHealth, bool Functional) GetSubsystemHealth(SubsystemType subsystem);

    bool ApplyDamage(SubsystemType subsystem, float damage);

    bool Repair(SubsystemType subsystem, float amount);
}
