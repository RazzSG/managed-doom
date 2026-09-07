using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Sectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomCombinedSectorLightingTest
{
    [TestMethod]
    public void CombinedStrobeUsesLowFiveBitsAndPreservesGeneralizedFlags()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var sector = FindPlainSector(world);

        ResetSectors(world);
        var flags = BoomSectorSpecialDecoder.DamageMask |
            BoomSectorSpecialDecoder.SecretMask |
            BoomSectorSpecialDecoder.FrictionMask |
            BoomSectorSpecialDecoder.PusherMask;

        sector.Special = (SectorSpecial)(2 | flags);
        world.Specials.SpawnSpecials();

        Assert.IsNotNull(sector.LightingData as StrobeFlash);
        Assert.AreEqual(flags, (int)sector.Special);
    }

    [TestMethod]
    public void CombinedLightFlashPreservesGeneralizedFlags()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var sector = FindPlainSector(world);

        ResetSectors(world);
        var flags = BoomSectorSpecialDecoder.SecretMask | BoomSectorSpecialDecoder.PusherMask;
        sector.Special = (SectorSpecial)(1 | flags);

        world.Specials.SpawnSpecials();

        Assert.IsNotNull(sector.LightingData as LightFlash);
        Assert.AreEqual(flags, (int)sector.Special);
    }

    [TestMethod]
    public void CombinedGlowAndFireFlickerPreserveGeneralizedFlags()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var sectors = world.Map.Sectors.Where(s => s.LightingData == null).Take(2).ToArray();

        Assert.AreEqual(2, sectors.Length);
        ResetSectors(world);

        const int flags = BoomSectorSpecialDecoder.FrictionMask | BoomSectorSpecialDecoder.PusherMask;
        sectors[0].Special = (SectorSpecial)(8 | flags);
        sectors[1].Special = (SectorSpecial)(17 | flags);

        world.Specials.SpawnSpecials();

        Assert.IsNotNull(sectors[0].LightingData as GlowingLight);
        Assert.IsNotNull(sectors[1].LightingData as FireFlicker);
        Assert.AreEqual(flags, (int)sectors[0].Special);
        Assert.AreEqual(flags, (int)sectors[1].Special);
    }

    [TestMethod]
    public void CombinedSpecialFourPreservesGeneralizedFlagsAndLegacyMarker()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var sector = FindPlainSector(world);

        ResetSectors(world);
        var flags = BoomSectorSpecialDecoder.SecretMask |
            BoomSectorSpecialDecoder.FrictionMask |
            BoomSectorSpecialDecoder.PusherMask;
        sector.Special = (SectorSpecial)(4 | flags);

        world.Specials.SpawnSpecials();

        Assert.IsNotNull(sector.LightingData as StrobeFlash);
        Assert.AreEqual(4 | flags, (int)sector.Special);
    }

    [TestMethod]
    public void LegacySpecialFourKeepsItsDamageMarkerAfterLightingSpawn()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var sector = FindPlainSector(world);

        ResetSectors(world);
        sector.Special = (SectorSpecial)4;

        world.Specials.SpawnSpecials();

        Assert.IsNotNull(sector.LightingData as StrobeFlash);
        Assert.AreEqual(4, (int)sector.Special);
    }

    [TestMethod]
    public void VanillaDoesNotDecodeGeneralizedLightingBits()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var sector = FindPlainSector(world);
        var raw = 2 | BoomSectorSpecialDecoder.PusherMask;

        ResetSectors(world);
        sector.Special = (SectorSpecial)raw;

        world.Specials.SpawnSpecials();

        Assert.IsNull(sector.LightingData);
        Assert.AreEqual(raw, (int)sector.Special);
    }

    [TestMethod]
    public void ReservedGeneralizedBitsDoNotThrowDuringPlayerThink()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer;
        var sector = player.Mobj.Subsector.Sector;

        sector.Special = (SectorSpecial)BoomSectorSpecialDecoder.ReservedSoundMask;
        player.Mobj.Z = sector.FloorHeight;

        world.PlayerBehavior.PlayerThink(player);

        Assert.AreEqual(BoomSectorSpecialDecoder.ReservedSoundMask, (int)sector.Special);
    }

    private static Sector FindPlainSector(World world)
    {
        var sector = world.Map.Sectors.FirstOrDefault(s => s.LightingData == null);
        Assert.IsNotNull(sector);
        return sector;
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
