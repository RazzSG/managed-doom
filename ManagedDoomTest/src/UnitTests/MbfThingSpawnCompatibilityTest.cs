using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Things;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfThingSpawnCompatibilityTest
{
    [TestMethod]
    public void MbfThingAndActorFlagsUseSpecificationBits()
    {
        Assert.AreEqual(0x0080, (int)ThingFlags.Friendly);
        Assert.AreEqual(0x0100, (int)ThingFlags.Reserved);
        Assert.AreEqual(0x10000000, (int)MobjFlags.Touchy);
        Assert.AreEqual(0x20000000, (int)MobjFlags.Bounces);
        Assert.AreEqual(0x40000000, (int)MobjFlags.Friend);
    }

    [TestMethod]
    public void MapThingParserPreservesMbfFlagBits()
    {
        var data = new byte[10];
        const int doomEdNum = 3001;
        data[6] = (byte)(doomEdNum & 0xFF);
        data[7] = (byte)(doomEdNum >> 8);
        var flags = (int)(ThingFlags.Normal | ThingFlags.Friendly | ThingFlags.Reserved);
        data[8] = (byte)(flags & 0xFF);
        data[9] = (byte)(flags >> 8);

        var thing = MapThing.FromData(data, 0);

        Assert.AreEqual(doomEdNum, thing.Type);
        Assert.IsTrue((thing.Flags & ThingFlags.Friendly) != 0);
        Assert.IsTrue((thing.Flags & ThingFlags.Reserved) != 0);
    }

    [TestMethod]
    public void MbfReservedThingBitClearsAllPostDoomMapFlags()
    {
        var flags =
            ThingFlags.Normal |
            ThingFlags.Ambush |
            ThingFlags.NotDeathmatch |
            ThingFlags.NotCooperative |
            ThingFlags.Friendly |
            ThingFlags.Reserved;

        var resolved = MbfThingSpawnCompatibility.ResolveMapFlags(
            GameCompatibility.Mbf,
            flags);

        Assert.AreEqual(ThingFlags.Normal | ThingFlags.Ambush, resolved);
    }

    [TestMethod]
    public void BoomDoesNotApplyMbfReservedBitSemantics()
    {
        var flags = ThingFlags.Normal | ThingFlags.Friendly | ThingFlags.Reserved;

        var resolved = MbfThingSpawnCompatibility.ResolveMapFlags(
            GameCompatibility.Boom,
            flags);

        Assert.AreEqual(flags, resolved);
    }

    [TestMethod]
    public void ThingAllocationAppliesFriendlyMapFlagInMbf()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);

        var spawned = SpawnAndFind(
            world,
            DoomInfo.MobjInfos[(int)MobjType.Troop].DoomEdNum,
            ThingFlags.Normal | ThingFlags.Friendly);

        Assert.IsTrue((spawned.Flags & MobjFlags.Friend) != 0);
    }

    [TestMethod]
    public void FriendlyMbfMonsterDoesNotIncreaseLevelKillTotal()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);

        var before = world.TotalKills;

        SpawnAndFind(
            world,
            DoomInfo.MobjInfos[(int)MobjType.Troop].DoomEdNum,
            ThingFlags.Normal | ThingFlags.Friendly);

        Assert.AreEqual(before, world.TotalKills);
    }

    [TestMethod]
    public void BoomDoesNotInterpretFriendlyMapFlag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Boom },
            null);

        var spawned = SpawnAndFind(
            world,
            DoomInfo.MobjInfos[(int)MobjType.Troop].DoomEdNum,
            ThingFlags.Normal | ThingFlags.Friendly);

        Assert.IsFalse((spawned.Flags & MobjFlags.Friend) != 0);
    }

    [TestMethod]
    public void ReservedBitAlsoClearsBoomModeFiltersBeforeSpawn()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);
        world.Options.NetGame = true;
        world.Options.Deathmatch = 1;

        var before = world.TotalItems;
        var player = world.ConsolePlayer.Mobj;
        var thing = new MapThing(
            player.X,
            player.Y,
            Angle.Ang0,
            2014,
            ThingFlags.Normal | ThingFlags.NotDeathmatch | ThingFlags.Reserved);

        world.ThingAllocation.SpawnMapThing(thing);

        Assert.AreEqual(before + 1, world.TotalItems);
    }

    private static Mobj SpawnAndFind(
        World world,
        int doomEdNum,
        ThingFlags flags)
    {
        var player = world.ConsolePlayer.Mobj;
        var mapThing = new MapThing(
            player.X,
            player.Y,
            Angle.Ang0,
            doomEdNum,
            flags);

        world.ThingAllocation.SpawnMapThing(mapThing);

        foreach (var thinker in world.Thinkers)
        {
            if (thinker is Mobj mobj && ReferenceEquals(mobj.SpawnPoint, mapThing))
                return mobj;
        }

        Assert.Fail($"No runtime mobj was spawned for DoomEdNum {doomEdNum}.");
        return null;
    }
}
