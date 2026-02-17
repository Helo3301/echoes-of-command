using Xunit;
using EchoesOfCommand.Enums;
using EchoesOfCommand.Ships;
using EchoesOfCommand.Subsystems;

namespace EchoesOfCommand.Tests;

public class SubsystemTests
{
    private static ShipSubsystems CreateBattlecruiserSubsystems()
    {
        return new ShipSubsystems(ShipClassDatabase.Get(ShipClass.Battlecruiser));
    }

    [Fact]
    public void ApplyDamage_Shield150_LeavesShieldAt50()
    {
        // Battlecruiser shield = 200 HP
        var subs = CreateBattlecruiserSubsystems();

        bool destroyed = subs.ApplyDamage(SubsystemType.Shield, 150f);

        Assert.False(destroyed);
        var (current, max, functional) = subs.GetSubsystemHealth(SubsystemType.Shield);
        Assert.Equal(50f, current, precision: 1);
        Assert.True(functional);
    }

    [Fact]
    public void ApplyDamage_Shield250_DestroysShield()
    {
        // Battlecruiser shield = 200 HP, 250 > 200
        var subs = CreateBattlecruiserSubsystems();

        bool destroyed = subs.ApplyDamage(SubsystemType.Shield, 250f);

        Assert.True(destroyed);
        var (current, _, functional) = subs.GetSubsystemHealth(SubsystemType.Shield);
        Assert.Equal(0f, current);
        Assert.False(functional);
    }

    [Fact]
    public void ApplyDamage_HullWithShieldsUp_ShieldsAbsorbFirst()
    {
        // Battlecruiser: shield=200, hull=300
        // 250 damage to hull: shield absorbs 200, hull takes 50
        var subs = CreateBattlecruiserSubsystems();

        bool destroyed = subs.ApplyDamage(SubsystemType.Hull, 250f);

        Assert.False(destroyed);
        var (shieldHp, _, _) = subs.GetSubsystemHealth(SubsystemType.Shield);
        var (hullHp, _, _) = subs.GetSubsystemHealth(SubsystemType.Hull);
        Assert.Equal(0f, shieldHp);
        Assert.Equal(250f, hullHp, precision: 1); // 300 - 50 = 250
    }

    [Fact]
    public void ApplyDamage_HullWithShieldsDown_DirectHullDamage()
    {
        var subs = CreateBattlecruiserSubsystems();

        // Destroy shields first
        subs.ApplyDamage(SubsystemType.Shield, 200f);

        // Now damage hull directly
        subs.ApplyDamage(SubsystemType.Hull, 100f);

        var (hullHp, _, _) = subs.GetSubsystemHealth(SubsystemType.Hull);
        Assert.Equal(200f, hullHp, precision: 1); // 300 - 100
    }

    [Fact]
    public void DestroyedEngine_ReducesSpeedAndTurnRate()
    {
        var subs = CreateBattlecruiserSubsystems();

        // Destroy engine: shield=200 absorbs first, engine=150
        // Need 200 + 150 = 350 damage to engine subsystem
        subs.ApplyDamage(SubsystemType.Engine, 350f);

        Assert.Equal(0.5f, subs.SpeedMultiplier);
        Assert.Equal(0.3f, subs.TurnRateMultiplier);
    }

    [Fact]
    public void FunctionalEngine_FullSpeedAndTurnRate()
    {
        var subs = CreateBattlecruiserSubsystems();

        Assert.Equal(1f, subs.SpeedMultiplier);
        Assert.Equal(1f, subs.TurnRateMultiplier);
    }

    [Fact]
    public void DestroyedWeapons_CannotFire()
    {
        var subs = CreateBattlecruiserSubsystems();

        // Weapons HP = 100, shield = 200. Need 300 total.
        subs.ApplyDamage(SubsystemType.Weapons, 300f);

        Assert.False(subs.CanFire);
    }

    [Fact]
    public void Repair_RestoresHealth()
    {
        var subs = CreateBattlecruiserSubsystems();

        // Damage shield
        subs.ApplyDamage(SubsystemType.Shield, 100f);
        var (before, _, _) = subs.GetSubsystemHealth(SubsystemType.Shield);
        Assert.Equal(100f, before, precision: 1);

        // Repair 50
        bool repaired = subs.Repair(SubsystemType.Shield, 50f);

        Assert.True(repaired);
        var (after, _, _) = subs.GetSubsystemHealth(SubsystemType.Shield);
        Assert.Equal(150f, after, precision: 1);
    }

    [Fact]
    public void Repair_DoesNotExceedMax()
    {
        var subs = CreateBattlecruiserSubsystems();

        subs.ApplyDamage(SubsystemType.Shield, 10f);
        subs.Repair(SubsystemType.Shield, 1000f);

        var (current, max, _) = subs.GetSubsystemHealth(SubsystemType.Shield);
        Assert.Equal(max, current);
    }

    [Fact]
    public void HullDestroyed_ShipIsDestroyed()
    {
        var subs = CreateBattlecruiserSubsystems();
        bool eventFired = false;
        subs.ShipDestroyed += () => eventFired = true;

        // Shield=200, Hull=300. Need 500 total to hull.
        subs.ApplyDamage(SubsystemType.Hull, 500f);

        Assert.True(subs.IsDestroyed);
        Assert.True(eventFired);
    }

    [Fact]
    public void SubsystemDestroyedEvent_Fires()
    {
        var subs = CreateBattlecruiserSubsystems();
        var destroyed = new List<SubsystemType>();
        subs.SubsystemDestroyed += t => destroyed.Add(t);

        // 250 to hull: shield absorbs 200 (destroyed), hull takes 50 (alive)
        subs.ApplyDamage(SubsystemType.Hull, 250f);

        Assert.Contains(SubsystemType.Shield, destroyed);
    }

    [Fact]
    public void ShieldRegen_RestoresOverTime()
    {
        var subs = CreateBattlecruiserSubsystems();

        // Damage shield by 50 (200 -> 150)
        subs.ApplyDamage(SubsystemType.Shield, 50f);

        // Regen for 2 seconds at 5 HP/s = 10 HP
        subs.RegenShields(2f);

        var (current, _, _) = subs.GetSubsystemHealth(SubsystemType.Shield);
        Assert.Equal(160f, current, precision: 1);
    }

    [Fact]
    public void ZeroDamage_NoEffect()
    {
        var subs = CreateBattlecruiserSubsystems();

        bool destroyed = subs.ApplyDamage(SubsystemType.Hull, 0f);

        Assert.False(destroyed);
        var (current, max, _) = subs.GetSubsystemHealth(SubsystemType.Hull);
        Assert.Equal(max, current);
    }
}
