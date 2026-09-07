using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Sectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomSectorMovementQuirksTest
{
    [TestMethod]
    public void IntermediateLoweringFloorRollbackIsVanillaOnly()
    {
        Assert.IsTrue(BoomSectorMovementQuirks.ShouldRestoreIntermediateLoweringFloorAfterNoFit(
            GameCompatibility.Vanilla));

        Assert.IsFalse(BoomSectorMovementQuirks.ShouldRestoreIntermediateLoweringFloorAfterNoFit(
            GameCompatibility.Boom));
        Assert.IsFalse(BoomSectorMovementQuirks.ShouldRestoreIntermediateLoweringFloorAfterNoFit(
            GameCompatibility.Mbf));
        Assert.IsFalse(BoomSectorMovementQuirks.ShouldRestoreIntermediateLoweringFloorAfterNoFit(
            GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void CrushingRaisingFloorRollbackUsesBoomFloorCrusherFix()
    {
        Assert.IsFalse(BoomSectorMovementQuirks.ShouldRestoreIntermediateRaisingFloorAfterNoFit(
            GameCompatibility.Vanilla,
            true));
        Assert.IsTrue(BoomSectorMovementQuirks.ShouldRestoreIntermediateRaisingFloorAfterNoFit(
            GameCompatibility.Vanilla,
            false));

        Assert.IsTrue(BoomSectorMovementQuirks.ShouldRestoreIntermediateRaisingFloorAfterNoFit(
            GameCompatibility.Boom,
            true));
        Assert.IsTrue(BoomSectorMovementQuirks.ShouldRestoreIntermediateRaisingFloorAfterNoFit(
            GameCompatibility.Mbf,
            true));
        Assert.IsTrue(BoomSectorMovementQuirks.ShouldRestoreIntermediateRaisingFloorAfterNoFit(
            GameCompatibility.Mbf21,
            true));
    }

    [TestMethod]
    public void VanillaIntermediateLoweringFloorRollsBackOnNoFit()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var player = world.ConsolePlayer.Mobj;
        var sector = player.Subsector.Sector;
        var startHeight = sector.FloorHeight;

        PrepareNoFitPlayer(world, player, sector);

        var result = world.SectorAction.MovePlane(
            sector,
            Fixed.FromInt(8),
            startHeight - Fixed.FromInt(32),
            false,
            0,
            -1);

        Assert.AreEqual(SectorActionResult.Crushed, result);
        Assert.AreEqual(startHeight.Data, sector.FloorHeight.Data);
    }

    [TestMethod]
    public void BoomIntermediateLoweringFloorKeepsNewHeightOnNoFit()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer.Mobj;
        var sector = player.Subsector.Sector;
        var startHeight = sector.FloorHeight;

        PrepareNoFitPlayer(world, player, sector);

        var result = world.SectorAction.MovePlane(
            sector,
            Fixed.FromInt(8),
            startHeight - Fixed.FromInt(32),
            false,
            0,
            -1);

        Assert.AreEqual(SectorActionResult.OK, result);
        Assert.AreEqual((startHeight - Fixed.FromInt(8)).Data, sector.FloorHeight.Data);
    }

    [TestMethod]
    public void VanillaCrushingRaisingFloorKeepsBlockedIntermediateHeight()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var player = world.ConsolePlayer.Mobj;
        var sector = player.Subsector.Sector;
        var startHeight = sector.FloorHeight;

        PrepareRaisingFloorCrusherPlayer(world, player, sector);

        var startHealth = player.Health;
        var result = world.SectorAction.MovePlane(
            sector,
            Fixed.FromInt(8),
            startHeight + Fixed.FromInt(32),
            true,
            0,
            1);

        Assert.AreEqual(SectorActionResult.Crushed, result);
        Assert.AreEqual((startHeight + Fixed.FromInt(8)).Data, sector.FloorHeight.Data);
        Assert.AreEqual(startHealth - 10, player.Health);
    }

    [TestMethod]
    public void BoomCrushingRaisingFloorRetriesBlockedStepAndAppliesDamage()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer.Mobj;
        var sector = player.Subsector.Sector;
        var startHeight = sector.FloorHeight;

        PrepareRaisingFloorCrusherPlayer(world, player, sector);

        var startHealth = player.Health;

        var firstResult = world.SectorAction.MovePlane(
            sector,
            Fixed.FromInt(8),
            startHeight + Fixed.FromInt(32),
            true,
            0,
            1);

        Assert.AreEqual(SectorActionResult.Crushed, firstResult);
        Assert.AreEqual(startHeight.Data, sector.FloorHeight.Data,
            "Boom floor crushers must restore a blocked intermediate upward step.");
        Assert.AreEqual(startHealth - 10, player.Health);

        var secondResult = world.SectorAction.MovePlane(
            sector,
            Fixed.FromInt(8),
            startHeight + Fixed.FromInt(32),
            true,
            0,
            1);

        Assert.AreEqual(SectorActionResult.Crushed, secondResult);
        Assert.AreEqual(startHeight.Data, sector.FloorHeight.Data,
            "The crusher should keep retrying from the last fitting floor height.");
        Assert.AreEqual(startHealth - 20, player.Health,
            "A blocked Boom floor crusher should continue checking the obstruction on successive attempts.");
    }

    [TestMethod]
    public void BoomRaisingFloorStopsAtCurrentCeilingInsteadOfCrossingIt()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var sector = CreateDetachedSector(96, 100);

        var first = world.SectorAction.MovePlane(
            sector,
            Fixed.FromInt(4),
            Fixed.FromInt(128),
            false,
            0,
            1);

        Assert.AreEqual(SectorActionResult.OK, first);
        Assert.AreEqual(Fixed.FromInt(100).Data, sector.FloorHeight.Data);

        var second = world.SectorAction.MovePlane(
            sector,
            Fixed.FromInt(4),
            Fixed.FromInt(128),
            false,
            0,
            1);

        Assert.AreEqual(SectorActionResult.PastDestination, second);
        Assert.AreEqual(Fixed.FromInt(100).Data, sector.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(100).Data, sector.CeilingHeight.Data);
    }

    [TestMethod]
    public void BoomLoweringCeilingStopsAtCurrentFloorInsteadOfCrossingIt()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var sector = CreateDetachedSector(100, 101);

        var first = world.SectorAction.MovePlane(
            sector,
            Fixed.FromInt(1),
            Fixed.Zero,
            false,
            1,
            -1);

        Assert.AreEqual(SectorActionResult.OK, first);
        Assert.AreEqual(Fixed.FromInt(100).Data, sector.CeilingHeight.Data);

        var second = world.SectorAction.MovePlane(
            sector,
            Fixed.FromInt(1),
            Fixed.Zero,
            false,
            1,
            -1);

        Assert.AreEqual(SectorActionResult.PastDestination, second);
        Assert.AreEqual(Fixed.FromInt(100).Data, sector.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(100).Data, sector.CeilingHeight.Data);
    }

    [TestMethod]
    public void VanillaPlaneMovementKeepsOriginalCrossingBehavior()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var sector = CreateDetachedSector(100, 100);

        var result = world.SectorAction.MovePlane(
            sector,
            Fixed.FromInt(4),
            Fixed.FromInt(128),
            false,
            0,
            1);

        Assert.AreEqual(SectorActionResult.OK, result);
        Assert.AreEqual(Fixed.FromInt(104).Data, sector.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(100).Data, sector.CeilingHeight.Data);
    }

    [TestMethod]
    public void BoomDestinationStepStillRollsBackOnNoFit()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer.Mobj;
        var sector = player.Subsector.Sector;
        var startHeight = sector.FloorHeight;

        PrepareNoFitPlayer(world, player, sector);

        var result = world.SectorAction.MovePlane(
            sector,
            Fixed.FromInt(8),
            startHeight - Fixed.FromInt(4),
            false,
            0,
            -1);

        Assert.AreEqual(SectorActionResult.PastDestination, result);
        Assert.AreEqual(startHeight.Data, sector.FloorHeight.Data);
    }

    private static void PrepareRaisingFloorCrusherPlayer(World world, Mobj player, Sector sector)
    {
        player.Flags |= MobjFlags.Solid | MobjFlags.Shootable;
        player.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoBlockMap);
        player.Health = 100;

        world.ThingMovement.UnsetThingPosition(player);
        world.ThingMovement.SetThingPosition(player);

        player.Z = sector.FloorHeight;
        player.FloorZ = sector.FloorHeight;

        // The actor exactly fits before the floor moves. Raising the floor by
        // one turbo step makes the sector too short, while restoring the old
        // height makes it fit again. This distinguishes Boom's floor-crusher
        // rollback from the vanilla crush=true behavior.
        sector.CeilingHeight = sector.FloorHeight + player.Height;
        player.CeilingZ = sector.CeilingHeight;

        Assert.AreEqual(player.Height.Data,
            (sector.CeilingHeight - sector.FloorHeight).Data,
            "The crusher fixture must fit exactly before the upward step.");
    }

    private static Sector CreateDetachedSector(int floorHeight, int ceilingHeight)
    {
        return new Sector(
            -1,
            Fixed.FromInt(floorHeight),
            Fixed.FromInt(ceilingHeight),
            0,
            0,
            0,
            0,
            0)
        {
            // Boom P_CheckSector sees an empty touching list. Vanilla P_ChangeSector
            // scans BlockBox, so keep that scan deterministically outside the map.
            BlockBox = new[] { -1, -1, -1, -1 }
        };
    }

    private static void PrepareNoFitPlayer(World world, Mobj player, Sector sector)
    {
        player.Flags |= MobjFlags.Solid | MobjFlags.Shootable;
        player.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoBlockMap);

        // A World created directly from a fresh GameOptions has a test player whose
        // backing Player has not gone through Reborn(), so the spawned mobj can start
        // with zero health. P_ChangeSector treats zero-health mobjs as corpses and
        // never reports no-fit for them. This fixture explicitly needs a live,
        // shootable actor so it reaches the no-fit branch we are testing.
        player.Health = 100;
        Assert.IsTrue(player.Health > 0, "The no-fit fixture must use a live shootable mobj.");

        // Re-link through the engine's normal path. BlockBox only controls which
        // blockmap cells P_ChangeSector scans; it does not guarantee that the mobj
        // is actually linked into the corresponding ThingLists entry.
        world.ThingMovement.UnsetThingPosition(player);
        world.ThingMovement.SetThingPosition(player);

        player.Z = sector.FloorHeight;
        player.FloorZ = sector.FloorHeight;

        // Deliberately create a too-short sector so P_ThingHeightClip reports no-fit.
        sector.CeilingHeight = sector.FloorHeight + Fixed.FromInt(32);
        player.CeilingZ = sector.CeilingHeight;

        var blockMap = world.Map.BlockMap;
        var blockX = blockMap.GetBlockX(player.X);
        var blockY = blockMap.GetBlockY(player.Y);
        var blockIndex = blockMap.GetIndex(blockX, blockY);

        Assert.AreNotEqual(-1, blockIndex, "The test player must be inside the blockmap.");
        Assert.IsTrue(ContainsThing(blockMap.ThingLists[blockIndex], player),
            "The test player must be linked into the blockmap before MovePlane runs.");
        Assert.IsTrue(sector.CeilingHeight - sector.FloorHeight < player.Height,
            "The test sector must be too short for the player.");

        // Restrict P_ChangeSector to the exact cell containing the test player.
        sector.BlockBox[Box.Left] = blockX;
        sector.BlockBox[Box.Right] = blockX;
        sector.BlockBox[Box.Bottom] = blockY;
        sector.BlockBox[Box.Top] = blockY;
    }

    private static bool ContainsThing(Mobj head, Mobj expected)
    {
        for (var thing = head; thing != null; thing = thing.BlockNext)
        {
            if (ReferenceEquals(thing, expected))
            {
                return true;
            }
        }

        return false;
    }
}
