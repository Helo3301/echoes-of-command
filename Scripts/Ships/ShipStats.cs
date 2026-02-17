using EchoesOfCommand.Enums;

namespace EchoesOfCommand.Ships;

public class ShipStats
{
    public required ShipClass ShipClass { get; init; }
    public required float MaxSpeed { get; init; }
    public required float Acceleration { get; init; }
    public required float RotationSpeed { get; init; }
    public required float ShieldHp { get; init; }
    public required float HullHp { get; init; }
    public required float EngineHp { get; init; }
    public required float WeaponsHp { get; init; }
    public required float ShieldRegenPerSecond { get; init; }
    public required WeaponType PrimaryWeapon { get; init; }
    public required WeaponType SecondaryWeapon { get; init; }
}
