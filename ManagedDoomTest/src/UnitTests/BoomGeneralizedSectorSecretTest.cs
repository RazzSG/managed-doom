using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Sectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomGeneralizedSectorSecretTest
{
    [TestMethod]
    public void GeneralizedSecretIsCountedAndRevealedOnlyOnce()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer;
        var sector = player.Mobj.Subsector.Sector;

        ResetSectors(world);
        world.TotalSecrets = 0;
        player.SecretCount = 0;
        player.Message = null;
        sector.Special = (SectorSpecial)BoomSectorSpecialDecoder.SecretMask;

        world.Specials.SpawnSpecials();

        Assert.AreEqual(1, world.TotalSecrets);

        PutPlayerOnFloor(player, sector);
        world.PlayerBehavior.PlayerThink(player);

        Assert.AreEqual(1, player.SecretCount);
        Assert.AreEqual("A secret is revealed!", player.Message);
        Assert.AreEqual(0, (int)sector.Special);

        world.PlayerBehavior.PlayerThink(player);

        Assert.AreEqual(1, player.SecretCount);
    }

    [TestMethod]
    public void RevealingGeneralizedSecretPreservesOtherBoomFlags()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer;
        var sector = player.Mobj.Subsector.Sector;
        var remaining = 0x40 |
            BoomSectorSpecialDecoder.FrictionMask |
            BoomSectorSpecialDecoder.PusherMask;

        ResetSectors(world);
        world.TotalSecrets = 0;
        player.SecretCount = 0;
        player.Health = 100;
        player.Mobj.Health = 100;
        player.Powers[(int)PowerType.IronFeet] = 0;
        sector.Special = (SectorSpecial)(remaining | BoomSectorSpecialDecoder.SecretMask);
        world.Specials.SpawnSpecials();

        PutPlayerOnFloor(player, sector);
        world.LevelTime = 1;
        world.PlayerBehavior.PlayerThink(player);

        Assert.AreEqual(1, player.SecretCount);
        Assert.AreEqual(remaining, (int)sector.Special);
        Assert.AreEqual(100, player.Health);

        world.LevelTime = 32;
        world.PlayerBehavior.PlayerThink(player);

        Assert.AreEqual(90, player.Health);
        Assert.AreEqual(90, player.Mobj.Health);
        Assert.AreEqual(1, player.SecretCount);
        Assert.AreEqual(remaining, (int)sector.Special);
    }

    [TestMethod]
    public void CombinedLightingAndSecretCountsOnceAndKeepsLightingThinker()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer;
        var sector = player.Mobj.Subsector.Sector;

        ResetSectors(world);
        world.TotalSecrets = 0;
        player.SecretCount = 0;
        sector.Special = (SectorSpecial)(2 | BoomSectorSpecialDecoder.SecretMask);

        world.Specials.SpawnSpecials();

        Assert.AreEqual(1, world.TotalSecrets);
        Assert.IsNotNull(sector.LightingData as StrobeFlash);
        Assert.AreEqual(BoomSectorSpecialDecoder.SecretMask, (int)sector.Special);

        PutPlayerOnFloor(player, sector);
        world.PlayerBehavior.PlayerThink(player);

        Assert.AreEqual(1, player.SecretCount);
        Assert.AreEqual(0, (int)sector.Special);
        Assert.IsNotNull(sector.LightingData as StrobeFlash);
    }

    [TestMethod]
    public void VanillaDoesNotDecodeBoomSecretBit()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var sector = world.ConsolePlayer.Mobj.Subsector.Sector;

        ResetSectors(world);
        world.TotalSecrets = 0;
        sector.Special = (SectorSpecial)BoomSectorSpecialDecoder.SecretMask;

        world.Specials.SpawnSpecials();

        Assert.AreEqual(0, world.TotalSecrets);
        Assert.AreEqual(BoomSectorSpecialDecoder.SecretMask, (int)sector.Special);
    }

    [TestMethod]
    public void LegacySecretStillWorksInBoomCompatibility()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer;
        var sector = player.Mobj.Subsector.Sector;

        ResetSectors(world);
        world.TotalSecrets = 0;
        player.SecretCount = 0;
        sector.Special = (SectorSpecial)9;

        world.Specials.SpawnSpecials();

        Assert.AreEqual(1, world.TotalSecrets);

        PutPlayerOnFloor(player, sector);
        world.PlayerBehavior.PlayerThink(player);

        Assert.AreEqual(1, player.SecretCount);
        Assert.AreEqual(0, (int)sector.Special);
    }

    private static void PutPlayerOnFloor(Player player, Sector sector)
    {
        player.Mobj.Z = sector.FloorHeight;
    }

    private static void ResetSectors(World world)
    {
        foreach (var sector in world.Map.Sectors)
        {
            sector.Special = 0;
            sector.LightingData = null;
        }
    }
}
