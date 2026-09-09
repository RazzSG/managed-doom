using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Gameplay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfPowerupCheatCompatibilityTest
{
    [TestMethod]
    public void BoundaryMatchesMbfCompInfCheatSemantics()
    {
        var duration = DoomInfo.PowerDuration.Invulnerability;

        Assert.AreEqual(duration, MbfPowerupCheatCompatibility.ResolveActivatedPowerValue(
            GameCompatibility.Vanilla, false, PowerType.Invulnerability, duration));
        Assert.AreEqual(duration, MbfPowerupCheatCompatibility.ResolveActivatedPowerValue(
            GameCompatibility.Boom, false, PowerType.Invulnerability, duration));

        Assert.AreEqual(MbfPowerupCheatCompatibility.InfiniteDuration,
            MbfPowerupCheatCompatibility.ResolveActivatedPowerValue(
                GameCompatibility.Mbf, false, PowerType.Invulnerability, duration));
        Assert.AreEqual(MbfPowerupCheatCompatibility.InfiniteDuration,
            MbfPowerupCheatCompatibility.ResolveActivatedPowerValue(
                GameCompatibility.Mbf21, false, PowerType.Invulnerability, duration));

        Assert.AreEqual(duration, MbfPowerupCheatCompatibility.ResolveActivatedPowerValue(
            GameCompatibility.Mbf, true, PowerType.Invulnerability, duration));
        Assert.AreEqual(duration, MbfPowerupCheatCompatibility.ResolveActivatedPowerValue(
            GameCompatibility.Mbf21, true, PowerType.Invulnerability, duration));
    }

    [TestMethod]
    public void InfiniteSentinelStaysActiveWithoutTickingDown()
    {
        var value = MbfPowerupCheatCompatibility.InfiniteDuration;

        Assert.IsTrue(MbfPowerupCheatCompatibility.IsActive(value));
        Assert.IsFalse(MbfPowerupCheatCompatibility.ShouldTickDown(value));
        Assert.IsFalse(MbfPowerupCheatCompatibility.IsActive(0));
        Assert.IsTrue(MbfPowerupCheatCompatibility.ShouldTickDown(1));
    }

    [TestMethod]
    public void StrengthKeepsItsExistingPermanentCounterSemantics()
    {
        Assert.AreEqual(1, MbfPowerupCheatCompatibility.ResolveActivatedPowerValue(
            GameCompatibility.Mbf, false, PowerType.Strength, 1));
        Assert.AreEqual(1, MbfPowerupCheatCompatibility.ResolveActivatedPowerValue(
            GameCompatibility.Mbf21, false, PowerType.Strength, 1));
    }

    [TestMethod]
    public void RuntimeCheatUsesInfiniteDurationByDefaultAndTimedDurationWhenCompatible()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var mbfOptions = new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = GameCompatibility.Mbf
        };
        mbfOptions.MbfOptions.CompInfCheat = false;
        var mbfWorld = new World(content, mbfOptions, null);

        TypeCheat(mbfWorld, "idbeholdv");
        Assert.AreEqual(MbfPowerupCheatCompatibility.InfiniteDuration,
            mbfWorld.ConsolePlayer.Powers[(int)PowerType.Invulnerability]);

        mbfWorld.PlayerBehavior.PlayerThink(mbfWorld.ConsolePlayer);
        Assert.AreEqual(MbfPowerupCheatCompatibility.InfiniteDuration,
            mbfWorld.ConsolePlayer.Powers[(int)PowerType.Invulnerability]);

        var compatibleOptions = new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = GameCompatibility.Mbf
        };
        compatibleOptions.MbfOptions.CompInfCheat = true;
        var compatibleWorld = new World(content, compatibleOptions, null);

        TypeCheat(compatibleWorld, "idbeholdv");
        Assert.AreEqual(DoomInfo.PowerDuration.Invulnerability,
            compatibleWorld.ConsolePlayer.Powers[(int)PowerType.Invulnerability]);
    }

    private static void TypeCheat(World world, string text)
    {
        foreach (var ch in text)
        {
            var key = (DoomKey)((int)DoomKey.A + (ch - 'a'));
            world.Cheat.DoEvent(new DoomEvent(EventType.KeyDown, key));
        }
    }
}
