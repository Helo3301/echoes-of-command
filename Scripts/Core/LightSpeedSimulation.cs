using Godot;
using EchoesOfCommand.Interfaces;

namespace EchoesOfCommand.Core;

/// <summary>
/// Pure math for light-speed calculations. No Godot dependency — unit-testable.
/// </summary>
public static class LightSpeedMath
{
    public static float CalculateDelay(float distanceMeters, float speedOfLight)
    {
        if (distanceMeters <= 0f)
            return 0f;

        return distanceMeters / speedOfLight;
    }

    public static (Vector3 ApparentPosition, float DelaySeconds) GetDelayedPosition(
        Vector3 currentPosition,
        Vector3 observerPosition,
        Vector3 velocity,
        float speedOfLight)
    {
        float distance = currentPosition.DistanceTo(observerPosition);
        float delay = CalculateDelay(distance, speedOfLight);
        Vector3 apparentPosition = currentPosition - velocity * delay;
        return (apparentPosition, delay);
    }
}

/// <summary>
/// Godot Node wrapper with [Export] for editor tuning.
/// </summary>
public partial class LightSpeedSimulation : Node, ILightSpeedSimulation
{
    [Export]
    public float SpeedOfLight { get; set; } = GameConstants.SpeedOfLight;

    public float CalculateDelay(float distanceMeters)
        => LightSpeedMath.CalculateDelay(distanceMeters, SpeedOfLight);

    public (Vector3 ApparentPosition, float DelaySeconds) GetDelayedPosition(
        Vector3 currentPosition,
        Vector3 observerPosition,
        Vector3 velocity)
        => LightSpeedMath.GetDelayedPosition(currentPosition, observerPosition, velocity, SpeedOfLight);
}
