using Godot;

namespace EchoesOfCommand.Ships;

/// <summary>
/// Player-controlled flagship. Full input handling in WP-08.
/// </summary>
public partial class PlayerShip : ShipBase
{
    protected override float GetThrustInput()
    {
        // WP-08: WASD input
        return 0f;
    }

    protected override float GetRotationInput()
    {
        // WP-08: A/D rotation
        return 0f;
    }
}
