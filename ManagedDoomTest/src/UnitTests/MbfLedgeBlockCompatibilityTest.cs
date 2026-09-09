using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf;
using ManagedDoom.Compatibility.Mbf.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfLedgeBlockCompatibilityTest
{
    [TestMethod]
    public void ProfileDefaultsAndExplicitOverridesMatchPrBoom()
    {
        Assert.IsFalse(MbfLedgeBlockCompatibility.IsEnabled(
            GameCompatibility.Boom, true, true));

        Assert.IsFalse(MbfLedgeBlockCompatibility.IsEnabled(
            GameCompatibility.Mbf, false, false));
        Assert.IsFalse(MbfLedgeBlockCompatibility.IsEnabled(
            GameCompatibility.Mbf, false, true));
        Assert.IsTrue(MbfLedgeBlockCompatibility.IsEnabled(
            GameCompatibility.Mbf, true, true));

        Assert.IsTrue(MbfLedgeBlockCompatibility.IsEnabled(
            GameCompatibility.Mbf21, false, false));
        Assert.IsFalse(MbfLedgeBlockCompatibility.IsEnabled(
            GameCompatibility.Mbf21, false, true));
        Assert.IsTrue(MbfLedgeBlockCompatibility.IsEnabled(
            GameCompatibility.Mbf21, true, true));
    }

    [TestMethod]
    public void TallDropoffStartsAboveTwentyFourUnits()
    {
        Assert.IsFalse(MbfLedgeBlockCompatibility.BlocksTallDropoff(
            GameCompatibility.Mbf21,
            false,
            false,
            false,
            Fixed.FromInt(64),
            Fixed.FromInt(40)));

        Assert.IsTrue(MbfLedgeBlockCompatibility.BlocksTallDropoff(
            GameCompatibility.Mbf21,
            false,
            false,
            false,
            Fixed.FromInt(64),
            Fixed.FromInt(39)));
    }

    [TestMethod]
    public void ScrollingBypassIsMbf21Only()
    {
        Assert.IsFalse(MbfLedgeBlockCompatibility.BlocksTallDropoff(
            GameCompatibility.Mbf21,
            false,
            false,
            true,
            Fixed.FromInt(64),
            Fixed.FromInt(0)));

        Assert.IsTrue(MbfLedgeBlockCompatibility.BlocksTallDropoff(
            GameCompatibility.Mbf,
            true,
            true,
            true,
            Fixed.FromInt(64),
            Fixed.FromInt(0)));
    }

    [TestMethod]
    public void ZeroStrengthPushDoesNotCreateTransientScrollingMarker()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf21 },
            null);
        var thing = world.ConsolePlayer.Mobj;

        MbfLedgeBlockCompatibility.MarkScrollingMovement(
            thing, Fixed.Zero, Fixed.Zero);
        Assert.IsFalse(thing.MbfScrollingMovement);

        MbfLedgeBlockCompatibility.MarkScrollingMovement(
            thing, Fixed.One, Fixed.Zero);
        Assert.IsTrue(thing.MbfScrollingMovement);
    }

    [TestMethod]
    public void MobjRunClearsScrollingMarkerAfterNextXyMovement()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf21 },
            null);
        var thing = world.ConsolePlayer.Mobj;

        thing.MbfScrollingMovement = true;
        thing.MomX = Fixed.One;
        thing.MomY = Fixed.Zero;

        thing.Run();

        Assert.IsFalse(thing.MbfScrollingMovement);
    }

    [TestMethod]
    public void ParserAndClonePreserveExplicitFalseOverride()
    {
        var parsed = MbfOptionsReader.Parse("comp_ledgeblock 0");

        Assert.IsFalse(parsed.CompLedgeBlock);
        Assert.IsTrue(parsed.HasCompLedgeBlockOverride);

        var clone = parsed.Clone();
        Assert.IsFalse(clone.CompLedgeBlock);
        Assert.IsTrue(clone.HasCompLedgeBlockOverride);
    }
}
