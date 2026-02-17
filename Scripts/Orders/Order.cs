using Godot;
using EchoesOfCommand.Enums;

namespace EchoesOfCommand.Orders;

public class Order
{
    public string OrderId { get; } = System.Guid.NewGuid().ToString();
    public OrderType Type { get; init; }
    public Vector3? TargetPosition { get; init; }
    public int? TargetShipId { get; init; }
    public double IssuedAtTime { get; init; }
    public double ReceiveAtTime { get; init; }
    public bool Received { get; set; }
}
