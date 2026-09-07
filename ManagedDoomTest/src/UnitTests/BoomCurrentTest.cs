using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Pushers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomCurrentTest
{
    private const short TestTag = 32000;

    [TestMethod]
    public void TranslatorMatchesBoomIntegerMagnitudeRules()
    {
        var current = BoomPusherTranslator.ResolveCurrent(Fixed.FromInt(3), Fixed.FromInt(-3));

        Assert.AreEqual(3 << 9, current.X.Data);
        Assert.AreEqual(-3 << 9, current.Y.Data);
    }

    [TestMethod]
    public void InitializeAccumulatesAllCurrentLinesForTaggedSector()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var lines = world.Map.Lines
            .Where(line =>
                line.FrontSide != null &&
                (int)line.Special == 0 &&
                ((line.Dx.Data >> Fixed.FracBits) != 0 || (line.Dy.Data >> Fixed.FracBits) != 0))
            .Take(2)
            .ToArray();
        var sector = world.Map.Sectors[0];

        Assert.AreEqual(2, lines.Length);

        foreach (var line in lines)
        {
            line.Special = (LineSpecial)225;
            line.Tag = TestTag;
        }

        sector.Tag = TestTag;
        world.Map.BoomTags.Rebuild();

        BoomSectorCurrent.Initialize(world);

        var first = BoomPusherTranslator.ResolveCurrent(lines[0].Dx, lines[0].Dy);
        var second = BoomPusherTranslator.ResolveCurrent(lines[1].Dx, lines[1].Dy);

        Assert.AreEqual((first.X + second.X).Data, sector.CurrentX.Data);
        Assert.AreEqual((first.Y + second.Y).Data, sector.CurrentY.Data);
    }

    [TestMethod]
    public void CurrentTargetsAreResolvedAtMapStartup()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var target = world.Map.Sectors[0];
        var replacement = world.Map.Sectors.First(sector => !ReferenceEquals(sector, target));

        line.Special = (LineSpecial)225;
        line.Tag = TestTag;
        target.Tag = TestTag;
        replacement.Tag = TestTag - 1;
        world.Map.BoomTags.Rebuild();

        BoomSectorCurrent.Initialize(world);
        var expected = BoomPusherTranslator.ResolveCurrent(line.Dx, line.Dy);

        target.Tag = TestTag - 1;
        replacement.Tag = TestTag;
        world.Map.BoomTags.Rebuild();

        Assert.AreEqual(expected.X.Data, target.CurrentX.Data);
        Assert.AreEqual(expected.Y.Data, target.CurrentY.Data);
        Assert.AreEqual(Fixed.Zero.Data, replacement.CurrentX.Data);
        Assert.AreEqual(Fixed.Zero.Data, replacement.CurrentY.Data);
    }

    [TestMethod]
    public void GroundedPlayerGetsFullCurrentForce()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        ConfigurePlayerForCurrent(thing, sector);
        sector.CurrentX = new Fixed(1536);
        sector.CurrentY = new Fixed(-1024);
        thing.Z = sector.FloorHeight;

        BoomSectorCurrent.Apply(thing);

        Assert.AreEqual(1536, thing.MomX.Data);
        Assert.AreEqual(-1024, thing.MomY.Data);
    }

    [TestMethod]
    public void AirbornePlayerGetsNoCurrentForce()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        ConfigurePlayerForCurrent(thing, sector);
        sector.CurrentX = new Fixed(1536);
        sector.CurrentY = new Fixed(-1024);
        thing.Z = sector.FloorHeight + Fixed.One;

        BoomSectorCurrent.Apply(thing);

        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomY.Data);
    }

    [TestMethod]
    public void CurrentUsesSectorFloorHeightRatherThanThingFloorZ()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        ConfigurePlayerForCurrent(thing, sector);
        sector.CurrentX = new Fixed(1024);
        thing.FloorZ = sector.FloorHeight + Fixed.FromInt(8);
        thing.Z = thing.FloorZ;

        BoomSectorCurrent.Apply(thing);

        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);
    }

    [TestMethod]
    public void PushMaskCanDisableAndReenableCurrentAtRuntime()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        ConfigurePlayerForCurrent(thing, sector);
        sector.CurrentX = new Fixed(512);
        thing.Z = sector.FloorHeight;
        sector.Special = 0;

        BoomSectorCurrent.Apply(thing);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);

        sector.Special = (SectorSpecial)BoomPusherTranslator.PushMask;
        BoomSectorCurrent.Apply(thing);
        Assert.AreEqual(512, thing.MomX.Data);
    }

    [TestMethod]
    public void NoClipAndNoGravityPlayersIgnoreCurrent()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        ConfigurePlayerForCurrent(thing, sector);
        sector.CurrentX = new Fixed(512);
        thing.Z = sector.FloorHeight;

        thing.Flags |= MobjFlags.NoClip;
        BoomSectorCurrent.Apply(thing);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);

        thing.Flags &= ~MobjFlags.NoClip;
        thing.Flags |= MobjFlags.NoGravity;
        BoomSectorCurrent.Apply(thing);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);
    }

    [TestMethod]
    public void PlayerMobjRunAppliesBoomCurrentAfterNormalMovement()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        ConfigurePlayerForCurrent(thing, sector);
        sector.CurrentX = new Fixed(512);
        sector.CurrentY = new Fixed(1024);
        thing.Z = sector.FloorHeight;

        thing.Run();

        Assert.AreEqual(512, thing.MomX.Data);
        Assert.AreEqual(1024, thing.MomY.Data);
    }

    [TestMethod]
    public void VanillaPlayerMobjRunIgnoresBoomCurrentData()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        ConfigurePlayerForCurrent(thing, sector);
        sector.CurrentX = new Fixed(512);
        sector.CurrentY = new Fixed(1024);
        thing.Z = sector.FloorHeight;

        thing.Run();

        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomY.Data);
    }

    private static void ConfigurePlayerForCurrent(Mobj thing, Sector sector)
    {
        thing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;
        thing.FloorZ = sector.FloorHeight;
        sector.Special = (SectorSpecial)BoomPusherTranslator.PushMask;
    }

    private static LineDef FindUnusedLine(World world)
    {
        return world.Map.Lines.First(line =>
            line.FrontSide != null &&
            (int)line.Special == 0 &&
            ((line.Dx.Data >> Fixed.FracBits) != 0 || (line.Dy.Data >> Fixed.FracBits) != 0));
    }
}
