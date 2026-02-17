using EchoesOfCommand.Enums;

namespace EchoesOfCommand.Ships;

public static class ShipClassDatabase
{
    private static readonly Dictionary<ShipClass, ShipStats> Stats = new()
    {
        [ShipClass.Battlecruiser] = new ShipStats
        {
            ShipClass = ShipClass.Battlecruiser,
            MaxSpeed = 400f,
            Acceleration = 50f,
            RotationSpeed = 30f,
            ShieldHp = 200f,
            HullHp = 300f,
            EngineHp = 150f,
            WeaponsHp = 100f,
            ShieldRegenPerSecond = 5f,
            PrimaryWeapon = WeaponType.Laser,
            SecondaryWeapon = WeaponType.Missile
        },
        [ShipClass.Battleship] = new ShipStats
        {
            ShipClass = ShipClass.Battleship,
            MaxSpeed = 250f,
            Acceleration = 25f,
            RotationSpeed = 15f,
            ShieldHp = 350f,
            HullHp = 500f,
            EngineHp = 200f,
            WeaponsHp = 150f,
            ShieldRegenPerSecond = 8f,
            PrimaryWeapon = WeaponType.Laser,
            SecondaryWeapon = WeaponType.Scattershot
        },
        [ShipClass.HeavyCruiser] = new ShipStats
        {
            ShipClass = ShipClass.HeavyCruiser,
            MaxSpeed = 350f,
            Acceleration = 40f,
            RotationSpeed = 25f,
            ShieldHp = 200f,
            HullHp = 300f,
            EngineHp = 150f,
            WeaponsHp = 100f,
            ShieldRegenPerSecond = 5f,
            PrimaryWeapon = WeaponType.Laser,
            SecondaryWeapon = WeaponType.Missile
        },
        [ShipClass.LightCruiser] = new ShipStats
        {
            ShipClass = ShipClass.LightCruiser,
            MaxSpeed = 420f,
            Acceleration = 55f,
            RotationSpeed = 40f,
            ShieldHp = 150f,
            HullHp = 200f,
            EngineHp = 120f,
            WeaponsHp = 80f,
            ShieldRegenPerSecond = 4f,
            PrimaryWeapon = WeaponType.Scattershot,
            SecondaryWeapon = WeaponType.Laser
        },
        [ShipClass.Destroyer] = new ShipStats
        {
            ShipClass = ShipClass.Destroyer,
            MaxSpeed = 500f,
            Acceleration = 70f,
            RotationSpeed = 50f,
            ShieldHp = 80f,
            HullHp = 120f,
            EngineHp = 100f,
            WeaponsHp = 60f,
            ShieldRegenPerSecond = 2f,
            PrimaryWeapon = WeaponType.Missile,
            SecondaryWeapon = WeaponType.Scattershot
        }
    };

    public static ShipStats Get(ShipClass shipClass) => Stats[shipClass];
}
