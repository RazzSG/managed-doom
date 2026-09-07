using System;
using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Sectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomClassicMoverCompatibilityTest
{
    [TestMethod]
    public void ClassicMoverFixesAreBoomFamilyOnly()
    {
        Assert.IsFalse(BoomClassicMoverCompatibility.UsesFixedMultiTaggedStairScan(
            GameCompatibility.Vanilla));
        Assert.IsFalse(BoomClassicMoverCompatibility.RemovesBouncedPureRaisePlatform(
            GameCompatibility.Vanilla));

        foreach (var compatibility in new[]
                 {
                     GameCompatibility.Boom,
                     GameCompatibility.Mbf,
                     GameCompatibility.Mbf21
                 })
        {
            Assert.IsTrue(BoomClassicMoverCompatibility.UsesFixedMultiTaggedStairScan(
                compatibility));
            Assert.IsTrue(BoomClassicMoverCompatibility.RemovesBouncedPureRaisePlatform(
                compatibility));
        }
    }

    [TestMethod]
    public void VanillaClassicStairsCanSkipLaterTaggedStarter()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Vanilla);
        var (first, laterTagged, chained, trigger) = PrepareMultiTaggedStairCase(world);

        Assert.IsTrue(world.SectorAction.BuildStairs(trigger, StairType.Build8));

        Assert.IsNotNull(first.SpecialData as FloorMove);
        Assert.IsNotNull(chained.SpecialData as FloorMove);
        Assert.IsNull(laterTagged.SpecialData);
    }

    [TestMethod]
    public void BoomClassicStairsStartEveryTaggedStarter()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);
        var (first, laterTagged, chained, trigger) = PrepareMultiTaggedStairCase(world);

        Assert.IsTrue(world.SectorAction.BuildStairs(trigger, StairType.Build8));

        var firstMove = first.SpecialData as FloorMove;
        var laterMove = laterTagged.SpecialData as FloorMove;
        var chainedMove = chained.SpecialData as FloorMove;

        Assert.IsNotNull(firstMove);
        Assert.IsNotNull(laterMove);
        Assert.IsNotNull(chainedMove);
        Assert.AreEqual(Fixed.FromInt(8).Data, firstMove.FloorDestHeight.Data);
        Assert.AreEqual(Fixed.FromInt(8).Data, laterMove.FloorDestHeight.Data);
        Assert.AreEqual(Fixed.FromInt(16).Data, chainedMove.FloorDestHeight.Data);
    }

    [TestMethod]
    public void BoomActivePlatformRegistryIsNotLimitedToSixtyEntries()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);

        const short tag = 30000;
        var platforms = new Platform[128];

        for (var i = 0; i < platforms.Length; i++)
        {
            var platform = new Platform(world)
            {
                Tag = tag,
                Status = PlatformState.Up,
                ThinkerState = ThinkerState.Active
            };

            platforms[i] = platform;
            world.SectorAction.AddActivePlatform(platform);
        }

        var triggerSector = CreateSector(0, 0, 0);
        var trigger = CreateLine(triggerSector, triggerSector, 0);
        trigger.Tag = tag;

        world.SectorAction.StopPlatform(trigger);

        foreach (var platform in platforms)
        {
            Assert.AreEqual(PlatformState.InStasis, platform.Status);
            Assert.AreEqual(PlatformState.Up, platform.OldStatus);
            Assert.AreEqual(ThinkerState.InStasis, platform.ThinkerState);
        }

        world.SectorAction.ActivateInStasis(tag);

        foreach (var platform in platforms)
        {
            Assert.AreEqual(PlatformState.Up, platform.Status);
            Assert.AreEqual(ThinkerState.Active, platform.ThinkerState);
        }
    }

    [TestMethod]
    public void BoomRemovesPureRaisePlatformAfterItBouncesBackToLow()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);
        var sector = world.Map.Sectors.First(s =>
            s.SpecialData == null &&
            s.ThingList == null &&
            s.TouchingThingList == null);

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
        sector.SpecialData = platform;
        world.SectorAction.AddActivePlatform(platform);

        platform.Run();

        Assert.IsNull(sector.SpecialData);
        Assert.AreEqual(low.Data, sector.FloorHeight.Data);
    }

    private static World CreateWorld(GameContent content, GameCompatibility compatibility)
    {
        return new World(content, new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = compatibility
        }, null);
    }

    private static (Sector first, Sector laterTagged, Sector chained, LineDef trigger)
        PrepareMultiTaggedStairCase(World world)
    {
        foreach (var sector in world.Map.Sectors)
            sector.Tag = 0;

        const short tag = 30000;
        var first = CreateSector(0, 0, tag);
        var laterTagged = CreateSector(1, 0, tag);
        var chained = CreateSector(2, 0, 0);

        first.Lines = new[] { CreateLine(first, chained, LineFlags.TwoSided) };
        laterTagged.Lines = Array.Empty<LineDef>();
        chained.Lines = Array.Empty<LineDef>();

        world.Map.Sectors[0] = first;
        world.Map.Sectors[1] = laterTagged;
        world.Map.Sectors[2] = chained;
        world.Map.BoomTags.Rebuild();

        var trigger = CreateLine(first, laterTagged, 0);
        trigger.Tag = tag;

        return (first, laterTagged, chained, trigger);
    }

    private static Sector CreateSector(int number, int floorHeight, int tag)
    {
        return new Sector(
            number,
            Fixed.FromInt(floorHeight),
            Fixed.FromInt(128),
            0,
            0,
            128,
            SectorSpecial.Normal,
            tag)
        {
            Lines = Array.Empty<LineDef>()
        };
    }

    private static LineDef CreateLine(Sector front, Sector back, LineFlags flags)
    {
        var frontSide = new SideDef(Fixed.Zero, Fixed.Zero, 0, 0, 0, front);
        var backSide = new SideDef(Fixed.Zero, Fixed.Zero, 0, 0, 0, back);

        return new LineDef(
            new Vertex(Fixed.Zero, Fixed.Zero),
            new Vertex(Fixed.FromInt(64), Fixed.Zero),
            flags,
            (LineSpecial)0,
            0,
            frontSide,
            backSide);
    }
}
