using Xunit;
using Godot;
using EchoesOfCommand.Core;

namespace EchoesOfCommand.Tests;

public class LightSpeedTests
{
    private const float C = GameConstants.SpeedOfLight; // 300 m/s

    [Fact]
    public void CalculateDelay_AtSpeedOfLight_Returns1Second()
    {
        float delay = LightSpeedMath.CalculateDelay(300f, C);
        Assert.Equal(1.0f, delay, precision: 5);
    }

    [Fact]
    public void CalculateDelay_ZeroDistance_ReturnsZero()
    {
        float delay = LightSpeedMath.CalculateDelay(0f, C);
        Assert.Equal(0.0f, delay);
    }

    [Fact]
    public void CalculateDelay_NegativeDistance_ReturnsZero()
    {
        float delay = LightSpeedMath.CalculateDelay(-100f, C);
        Assert.Equal(0.0f, delay);
    }

    [Fact]
    public void CalculateDelay_1000m_Returns3Point33s()
    {
        float delay = LightSpeedMath.CalculateDelay(1000f, C);
        Assert.Equal(1000f / 300f, delay, precision: 3);
    }

    [Fact]
    public void GetDelayedPosition_MovingObject_ReturnsHistoricalPosition()
    {
        // Object at (600, 0, 0) moving at (100, 0, 0) m/s
        // Observer at origin
        // Distance = 600m, delay = 600/300 = 2s
        // Apparent position = (600, 0, 0) - (100, 0, 0) * 2 = (400, 0, 0)
        var current = new Vector3(600, 0, 0);
        var observer = new Vector3(0, 0, 0);
        var velocity = new Vector3(100, 0, 0);

        var (apparent, delay) = LightSpeedMath.GetDelayedPosition(current, observer, velocity, C);

        Assert.Equal(2.0f, delay, precision: 3);
        Assert.Equal(400f, apparent.X, precision: 1);
        Assert.Equal(0f, apparent.Y, precision: 1);
        Assert.Equal(0f, apparent.Z, precision: 1);
    }

    [Fact]
    public void GetDelayedPosition_StationaryObject_ReturnsSamePosition()
    {
        var current = new Vector3(300, 0, 0);
        var observer = new Vector3(0, 0, 0);
        var velocity = Vector3.Zero;

        var (apparent, delay) = LightSpeedMath.GetDelayedPosition(current, observer, velocity, C);

        Assert.Equal(1.0f, delay, precision: 3);
        Assert.Equal(current.X, apparent.X, precision: 1);
        Assert.Equal(current.Y, apparent.Y, precision: 1);
        Assert.Equal(current.Z, apparent.Z, precision: 1);
    }

    [Fact]
    public void GetDelayedPosition_SameLocation_NoDelay()
    {
        var pos = new Vector3(100, 200, 300);
        var velocity = new Vector3(50, 0, 0);

        var (apparent, delay) = LightSpeedMath.GetDelayedPosition(pos, pos, velocity, C);

        Assert.Equal(0f, delay, precision: 5);
        Assert.Equal(pos.X, apparent.X, precision: 1);
    }

    [Fact]
    public void CalculateDelay_CustomSpeedOfLight_Respected()
    {
        // With c=600, 300m should take 0.5s
        float delay = LightSpeedMath.CalculateDelay(300f, 600f);
        Assert.Equal(0.5f, delay, precision: 5);
    }
}
