namespace EchoesOfCommand.Core;

public static class GameConstants
{
    // Light speed (m/s) - scaled for gameplay (real c = 299,792,458)
    public const float SpeedOfLight = 300f;

    // Sensor range (meters)
    public const float SensorRange = 5000f;

    // Weapon stats: (Damage, Speed m/s, Cooldown s, Range m, ProjectileCount, TurnRate deg/s, Lifetime s)
    public const float LaserDamage = 50f;
    public const float LaserCooldown = 0.3f;
    public const float LaserBeamDuration = 0.2f;

    public const float MissileDamage = 150f;
    public const float MissileSpeed = 200f;
    public const float MissileCooldown = 2f;
    public const float MissileTurnRate = 45f;
    public const float MissileLifetime = 10f;

    public const float ScattershotDamage = 30f;
    public const float ScattershotSpeed = 400f;
    public const float ScattershotCooldown = 1.5f;
    public const float ScattershotRange = 800f;
    public const float ScattershotConeAngle = 15f;
    public const int ScattershotPelletCount = 5;
    public const float ScattershotLifetime = 3f;

    // Shield regen is per-ship (varies by class), see ShipClassDatabase
    // Formation
    public const float FormationSpacing = 500f;
    public const float MinAllySpacing = 100f;

    // Engagement ranges
    public const float MissileEngagementRange = 1500f;
    public const float ScattershotEngagementRange = 600f;
    public const float LaserEngagementRange = 1000f;
    public const float AISensorRange = 3000f;

    // AI defaults
    public const float HoldPositionRadius = 200f;
    public const float MinEnemySpacing = 150f;
}
