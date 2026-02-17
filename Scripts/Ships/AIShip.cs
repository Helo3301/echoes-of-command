using Godot;
using EchoesOfCommand.Orders;

namespace EchoesOfCommand.Ships;

/// <summary>
/// AI-controlled fleet ship. Full behavior tree in WP-06.
/// </summary>
public partial class AIShip : ShipBase
{
    private float _desiredThrust;
    private float _desiredRotation;

    public void ReceiveOrder(Order order)
    {
        // WP-06: Process order after light-speed delay
    }

    protected override float GetThrustInput() => _desiredThrust;
    protected override float GetRotationInput() => _desiredRotation;

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RpcReceiveOrder(string orderId, int orderType, Vector3 position, int targetId)
    {
        // Stub for WP-12 multiplayer
    }
}
