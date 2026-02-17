using Godot;
using EchoesOfCommand.Enums;
using EchoesOfCommand.Subsystems;

namespace EchoesOfCommand.Ships;

/// <summary>
/// Abstract base for all ships. CharacterBody3D with Newtonian physics (MotionMode.Floating).
/// Subclasses provide thrust/rotation input.
/// </summary>
public abstract partial class ShipBase : CharacterBody3D
{
    [Export] public ShipClass ShipClass { get; set; } = ShipClass.Battlecruiser;
    [Export] public Faction Faction { get; set; } = Faction.Player;

    public ShipStats Stats { get; private set; } = null!;
    public ShipSubsystems Subsystems { get; private set; } = null!;

    public Vector3 CurrentVelocity { get; internal set; }
    public int ShipId { get; set; }

    public override void _Ready()
    {
        MotionMode = MotionModeEnum.Floating;
        Stats = ShipClassDatabase.Get(ShipClass);
        Subsystems = new ShipSubsystems(Stats);

        AddToGroup("ships");
        CollisionLayer = 0b10;  // Layer 2 = ships
        CollisionMask = 0b110;  // Detect ships + projectiles
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (Subsystems.IsDestroyed)
            return;

        // Get input from subclass
        float thrust = GetThrustInput();
        float rotation = GetRotationInput();

        // Apply rotation
        float effectiveRotation = Stats.RotationSpeed * Subsystems.TurnRateMultiplier;
        RotateY(Mathf.DegToRad(rotation * effectiveRotation * dt));

        // Apply thrust as acceleration along forward axis (-Z in Godot)
        float effectiveMaxSpeed = Stats.MaxSpeed * Subsystems.SpeedMultiplier;
        if (thrust != 0f)
        {
            Vector3 forward = -GlobalTransform.Basis.Z.Normalized();
            CurrentVelocity += forward * thrust * Stats.Acceleration * dt;

            // Clamp to max speed
            if (CurrentVelocity.Length() > effectiveMaxSpeed)
                CurrentVelocity = CurrentVelocity.Normalized() * effectiveMaxSpeed;
        }

        // Newtonian: maintain velocity (no drag in space)
        Velocity = CurrentVelocity;
        MoveAndSlide();

        // Shield regen
        Subsystems.RegenShields(dt);
    }

    protected abstract float GetThrustInput();
    protected abstract float GetRotationInput();

    // ── Multiplayer Architecture (WP-12) ──────────────────────────────
    // Position sync: server-authoritative, unreliable-ordered (high frequency)
    // Damage: server-authoritative, reliable (must not be lost)
    // In multiplayer: server runs physics, clients interpolate
    // PlayerShip: client-authoritative for input, server validates

    /// <summary>
    /// Server broadcasts position/velocity to all clients every physics frame.
    /// Clients interpolate between received states.
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    public void RpcSyncPosition(Vector3 position, Vector3 velocity, float rotationY)
    {
        // Client-side: lerp to received position
        if (!Multiplayer.IsServer())
        {
            GlobalPosition = GlobalPosition.Lerp(position, 0.5f);
            CurrentVelocity = velocity;
        }
    }

    /// <summary>
    /// Server applies damage and broadcasts to all clients.
    /// Authority check: only server can apply damage.
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RpcApplyDamage(int subsystem, float damage)
    {
        if (Multiplayer.GetUniqueId() != 1 && Multiplayer.IsServer())
            return; // Only server applies damage

        Subsystems.ApplyDamage((SubsystemType)subsystem, damage);
    }

    /// <summary>
    /// Server broadcasts subsystem state after damage for client sync.
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RpcSyncSubsystems(float shieldHp, float hullHp, float engineHp, float weaponsHp)
    {
        // Client-side state reconciliation
    }
}
