using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Sectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomGeneralizedSectorDamageTest
{
    [DataTestMethod]
    [DataRow(0x20, 5)]
    [DataRow(0x40, 10)]
    [DataRow(0x60, 20)]
    public void GeneralizedDamageBitsDamagePlayerEveryThirtyTwoTics(int damageBits, int expectedDamage)
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer;
        var sector = player.Mobj.Subsector.Sector;

        PreparePlayer(world, player, sector);
        sector.Special = (SectorSpecial)damageBits;

        world.PlayerBehavior.PlayerThink(player);

        Assert.AreEqual(100 - expectedDamage, player.Health);
        Assert.AreEqual(100 - expectedDamage, player.Mobj.Health);
    }

    [TestMethod]
    public void GeneralizedDamageWaitsForThirtyTwoTicBoundary()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer;
        var sector = player.Mobj.Subsector.Sector;

        PreparePlayer(world, player, sector);
        sector.Special = (SectorSpecial)0x40;
        world.LevelTime = 1;

        world.PlayerBehavior.PlayerThink(player);

        Assert.AreEqual(100, player.Health);
        Assert.AreEqual(100, player.Mobj.Health);

        world.LevelTime = 32;
        world.PlayerBehavior.PlayerThink(player);

        Assert.AreEqual(90, player.Health);
        Assert.AreEqual(90, player.Mobj.Health);
    }

    [DataTestMethod]
    [DataRow(0x20)]
    [DataRow(0x40)]
    public void RadiationSuitBlocksFiveAndTenDamage(int damageBits)
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer;
        var sector = player.Mobj.Subsector.Sector;

        PreparePlayer(world, player, sector);
        sector.Special = (SectorSpecial)damageBits;
        player.Powers[(int)PowerType.IronFeet] = 100;

        world.PlayerBehavior.PlayerThink(player);

        Assert.AreEqual(100, player.Health);
        Assert.AreEqual(100, player.Mobj.Health);
    }

    [TestMethod]
    public void LowBitFourInGeneralizedSectorDoesNotApplyLegacyTwentyDamage()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer;
        var sector = player.Mobj.Subsector.Sector;

        PreparePlayer(world, player, sector);
        sector.Special = (SectorSpecial)(4 | 0x20 | BoomSectorSpecialDecoder.PusherMask);
        world.Specials.SpawnSpecials();

        world.PlayerBehavior.PlayerThink(player);

        Assert.IsNotNull(sector.LightingData as StrobeFlash);
        Assert.AreEqual(95, player.Health);
        Assert.AreEqual(95, player.Mobj.Health);
        Assert.AreEqual(4 | 0x20 | BoomSectorSpecialDecoder.PusherMask, (int)sector.Special);
    }

    [TestMethod]
    public void GeneralizedDamageRequiresPlayerToTouchSectorFloor()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer;
        var sector = player.Mobj.Subsector.Sector;

        PreparePlayer(world, player, sector);
        sector.Special = (SectorSpecial)0x60;
        player.Mobj.Z = sector.FloorHeight + Fixed.One;

        world.PlayerBehavior.PlayerThink(player);

        Assert.AreEqual(100, player.Health);
        Assert.AreEqual(100, player.Mobj.Health);
    }

    private static void PreparePlayer(World world, Player player, Sector sector)
    {
        world.LevelTime = 0;
        player.Health = 100;
        player.Mobj.Health = 100;
        player.Mobj.Z = sector.FloorHeight;
        player.Powers[(int)PowerType.IronFeet] = 0;
        sector.Special = 0;
        sector.LightingData = null;
    }
}
