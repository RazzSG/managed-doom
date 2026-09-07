using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Collision;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomCollisionQuirksTest
{
    [TestMethod]
    public void SolidMoverIsBlockedBySolidThingAtEveryCompatibilityLevel()
    {
        var moving = MobjFlags.Solid;
        var obstacle = MobjFlags.Solid;

        Assert.IsTrue(BoomCollisionQuirks.BlocksGenericThing(moving, obstacle, GameCompatibility.Vanilla));
        Assert.IsTrue(BoomCollisionQuirks.BlocksGenericThing(moving, obstacle, GameCompatibility.Boom));
        Assert.IsTrue(BoomCollisionQuirks.BlocksGenericThing(moving, obstacle, GameCompatibility.Mbf));
        Assert.IsTrue(BoomCollisionQuirks.BlocksGenericThing(moving, obstacle, GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void NonSolidMoverPassesSolidThingOnlyAtBoomAndLaterCompatibility()
    {
        var moving = (MobjFlags)0;
        var obstacle = MobjFlags.Solid;

        Assert.IsTrue(BoomCollisionQuirks.BlocksGenericThing(moving, obstacle, GameCompatibility.Vanilla));
        Assert.IsFalse(BoomCollisionQuirks.BlocksGenericThing(moving, obstacle, GameCompatibility.Boom));
        Assert.IsFalse(BoomCollisionQuirks.BlocksGenericThing(moving, obstacle, GameCompatibility.Mbf));
        Assert.IsFalse(BoomCollisionQuirks.BlocksGenericThing(moving, obstacle, GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void NoClipObstacleStopsBlockingOnlyAtBoomAndLaterCompatibility()
    {
        var moving = MobjFlags.Solid;
        var obstacle = MobjFlags.Solid | MobjFlags.NoClip;

        Assert.IsTrue(BoomCollisionQuirks.BlocksGenericThing(moving, obstacle, GameCompatibility.Vanilla));
        Assert.IsFalse(BoomCollisionQuirks.BlocksGenericThing(moving, obstacle, GameCompatibility.Boom));
        Assert.IsFalse(BoomCollisionQuirks.BlocksGenericThing(moving, obstacle, GameCompatibility.Mbf));
        Assert.IsFalse(BoomCollisionQuirks.BlocksGenericThing(moving, obstacle, GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void NonSolidObstacleNeverBlocks()
    {
        var moving = MobjFlags.Solid;
        var obstacle = MobjFlags.Shootable;

        Assert.IsFalse(BoomCollisionQuirks.BlocksGenericThing(moving, obstacle, GameCompatibility.Vanilla));
        Assert.IsFalse(BoomCollisionQuirks.BlocksGenericThing(moving, obstacle, GameCompatibility.Boom));
    }

    [TestMethod]
    public void CheckPositionLetsNonSolidMoverPassSolidThingInBoomOnly()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var vanillaWorld = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var vanillaMover = vanillaWorld.ConsolePlayer.Mobj;
        vanillaMover.Flags &= ~MobjFlags.Solid;
        var vanillaObstacle = vanillaWorld.ThingAllocation.SpawnMobj(
            vanillaMover.X, vanillaMover.Y, Mobj.OnFloorZ, MobjType.Barrel);

        Assert.IsFalse(vanillaWorld.ThingMovement.CheckPosition(
            vanillaMover, vanillaObstacle.X, vanillaObstacle.Y));

        var boomWorld = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var boomMover = boomWorld.ConsolePlayer.Mobj;
        boomMover.Flags &= ~MobjFlags.Solid;
        var boomObstacle = boomWorld.ThingAllocation.SpawnMobj(
            boomMover.X, boomMover.Y, Mobj.OnFloorZ, MobjType.Barrel);

        Assert.IsTrue(boomWorld.ThingMovement.CheckPosition(
            boomMover, boomObstacle.X, boomObstacle.Y));
    }

    [TestMethod]
    public void CheckPositionIgnoresNoClipSolidObstacleInBoomOnly()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var vanillaWorld = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var vanillaMover = vanillaWorld.ConsolePlayer.Mobj;
        var vanillaObstacle = vanillaWorld.ThingAllocation.SpawnMobj(
            vanillaMover.X, vanillaMover.Y, Mobj.OnFloorZ, MobjType.Barrel);
        vanillaObstacle.Flags |= MobjFlags.NoClip;

        Assert.IsFalse(vanillaWorld.ThingMovement.CheckPosition(
            vanillaMover, vanillaObstacle.X, vanillaObstacle.Y));

        var boomWorld = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var boomMover = boomWorld.ConsolePlayer.Mobj;
        var boomObstacle = boomWorld.ThingAllocation.SpawnMobj(
            boomMover.X, boomMover.Y, Mobj.OnFloorZ, MobjType.Barrel);
        boomObstacle.Flags |= MobjFlags.NoClip;

        Assert.IsTrue(boomWorld.ThingMovement.CheckPosition(
            boomMover, boomObstacle.X, boomObstacle.Y));
    }
}
