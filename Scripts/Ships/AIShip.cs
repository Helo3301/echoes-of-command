using Godot;
using EchoesOfCommand.Orders;

namespace EchoesOfCommand.Ships;

/// <summary>
/// AI-controlled fleet ship. Steering set by FleetAI controller.
/// </summary>
public partial class AIShip : ShipBase
{
    private float _desiredThrust;
    private float _desiredRotation;

    public event System.Action<Order>? OrderReceived;

    public void SetSteering(float thrust, float rotation)
    {
        _desiredThrust = thrust;
        _desiredRotation = rotation;
    }

    public void ReceiveOrder(Order order)
    {
        OrderReceived?.Invoke(order);
    }

    protected override float GetThrustInput() => _desiredThrust;
    protected override float GetRotationInput() => _desiredRotation;

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RpcReceiveOrder(string orderId, int orderType, Vector3 position, int targetId)
    {
        // Stub for WP-12 multiplayer
    }
}
