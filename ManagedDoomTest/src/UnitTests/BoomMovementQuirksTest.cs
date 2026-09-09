using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Friction;
using ManagedDoom.Compatibility.Boom.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomMovementQuirksTest
{
    [TestMethod]
    public void NegativeLargeMovementIsSplitOnlyAtBoomAndLaterCompatibility()
    {
        var halfMaxMove = Fixed.FromInt(15);
        var largeNegative = Fixed.FromInt(-16);

        Assert.IsFalse(BoomMovementQuirks.ShouldSplitNegativeDisplacement(
            largeNegative, Fixed.Zero, halfMaxMove, GameCompatibility.Vanilla));

        Assert.IsTrue(BoomMovementQuirks.ShouldSplitNegativeDisplacement(
            largeNegative, Fixed.Zero, halfMaxMove, GameCompatibility.Boom));
        Assert.IsTrue(BoomMovementQuirks.ShouldSplitNegativeDisplacement(
            Fixed.Zero, largeNegative, halfMaxMove, GameCompatibility.Mbf));
        Assert.IsTrue(BoomMovementQuirks.ShouldSplitNegativeDisplacement(
            largeNegative, Fixed.Zero, halfMaxMove, GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void NegativeMovementAtHalfMaxBoundaryIsNotSplit()
    {
        var halfMaxMove = Fixed.FromInt(15);

        Assert.IsFalse(BoomMovementQuirks.ShouldSplitNegativeDisplacement(
            -halfMaxMove, Fixed.Zero, halfMaxMove, GameCompatibility.Boom));
        Assert.IsFalse(BoomMovementQuirks.ShouldSplitNegativeDisplacement(
            Fixed.Zero, -halfMaxMove, halfMaxMove, GameCompatibility.Boom));
    }

    [TestMethod]
    public void BlockedBoomPlayerUsesOriginalFrictionForThatTic()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;
        var customFriction = new Fixed(0xf000);

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = customFriction;
        thing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
        thing.Z = sector.FloorHeight;

        var oldX = thing.X;
        var oldY = thing.Y;

        Assert.AreEqual(
            BoomFrictionTranslator.OriginalFrictionData,
            BoomMovementQuirks.GetCoastingFriction(
                thing, oldX, oldY, GameCompatibility.Boom).Data);

        thing.X += Fixed.One;

        Assert.AreEqual(
            customFriction.Data,
            BoomMovementQuirks.GetCoastingFriction(
                thing, oldX, oldY, GameCompatibility.Boom).Data);
    }

    [TestMethod]
    public void VanillaCoastingFrictionIgnoresBoomFloorFriction()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = new Fixed(0xf000);
        thing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
        thing.Z = sector.FloorHeight;

        var oldX = thing.X - Fixed.One;

        Assert.AreEqual(
            BoomFrictionTranslator.OriginalFrictionData,
            BoomMovementQuirks.GetCoastingFriction(
                thing, oldX, thing.Y, GameCompatibility.Vanilla).Data);
    }

    [TestMethod]
    public void BoomIceUsesQuarterMaximumBobWhileMbfUsesNormalMaximum()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var boomWorld = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var boomPlayer = boomWorld.ConsolePlayer;
        var boomThing = boomPlayer.Mobj;
        var boomSector = boomThing.Subsector.Sector;

        boomSector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        boomSector.Friction = new Fixed(0xf000);
        boomThing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
        boomThing.Z = boomSector.FloorHeight;
        boomThing.MomX = Fixed.FromInt(20);
        boomThing.MomY = Fixed.Zero;

        boomWorld.PlayerBehavior.CalcHeight(boomPlayer);

        Assert.AreEqual(0x100000 >> 2, boomPlayer.Bob.Data);

        foreach (var compatibility in new[] { GameCompatibility.Mbf, GameCompatibility.Mbf21 })
        {
            var world = new World(content, new GameOptions { Compatibility = compatibility }, null);
            var player = world.ConsolePlayer;
            var thing = player.Mobj;
            var sector = thing.Subsector.Sector;

            sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
            sector.Friction = new Fixed(0xf000);
            thing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
            thing.Z = sector.FloorHeight;
            thing.MomX = Fixed.Zero;
            thing.MomY = Fixed.Zero;
            player.BobMomX = Fixed.FromInt(20);
            player.BobMomY = Fixed.Zero;

            world.PlayerBehavior.CalcHeight(player);

            Assert.AreEqual(0x100000, player.Bob.Data, compatibility.ToString());
        }
    }

    [TestMethod]
    public void VanillaStillUsesNormalMaximumBobFromPhysicalMomentum()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var player = world.ConsolePlayer;
        var thing = player.Mobj;

        thing.MomX = Fixed.FromInt(20);
        thing.MomY = Fixed.Zero;
        player.BobMomX = Fixed.Zero;
        player.BobMomY = Fixed.Zero;

        world.PlayerBehavior.CalcHeight(player);

        Assert.AreEqual(0x100000, player.Bob.Data);
    }
}
