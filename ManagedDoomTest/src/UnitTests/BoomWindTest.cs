using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Pushers;
using ManagedDoom.Compatibility.Boom.Sectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomWindTest
{
    private const short TestTag = 32000;

    [TestMethod]
    public void TranslatorMatchesBoomIntegerAndGroundRoundingRules()
    {
        var wind = BoomPusherTranslator.ResolveWind(Fixed.FromInt(3), Fixed.FromInt(-3));

        Assert.AreEqual(3 << 9, wind.FullX.Data);
        Assert.AreEqual(-3 << 9, wind.FullY.Data);
        Assert.AreEqual(1 << 9, wind.GroundX.Data);
        Assert.AreEqual(-2 << 9, wind.GroundY.Data);
    }

    [TestMethod]
    public void InitializeAccumulatesAllWindLinesForTaggedSector()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var lines = world.Map.Lines.Where(line => line.FrontSide != null && (int)line.Special == 0).Take(2).ToArray();
        var sector = world.Map.Sectors[0];

        Assert.AreEqual(2, lines.Length);

        foreach (var line in lines)
        {
            line.Special = (LineSpecial)224;
            line.Tag = TestTag;
        }

        sector.Tag = TestTag;
        world.Map.BoomTags.Rebuild();

        BoomSectorWind.Initialize(world);

        var first = BoomPusherTranslator.ResolveWind(lines[0].Dx, lines[0].Dy);
        var second = BoomPusherTranslator.ResolveWind(lines[1].Dx, lines[1].Dy);

        Assert.AreEqual((first.FullX + second.FullX).Data, sector.WindAboveX.Data);
        Assert.AreEqual((first.FullY + second.FullY).Data, sector.WindAboveY.Data);
        Assert.AreEqual((first.GroundX + second.GroundX).Data, sector.WindGroundX.Data);
        Assert.AreEqual((first.GroundY + second.GroundY).Data, sector.WindGroundY.Data);
    }

    [TestMethod]
    public void WindTargetsAreResolvedAtMapStartup()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var target = world.Map.Sectors[0];
        var replacement = world.Map.Sectors.First(sector => !ReferenceEquals(sector, target));

        line.Special = (LineSpecial)224;
        line.Tag = TestTag;
        target.Tag = TestTag;
        replacement.Tag = TestTag - 1;
        world.Map.BoomTags.Rebuild();

        BoomSectorWind.Initialize(world);
        var expected = BoomPusherTranslator.ResolveWind(line.Dx, line.Dy);

        target.Tag = TestTag - 1;
        replacement.Tag = TestTag;
        world.Map.BoomTags.Rebuild();

        Assert.AreEqual(expected.FullX.Data, target.WindAboveX.Data);
        Assert.AreEqual(expected.FullY.Data, target.WindAboveY.Data);
        Assert.AreEqual(Fixed.Zero.Data, replacement.WindAboveX.Data);
        Assert.AreEqual(Fixed.Zero.Data, replacement.WindAboveY.Data);
    }

    [TestMethod]
    public void GroundedPlayerGetsHalfWindForce()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        ConfigurePlayerForWind(thing, sector);
        sector.WindGroundX = new Fixed(512);
        sector.WindGroundY = new Fixed(-1024);
        sector.WindAboveX = new Fixed(1536);
        sector.WindAboveY = new Fixed(-1536);
        thing.Z = thing.FloorZ;

        BoomSectorWind.Apply(thing);

        Assert.AreEqual(512, thing.MomX.Data);
        Assert.AreEqual(-1024, thing.MomY.Data);
        Assert.IsTrue(thing.MbfScrollingMovement);
    }

    [TestMethod]
    public void AirbornePlayerGetsFullWindForce()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        ConfigurePlayerForWind(thing, sector);
        sector.WindGroundX = new Fixed(512);
        sector.WindGroundY = new Fixed(-1024);
        sector.WindAboveX = new Fixed(1536);
        sector.WindAboveY = new Fixed(-1536);
        thing.Z = thing.FloorZ + Fixed.One;

        BoomSectorWind.Apply(thing);

        Assert.AreEqual(1536, thing.MomX.Data);
        Assert.AreEqual(-1536, thing.MomY.Data);
    }

    [TestMethod]
    public void TouchingWindSectorAffectsAirbornePlayerWhenOriginIsInNeighborSector()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var origin = thing.Subsector.Sector;
        var windSector = FindUntouchedSector(world, thing, origin);

        ConfigurePlayerForWind(thing, origin);
        origin.Special = 0;
        windSector.Special = (SectorSpecial)BoomPusherTranslator.PushMask;
        windSector.WindAboveX = new Fixed(1536);
        windSector.WindAboveY = new Fixed(-1024);
        thing.Z = thing.FloorZ + Fixed.One;
        PrependTouchingSector(thing, windSector);

        BoomSectorWind.Apply(thing);

        Assert.AreEqual(1536, thing.MomX.Data);
        Assert.AreEqual(-1024, thing.MomY.Data);
    }

    [TestMethod]
    public void HeightSectorWindMatchesPrBoomWaterSurfaceRules()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;
        var control = world.Map.Sectors.First(candidate => !ReferenceEquals(candidate, sector));

        ConfigurePlayerForWind(thing, sector);
        sector.WindGroundX = new Fixed(512);
        sector.WindAboveX = new Fixed(1536);
        control.FloorHeight = Fixed.FromInt(64);
        sector.HeightSector = control;

        thing.Z = Fixed.FromInt(80);
        thing.Player.ViewZ = Fixed.FromInt(100);
        BoomSectorWind.Apply(thing);
        Assert.AreEqual(1536, thing.MomX.Data);

        thing.MomX = Fixed.Zero;
        thing.Z = Fixed.FromInt(48);
        thing.Player.ViewZ = Fixed.FromInt(80);
        BoomSectorWind.Apply(thing);
        Assert.AreEqual(512, thing.MomX.Data);

        thing.MomX = Fixed.Zero;
        thing.Player.ViewZ = Fixed.FromInt(48);
        BoomSectorWind.Apply(thing);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);
    }

    [TestMethod]
    public void PushMaskCanDisableAndReenableWindAtRuntime()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        ConfigurePlayerForWind(thing, sector);
        sector.WindGroundX = new Fixed(512);
        sector.Special = 0;

        BoomSectorWind.Apply(thing);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);

        sector.Special = (SectorSpecial)BoomPusherTranslator.PushMask;
        BoomSectorWind.Apply(thing);
        Assert.AreEqual(512, thing.MomX.Data);
    }

    [TestMethod]
    public void NoClipAndNoGravityPlayersIgnoreWind()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        ConfigurePlayerForWind(thing, sector);
        sector.WindGroundX = new Fixed(512);

        thing.Flags |= MobjFlags.NoClip;
        BoomSectorWind.Apply(thing);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);

        thing.Flags &= ~MobjFlags.NoClip;
        thing.Flags |= MobjFlags.NoGravity;
        BoomSectorWind.Apply(thing);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);
    }

    [TestMethod]
    public void PlayerMobjRunAppliesBoomWindAfterNormalMovement()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        ConfigurePlayerForWind(thing, sector);
        sector.WindGroundX = new Fixed(512);
        sector.WindGroundY = new Fixed(1024);
        thing.Z = thing.FloorZ;

        thing.Run();

        Assert.AreEqual(512, thing.MomX.Data);
        Assert.AreEqual(1024, thing.MomY.Data);
    }

    [TestMethod]
    public void VanillaPlayerMobjRunIgnoresBoomWindData()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        ConfigurePlayerForWind(thing, sector);
        sector.WindGroundX = new Fixed(512);
        sector.WindGroundY = new Fixed(1024);
        thing.Z = thing.FloorZ;

        thing.Run();

        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomY.Data);
    }

    private static void ConfigurePlayerForWind(Mobj thing, Sector sector)
    {
        thing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;
        thing.FloorZ = sector.FloorHeight;
        sector.Special = (SectorSpecial)BoomPusherTranslator.PushMask;
    }

    private static Sector FindUntouchedSector(World world, Mobj thing, Sector origin)
    {
        return world.Map.Sectors.First(sector =>
            !ReferenceEquals(sector, origin) &&
            !TouchesSector(thing, sector));
    }

    private static bool TouchesSector(Mobj thing, Sector sector)
    {
        for (var node = thing.TouchingSectorList; node != null; node = node.ThingNext)
        {
            if (ReferenceEquals(node.Sector, sector))
                return true;
        }

        return false;
    }

    private static void PrependTouchingSector(Mobj thing, Sector sector)
    {
        thing.TouchingSectorList = new BoomSectorTouchNode
        {
            Thing = thing,
            Sector = sector,
            ThingNext = thing.TouchingSectorList
        };
    }

    private static LineDef FindUnusedLine(World world)
    {
        return world.Map.Lines.First(line => line.FrontSide != null && (int)line.Special == 0);
    }
}
