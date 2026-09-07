using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Pushers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomPointPusherTest
{
    private const short TestTag = 32000;

    [TestMethod]
    public void TranslatorMatchesBoomPointMagnitudeAndFalloffRules()
    {
        var magnitude = BoomPusherTranslator.ResolvePointMagnitude(
            Fixed.FromInt(64),
            Fixed.Zero);

        Assert.AreEqual(64, magnitude);
        Assert.AreEqual(64 << 8, BoomPusherTranslator.ResolvePointSpeed(
            magnitude,
            Fixed.Zero,
            Fixed.Zero).Data);
        Assert.AreEqual(32 << 8, BoomPusherTranslator.ResolvePointSpeed(
            magnitude,
            Fixed.FromInt(64),
            Fixed.Zero).Data);
        Assert.AreEqual(1 << 8, BoomPusherTranslator.ResolvePointSpeed(
            magnitude,
            Fixed.FromInt(127),
            Fixed.Zero).Data);
        Assert.AreEqual(Fixed.Zero.Data, BoomPusherTranslator.ResolvePointSpeed(
            magnitude,
            Fixed.FromInt(128),
            Fixed.Zero).Data);
    }

    [TestMethod]
    public void PointMagnitudeUsesIntegerLinedefComponents()
    {
        var dx = new Fixed((64 << Fixed.FracBits) + Fixed.FracUnit - 1);
        var dy = new Fixed(-(32 << Fixed.FracBits) + 1);

        var magnitude = BoomPusherTranslator.ResolvePointMagnitude(dx, dy);

        // Boom truncates linedef components to integer map units first:
        // 64 + 32 - (32 / 2) = 80.
        Assert.AreEqual(80, magnitude);
    }

    [TestMethod]
    public void ResolveCreatesPusherForTaggedSectorAndSource()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var source = FindSourcePointInPlayerSector(world);
        var sector = source.Sector;

        ConfigurePointPusherMap(
            world,
            line,
            sector,
            source.X,
            source.Y,
            BoomPointPusher.PushSourceThingType);

        var pushers = BoomPointPusher.Resolve(world);

        Assert.AreEqual(1, pushers.Length);
        Assert.AreSame(sector, pushers[0].SourceSector);
        Assert.AreEqual(source.X.Data, pushers[0].SourceX.Data);
        Assert.AreEqual(source.Y.Data, pushers[0].SourceY.Data);
        Assert.IsTrue(pushers[0].PushesAway);
        Assert.AreEqual(
            BoomPusherTranslator.ResolvePointMagnitude(line.Dx, line.Dy),
            pushers[0].Magnitude);
    }

    [TestMethod]
    public void ResolveUsesLastEligiblePointSourceInSector()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var source = FindSourcePointInPlayerSector(world);
        var sector = source.Sector;
        var things = world.Map.Things;

        Assert.IsTrue(things.Length >= 2);

        line.Special = (LineSpecial)226;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        world.Map.BoomTags.Rebuild();

        things[^2] = new MapThing(
            source.X,
            source.Y,
            Angle.Ang0,
            BoomPointPusher.PushSourceThingType,
            ThingFlags.Easy | ThingFlags.Normal | ThingFlags.Hard);
        things[^1] = new MapThing(
            source.X,
            source.Y,
            Angle.Ang0,
            BoomPointPusher.PullSourceThingType,
            ThingFlags.Easy | ThingFlags.Normal | ThingFlags.Hard);

        var pushers = BoomPointPusher.Resolve(world);

        Assert.AreEqual(1, pushers.Length);
        Assert.IsFalse(pushers[0].PushesAway);
    }

    [TestMethod]
    public void PushAndPullApplyOppositeMomentum()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var source = FindSourcePointInPlayerSector(world);
        var sector = source.Sector;

        ConfigurePlayer(thing, sector);

        var pull = new BoomPointPusher(
            world,
            sector,
            source.X,
            source.Y,
            64,
            false);

        pull.Apply(thing);
        var pullX = thing.MomX;
        var pullY = thing.MomY;

        Assert.IsTrue(pullX != Fixed.Zero || pullY != Fixed.Zero);

        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;

        var push = new BoomPointPusher(
            world,
            sector,
            source.X,
            source.Y,
            64,
            true);

        push.Apply(thing);

        Assert.IsTrue(System.Math.Abs(pullX.Data + thing.MomX.Data) <= 1);
        Assert.IsTrue(System.Math.Abs(pullY.Data + thing.MomY.Data) <= 1);
    }

    [TestMethod]
    public void PointPusherStopsAtBoomRadius()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        ConfigurePlayer(thing, sector);

        var pusher = new BoomPointPusher(
            world,
            sector,
            thing.X + Fixed.FromInt(128),
            thing.Y,
            64,
            false);

        pusher.Apply(thing);

        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomY.Data);
    }

    [TestMethod]
    public void PushMaskCanDisableAndReenablePointPusher()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var source = FindSourcePointInPlayerSector(world);
        var sector = source.Sector;
        var pusher = new BoomPointPusher(
            world,
            sector,
            source.X,
            source.Y,
            64,
            false);

        ConfigurePlayer(thing, sector);
        sector.Special = 0;

        pusher.Apply(thing);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);

        sector.Special = (SectorSpecial)BoomPusherTranslator.PushMask;
        pusher.Apply(thing);
        Assert.IsTrue(thing.MomX != Fixed.Zero || thing.MomY != Fixed.Zero);
    }

    [TestMethod]
    public void NoClipAndNoGravityPlayersIgnorePointPusher()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var source = FindSourcePointInPlayerSector(world);
        var sector = source.Sector;
        var pusher = new BoomPointPusher(
            world,
            sector,
            source.X,
            source.Y,
            64,
            false);

        ConfigurePlayer(thing, sector);

        thing.Flags |= MobjFlags.NoClip;
        pusher.Apply(thing);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);

        thing.Flags &= ~MobjFlags.NoClip;
        thing.Flags |= MobjFlags.NoGravity;
        pusher.Apply(thing);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);
    }

    [TestMethod]
    public void BoomSpawnMapThingAcceptsPointSourceMarkers()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;

        world.ThingAllocation.SpawnMapThing(
            new MapThing(
                thing.X,
                thing.Y,
                Angle.Ang0,
                BoomPointPusher.PushSourceThingType,
                ThingFlags.Normal));

        world.ThingAllocation.SpawnMapThing(
            new MapThing(
                thing.X,
                thing.Y,
                Angle.Ang0,
                BoomPointPusher.PullSourceThingType,
                ThingFlags.Normal));
    }

    [TestMethod]
    public void PlayerMobjRunAppliesResolvedBoomPointPusher()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var line = FindUnusedLine(world);
        var source = FindSourcePointInPlayerSector(world);
        var sector = source.Sector;

        ConfigurePointPusherMap(
            world,
            line,
            sector,
            source.X,
            source.Y,
            BoomPointPusher.PullSourceThingType);
        ConfigurePlayer(thing, sector);

        world.Specials.SpawnSpecials();

        thing.Run();

        Assert.IsTrue(thing.MomX != Fixed.Zero || thing.MomY != Fixed.Zero);
    }

    [TestMethod]
    public void VanillaDoesNotResolveBoomPointPushers()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var line = FindUnusedLine(world);
        var source = FindSourcePointInPlayerSector(world);
        var sector = source.Sector;

        ConfigurePointPusherMap(
            world,
            line,
            sector,
            source.X,
            source.Y,
            BoomPointPusher.PushSourceThingType);

        var pushers = BoomPointPusher.Resolve(world);

        Assert.AreEqual(0, pushers.Length);
    }

    private static void ConfigurePointPusherMap(
        World world,
        LineDef line,
        Sector sector,
        Fixed sourceX,
        Fixed sourceY,
        int sourceType)
    {
        line.Special = (LineSpecial)226;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        sector.Special = (SectorSpecial)BoomPusherTranslator.PushMask;
        world.Map.BoomTags.Rebuild();

        world.Map.Things[^1] = new MapThing(
            sourceX,
            sourceY,
            Angle.Ang0,
            sourceType,
            ThingFlags.Easy | ThingFlags.Normal | ThingFlags.Hard);
    }

    private static void ConfigurePlayer(Mobj thing, Sector sector)
    {
        Assert.AreSame(sector, thing.Subsector.Sector);

        thing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;
        sector.Special = (SectorSpecial)BoomPusherTranslator.PushMask;
    }

    private static (Fixed X, Fixed Y, Sector Sector) FindSourcePointInPlayerSector(World world)
    {
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;
        var offsets = new[] { 8, 16, 24, 32, -8, -16, -24, -32 };

        foreach (var offset in offsets)
        {
            var x = thing.X + Fixed.FromInt(offset);
            var y = thing.Y;

            if (ReferenceEquals(Geometry.PointInSubsector(x, y, world.Map).Sector, sector) &&
                world.VisibilityCheck.CheckSightToPoint(
                    thing,
                    x,
                    y,
                    sector.FloorHeight,
                    new Fixed(8),
                    sector))
            {
                return (x, y, sector);
            }
        }

        foreach (var offset in offsets)
        {
            var x = thing.X;
            var y = thing.Y + Fixed.FromInt(offset);

            if (ReferenceEquals(Geometry.PointInSubsector(x, y, world.Map).Sector, sector) &&
                world.VisibilityCheck.CheckSightToPoint(
                    thing,
                    x,
                    y,
                    sector.FloorHeight,
                    new Fixed(8),
                    sector))
            {
                return (x, y, sector);
            }
        }

        Assert.Fail("Could not find a nearby point in the player's sector.");
        return default;
    }

    private static LineDef FindUnusedLine(World world)
    {
        return world.Map.Lines.First(line =>
            line.FrontSide != null &&
            (int)line.Special == 0 &&
            BoomPusherTranslator.ResolvePointMagnitude(line.Dx, line.Dy) >= 64);
    }
}
