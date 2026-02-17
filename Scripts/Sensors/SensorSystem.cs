using Godot;
using EchoesOfCommand.Core;
using EchoesOfCommand.Enums;
using EchoesOfCommand.Ships;

namespace EchoesOfCommand.Sensors;

/// <summary>
/// Calculates apparent enemy positions based on light-speed delay.
/// Player sees enemies where they WERE, not where they ARE.
/// </summary>
public partial class SensorSystem : Node
{
    [Export] public float SensorRange { get; set; } = GameConstants.SensorRange;
    [Export] public bool DebugShowActualPositions { get; set; }

    private LightSpeedSimulation _lightSim = null!;
    private ShipBase? _observerShip;

    public struct SensorContact
    {
        public int ShipId;
        public Vector3 ApparentPosition;
        public Vector3 ApparentVelocity;
        public Vector3 ActualPosition;
        public float Delay;
        public bool InRange;
        public ShipClass ShipClass;
    }

    private readonly Dictionary<int, SensorContact> _contacts = new();

    public IReadOnlyDictionary<int, SensorContact> Contacts => _contacts;

    public override void _Ready()
    {
        _lightSim = GetParent().GetNode<LightSpeedSimulation>("LightSpeedSimulation");
    }

    public void SetObserver(ShipBase observer)
    {
        _observerShip = observer;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_observerShip == null)
            return;

        _contacts.Clear();

        foreach (var node in GetTree().GetNodesInGroup("ships"))
        {
            if (node is not ShipBase ship)
                continue;

            // Skip friendly ships (shown in real-time via local sensors)
            if (ship.Faction == _observerShip.Faction)
                continue;

            float distance = _observerShip.GlobalPosition.DistanceTo(ship.GlobalPosition);
            bool inRange = distance <= SensorRange;

            if (!inRange)
                continue;

            var (apparentPos, delay) = _lightSim.GetDelayedPosition(
                ship.GlobalPosition,
                _observerShip.GlobalPosition,
                ship.CurrentVelocity);

            // Delayed velocity (what we observed `delay` seconds ago — approximated as current)
            var apparentVelocity = ship.CurrentVelocity;

            _contacts[ship.ShipId] = new SensorContact
            {
                ShipId = ship.ShipId,
                ApparentPosition = apparentPos,
                ApparentVelocity = apparentVelocity,
                ActualPosition = ship.GlobalPosition,
                Delay = delay,
                InRange = true,
                ShipClass = ship.ShipClass
            };
        }
    }

    /// <summary>
    /// Predict where a contact will be at a future time, based on delayed data.
    /// Used for targeting assistance.
    /// </summary>
    public Vector3 PredictPosition(int shipId, float secondsAhead)
    {
        if (!_contacts.TryGetValue(shipId, out var contact))
            return Vector3.Zero;

        // Extrapolate from apparent position using apparent velocity
        return contact.ApparentPosition + contact.ApparentVelocity * (contact.Delay + secondsAhead);
    }
}
