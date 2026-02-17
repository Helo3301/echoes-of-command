using Godot;
using EchoesOfCommand.Core;
using EchoesOfCommand.Enums;
using EchoesOfCommand.Interfaces;
using EchoesOfCommand.Ships;

namespace EchoesOfCommand.Orders;

/// <summary>
/// Manages fleet orders with light-speed delay. Orders are issued from the player flagship
/// and delayed by distance/c before reaching the target ship.
/// </summary>
public partial class OrderSystem : Node, IOrderSystem
{
    [Export] public NodePath LightSpeedSimPath { get; set; } = "";

    private LightSpeedSimulation _lightSim = null!;
    private ShipBase? _flagship;

    // Per-ship order queue: only latest order per ship
    private readonly Dictionary<int, Order> _pendingOrders = new();
    private readonly Dictionary<int, Order> _activeOrders = new();

    public event Action<int, Order>? OrderTransmitted;
    public event Action<int, Order>? OrderReceived;

    public override void _Ready()
    {
        _lightSim = GetNode<LightSpeedSimulation>(LightSpeedSimPath);
    }

    public void SetFlagship(ShipBase flagship)
    {
        _flagship = flagship;
    }

    public (string OrderId, float EstimatedArrivalTime) IssueOrder(
        int targetShipId,
        OrderType orderType,
        Vector3? position = null,
        int? targetId = null)
    {
        if (_flagship == null)
            return ("", 0f);

        // Find target ship to calculate distance
        var targetShip = FindShipById(targetShipId);
        if (targetShip == null)
            return ("", 0f);

        float distance = _flagship.GlobalPosition.DistanceTo(targetShip.GlobalPosition);
        float delay = _lightSim.CalculateDelay(distance);
        double currentTime = GetTree().Root.GetPhysicsProcessDeltaTime() > 0
            ? Time.GetTicksMsec() / 1000.0
            : 0.0;

        var order = new Order
        {
            Type = orderType,
            TargetPosition = position,
            TargetShipId = targetId,
            IssuedAtTime = currentTime,
            ReceiveAtTime = currentTime + delay
        };

        // Replace any existing pending order for this ship
        _pendingOrders[targetShipId] = order;

        OrderTransmitted?.Invoke(targetShipId, order);

        return (order.OrderId, delay);
    }

    public override void _PhysicsProcess(double delta)
    {
        double currentTime = Time.GetTicksMsec() / 1000.0;

        // Check pending orders for delivery
        var delivered = new List<int>();
        foreach (var (shipId, order) in _pendingOrders)
        {
            if (currentTime >= order.ReceiveAtTime)
            {
                order.Received = true;
                _activeOrders[shipId] = order;
                delivered.Add(shipId);

                // Deliver to AI ship
                var ship = FindShipById(shipId);
                if (ship is AIShip aiShip)
                    aiShip.ReceiveOrder(order);

                OrderReceived?.Invoke(shipId, order);
            }
        }

        foreach (var id in delivered)
            _pendingOrders.Remove(id);
    }

    public Order? GetActiveOrder(int shipId)
    {
        return _activeOrders.GetValueOrDefault(shipId);
    }

    public Order? GetPendingOrder(int shipId)
    {
        return _pendingOrders.GetValueOrDefault(shipId);
    }

    public bool HasPendingOrder(int shipId) => _pendingOrders.ContainsKey(shipId);

    private ShipBase? FindShipById(int shipId)
    {
        // Search through all ships in the "ships" group
        foreach (var node in GetTree().GetNodesInGroup("ships"))
        {
            if (node is ShipBase ship && ship.ShipId == shipId)
                return ship;
        }
        return null;
    }
}
