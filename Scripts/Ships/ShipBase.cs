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

    public Vector3 CurrentVelocity { get; protected set; }
    public int ShipId { get; set; }

    public override void _Ready()
    {
        MotionMode = MotionModeEnum.Floating;
        Stats = ShipClassDatabase.Get(ShipClass);
        Subsystems = new ShipSubsystems(Stats);
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

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    public void RpcSyncPosition(Vector3 position, Vector3 velocity, float rotation)
    {
        // Stub for WP-12 multiplayer
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RpcApplyDamage(int subsystem, float damage)
    {
        // Stub for WP-12 multiplayer
    }
}
