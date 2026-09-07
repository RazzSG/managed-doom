using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomThingSpawnFlagsTest
{
    private const int HealthBonusThingType = 2014;

    [TestMethod]
    public void BoomThingFlagsUseExpectedBits()
    {
        Assert.AreEqual(0x10, (int)ThingFlags.MultiplayerOnly);
        Assert.AreEqual(0x20, (int)ThingFlags.NotDeathmatch);
        Assert.AreEqual(0x40, (int)ThingFlags.NotCooperative);
    }

    [TestMethod]
    public void MapThingParserPreservesBoomSpawnFlags()
    {
        var data = new byte[10];
        data[6] = (byte)(HealthBonusThingType & 0xFF);
        data[7] = (byte)(HealthBonusThingType >> 8);
        data[8] = (byte)((int)(ThingFlags.Normal | ThingFlags.NotDeathmatch | ThingFlags.NotCooperative));

        var thing = MapThing.FromData(data, 0);

        Assert.AreEqual(HealthBonusThingType, thing.Type);
        Assert.IsTrue((thing.Flags & ThingFlags.Normal) != 0);
        Assert.IsTrue((thing.Flags & ThingFlags.NotDeathmatch) != 0);
        Assert.IsTrue((thing.Flags & ThingFlags.NotCooperative) != 0);
    }

    [TestMethod]
    public void SinglePlayerUsesVanillaMultiplayerOnlyFlag()
    {
        var options = CreateOptions(GameCompatibility.Boom, false, 0);

        Assert.IsFalse(BoomThingSpawnFilter.IsAllowed(options, ThingFlags.MultiplayerOnly));
        Assert.IsTrue(BoomThingSpawnFilter.IsAllowed(options, ThingFlags.NotDeathmatch));
        Assert.IsTrue(BoomThingSpawnFilter.IsAllowed(options, ThingFlags.NotCooperative));
    }

    [TestMethod]
    public void BoomDeathmatchHonorsNotDeathmatchOnly()
    {
        var options = CreateOptions(GameCompatibility.Boom, true, 1);

        Assert.IsFalse(BoomThingSpawnFilter.IsAllowed(options, ThingFlags.NotDeathmatch));
        Assert.IsTrue(BoomThingSpawnFilter.IsAllowed(options, ThingFlags.NotCooperative));
    }

    [TestMethod]
    public void BoomCooperativeHonorsNotCooperativeOnly()
    {
        var options = CreateOptions(GameCompatibility.Boom, true, 0);

        Assert.IsTrue(BoomThingSpawnFilter.IsAllowed(options, ThingFlags.NotDeathmatch));
        Assert.IsFalse(BoomThingSpawnFilter.IsAllowed(options, ThingFlags.NotCooperative));
    }

    [TestMethod]
    public void VanillaIgnoresBoomMultiplayerFilters()
    {
        var deathmatch = CreateOptions(GameCompatibility.Vanilla, true, 1);
        var cooperative = CreateOptions(GameCompatibility.Vanilla, true, 0);

        Assert.IsTrue(BoomThingSpawnFilter.IsAllowed(deathmatch, ThingFlags.NotDeathmatch));
        Assert.IsTrue(BoomThingSpawnFilter.IsAllowed(cooperative, ThingFlags.NotCooperative));
    }

    [TestMethod]
    public void MbfAndMbf21InheritBoomThingSpawnFlags()
    {
        var mbfDeathmatch = CreateOptions(GameCompatibility.Mbf, true, 1);
        var mbf21Cooperative = CreateOptions(GameCompatibility.Mbf21, true, 0);

        Assert.IsFalse(BoomThingSpawnFilter.IsAllowed(mbfDeathmatch, ThingFlags.NotDeathmatch));
        Assert.IsFalse(BoomThingSpawnFilter.IsAllowed(mbf21Cooperative, ThingFlags.NotCooperative));
    }

    [TestMethod]
    public void ThingAllocationHonorsBoomNotDeathmatchFlag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        world.Options.NetGame = true;
        world.Options.Deathmatch = 1;

        var before = world.TotalItems;
        world.ThingAllocation.SpawnMapThing(CreateHealthBonus(world, ThingFlags.Normal | ThingFlags.NotDeathmatch));
        Assert.AreEqual(before, world.TotalItems);

        world.ThingAllocation.SpawnMapThing(CreateHealthBonus(world, ThingFlags.Normal | ThingFlags.NotCooperative));
        Assert.AreEqual(before + 1, world.TotalItems);
    }

    [TestMethod]
    public void ThingAllocationHonorsBoomNotCooperativeFlag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        world.Options.NetGame = true;
        world.Options.Deathmatch = 0;

        var before = world.TotalItems;
        world.ThingAllocation.SpawnMapThing(CreateHealthBonus(world, ThingFlags.Normal | ThingFlags.NotCooperative));
        Assert.AreEqual(before, world.TotalItems);

        world.ThingAllocation.SpawnMapThing(CreateHealthBonus(world, ThingFlags.Normal | ThingFlags.NotDeathmatch));
        Assert.AreEqual(before + 1, world.TotalItems);
    }

    [TestMethod]
    public void ThingAllocationVanillaIgnoresBoomSpawnFlags()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        world.Options.NetGame = true;
        world.Options.Deathmatch = 1;

        var before = world.TotalItems;
        world.ThingAllocation.SpawnMapThing(CreateHealthBonus(world, ThingFlags.Normal | ThingFlags.NotDeathmatch));

        Assert.AreEqual(before + 1, world.TotalItems);
    }

    private static GameOptions CreateOptions(GameCompatibility compatibility, bool netGame, int deathmatch)
    {
        return new GameOptions
        {
            Compatibility = compatibility,
            NetGame = netGame,
            Deathmatch = deathmatch
        };
    }

    private static MapThing CreateHealthBonus(World world, ThingFlags flags)
    {
        var player = world.ConsolePlayer.Mobj;
        return new MapThing(player.X, player.Y, Angle.Ang0, HealthBonusThingType, flags);
    }
}
