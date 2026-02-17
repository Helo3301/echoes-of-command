using Godot;

namespace EchoesOfCommand.Interfaces;

public interface ILightSpeedSimulation
{
    float SpeedOfLight { get; }

    float CalculateDelay(float distanceMeters);

    (Vector3 ApparentPosition, float DelaySeconds) GetDelayedPosition(
        Vector3 currentPosition,
        Vector3 observerPosition,
        Vector3 velocity);
}
