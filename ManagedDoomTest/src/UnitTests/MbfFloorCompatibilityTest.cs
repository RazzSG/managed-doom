using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Sectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfFloorCompatibilityTest
{
    [TestMethod]
    public void SelectorMatchesVanillaBoomMbfAndMbf21Boundaries()
    {
        Assert.IsTrue(MbfFloorCompatibility.UsesDoomFloorSemantics(
            GameCompatibility.Vanilla, compFloors: false));
        Assert.IsTrue(MbfFloorCompatibility.UsesDoomFloorSemantics(
            GameCompatibility.Vanilla, compFloors: true));

        Assert.IsFalse(MbfFloorCompatibility.UsesDoomFloorSemantics(
            GameCompatibility.Boom, compFloors: false));
        Assert.IsFalse(MbfFloorCompatibility.UsesDoomFloorSemantics(
            GameCompatibility.Boom, compFloors: true));

        Assert.IsFalse(MbfFloorCompatibility.UsesDoomFloorSemantics(
            GameCompatibility.Mbf, compFloors: false));
        Assert.IsTrue(MbfFloorCompatibility.UsesDoomFloorSemantics(
            GameCompatibility.Mbf, compFloors: true));

        Assert.IsFalse(MbfFloorCompatibility.UsesDoomFloorSemantics(
            GameCompatibility.Mbf21, compFloors: false));
        Assert.IsTrue(MbfFloorCompatibility.UsesDoomFloorSemantics(
            GameCompatibility.Mbf21, compFloors: true));
    }

    [TestMethod]
    public void SelectorCoversAllPrBoomCompFloorsBranches()
    {
        Assert.IsFalse(MbfFloorCompatibility.ShouldClampRaisingFloorDestinationToCeiling(
            GameCompatibility.Vanilla, false));
        Assert.IsTrue(MbfFloorCompatibility.ShouldClampRaisingFloorDestinationToCeiling(
            GameCompatibility.Boom, true));
        Assert.IsTrue(MbfFloorCompatibility.ShouldClampRaisingFloorDestinationToCeiling(
            GameCompatibility.Mbf, false));
        Assert.IsFalse(MbfFloorCompatibility.ShouldClampRaisingFloorDestinationToCeiling(
            GameCompatibility.Mbf, true));

        Assert.IsTrue(MbfFloorCompatibility.ShouldRestoreIntermediateLoweringFloorAfterNoFit(
            GameCompatibility.Mbf, true));
        Assert.IsFalse(MbfFloorCompatibility.ShouldRestoreIntermediateLoweringFloorAfterNoFit(
            GameCompatibility.Mbf, false));

        Assert.IsFalse(MbfFloorCompatibility.ShouldRestoreIntermediateRaisingFloorAfterNoFit(
            GameCompatibility.Mbf, true, crush: true));
        Assert.IsTrue(MbfFloorCompatibility.ShouldRestoreIntermediateRaisingFloorAfterNoFit(
            GameCompatibility.Mbf, false, crush: true));
        Assert.IsTrue(MbfFloorCompatibility.ShouldRestoreIntermediateRaisingFloorAfterNoFit(
            GameCompatibility.Mbf, true, crush: false));

        Assert.IsTrue(MbfFloorCompatibility.RemovesBouncedPureRaisePlatform(
            GameCompatibility.Mbf, false));
        Assert.IsFalse(MbfFloorCompatibility.RemovesBouncedPureRaisePlatform(
            GameCompatibility.Mbf, true));

        Assert.IsTrue(MbfFloorCompatibility.BlocksDonutWhenPoolFloorIsBusy(
            GameCompatibility.Mbf, false));
        Assert.IsFalse(MbfFloorCompatibility.BlocksDonutWhenPoolFloorIsBusy(
            GameCompatibility.Mbf, true));
    }

    [TestMethod]
    public void MbfCompFloorsControlsFloorAndCeilingCrossing()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var fixedWorld = CreateWorld(content, GameCompatibility.Mbf, compFloors: false);
        var fixedSector = CreateDetachedSector(100, 100);
        var fixedFloorResult = fixedWorld.SectorAction.MovePlane(
            fixedSector,
            Fixed.FromInt(4),
            Fixed.FromInt(128),
            false,
            0,
            1);

        Assert.AreEqual(SectorActionResult.PastDestination, fixedFloorResult);
        Assert.AreEqual(Fixed.FromInt(100).Data, fixedSector.FloorHeight.Data);

        var doomWorld = CreateWorld(content, GameCompatibility.Mbf, compFloors: true);
        var doomSector = CreateDetachedSector(100, 100);
        var doomFloorResult = doomWorld.SectorAction.MovePlane(
            doomSector,
            Fixed.FromInt(4),
            Fixed.FromInt(128),
            false,
            0,
            1);

        Assert.AreEqual(SectorActionResult.OK, doomFloorResult);
        Assert.AreEqual(Fixed.FromInt(104).Data, doomSector.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(100).Data, doomSector.CeilingHeight.Data);

        var fixedCeiling = CreateDetachedSector(100, 100);
        var fixedCeilingResult = fixedWorld.SectorAction.MovePlane(
            fixedCeiling,
            Fixed.FromInt(4),
            Fixed.Zero,
            false,
            1,
            -1);

        Assert.AreEqual(SectorActionResult.PastDestination, fixedCeilingResult);
        Assert.AreEqual(Fixed.FromInt(100).Data, fixedCeiling.CeilingHeight.Data);

        var doomCeiling = CreateDetachedSector(100, 100);
        var doomCeilingResult = doomWorld.SectorAction.MovePlane(
            doomCeiling,
            Fixed.FromInt(4),
            Fixed.Zero,
            false,
            1,
            -1);

        Assert.AreEqual(SectorActionResult.OK, doomCeilingResult);
        Assert.AreEqual(Fixed.FromInt(96).Data, doomCeiling.CeilingHeight.Data);
        Assert.AreEqual(Fixed.FromInt(100).Data, doomCeiling.FloorHeight.Data);
    }

    [TestMethod]
    public void MbfCompFloorsControlsBouncedPureRaisePlatformCleanup()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var fixedWorld = CreateWorld(content, GameCompatibility.Mbf, compFloors: false);
        var fixedSector = FindFreeSector(fixedWorld);
        var fixedPlatform = CreateBouncedPureRaisePlatform(fixedWorld, fixedSector);
        fixedPlatform.Run();

        Assert.IsNull(fixedSector.FloorData);

        var doomWorld = CreateWorld(content, GameCompatibility.Mbf, compFloors: true);
        var doomSector = FindFreeSector(doomWorld);
        var doomPlatform = CreateBouncedPureRaisePlatform(doomWorld, doomSector);
        doomPlatform.Run();

        Assert.AreSame(doomPlatform, doomSector.FloorData);
        Assert.AreEqual(PlatformState.Waiting, doomPlatform.Status);
    }

    [TestMethod]
    public void MbfCompFloorsControlsBusyDonutPoolCompatibilityBug()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom1);

        var fixedWorld = CreateWorld(content, GameCompatibility.Mbf, compFloors: false, episode: 1, map: 2);
        var fixedLine = FindVanillaDonutLine(fixedWorld);
        var fixedPool = FindDonutPool(fixedWorld, fixedLine);
        var fixedBlocker = new Thinker();
        fixedPool.FloorData = fixedBlocker;

        Assert.IsFalse(fixedWorld.SectorAction.DoDonut(fixedLine));
        Assert.AreSame(fixedBlocker, fixedPool.FloorData);
        Assert.IsFalse(fixedWorld.Map.Sectors.Any(s => s.Tag == fixedLine.Tag && s.FloorData is FloorMove));

        var doomWorld = CreateWorld(content, GameCompatibility.Mbf, compFloors: true, episode: 1, map: 2);
        var doomLine = FindVanillaDonutLine(doomWorld);
        var doomPool = FindDonutPool(doomWorld, doomLine);
        doomPool.FloorData = new Thinker();

        Assert.IsTrue(doomWorld.SectorAction.DoDonut(doomLine));
        Assert.IsTrue(doomPool.FloorData is FloorMove);
        Assert.IsTrue(doomWorld.Map.Sectors.Any(s => s.Tag == doomLine.Tag && s.FloorData is FloorMove));
    }

    private static World CreateWorld(
        GameContent content,
        GameCompatibility compatibility,
        bool compFloors,
        int episode = 1,
        int map = 1)
    {
        var options = new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Episode = episode,
            Map = map,
            Compatibility = compatibility
        };
        options.MbfOptions.CompFloors = compFloors;
        return new World(content, options, null);
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
            BlockBox = new[] { -1, -1, -1, -1 }
        };
    }

    private static Sector FindFreeSector(World world)
    {
        return world.Map.Sectors.First(s =>
            s.FloorData == null &&
            s.CeilingData == null &&
            s.ThingList == null &&
            s.TouchingThingList == null);
    }

    private static Platform CreateBouncedPureRaisePlatform(World world, Sector sector)
    {
        var low = sector.FloorHeight;
        var platform = new Platform(world)
        {
            Sector = sector,
            Speed = Fixed.One,
            Low = low,
            High = low + Fixed.FromInt(24),
            Wait = 0,
            Status = PlatformState.Down,
            Type = PlatformType.RaiseAndChange,
            Tag = 30000
        };

        world.Thinkers.Add(platform);
        sector.FloorData = platform;
        world.SectorAction.AddActivePlatform(platform);
        return platform;
    }

    private static LineDef FindVanillaDonutLine(World world)
    {
        return world.Map.Lines.First(l => (int)l.Special == 9 && l.Tag != 0);
    }

    private static Sector FindDonutPool(World world, LineDef line)
    {
        var pillar = world.Map.Sectors.First(s => s.Tag == line.Tag);
        var edge = pillar.Lines[0];
        return edge.FrontSector == pillar ? edge.BackSector : edge.FrontSector;
    }
}
