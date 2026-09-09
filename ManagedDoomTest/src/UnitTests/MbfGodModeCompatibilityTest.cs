using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Gameplay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfGodModeCompatibilityTest
{
    [TestMethod]
    public void CompatibilityGatePreservesVanillaAndBoomAndAddsMbfToggle()
    {
        Assert.IsFalse(MbfGodModeCompatibility.UsesAbsoluteGodMode(
            GameCompatibility.Vanilla, compGod: false));
        Assert.IsTrue(MbfGodModeCompatibility.UsesAbsoluteGodMode(
            GameCompatibility.Boom, compGod: true));

        Assert.IsTrue(MbfGodModeCompatibility.UsesAbsoluteGodMode(
            GameCompatibility.Mbf, compGod: false));
        Assert.IsFalse(MbfGodModeCompatibility.UsesAbsoluteGodMode(
            GameCompatibility.Mbf, compGod: true));
        Assert.IsTrue(MbfGodModeCompatibility.UsesAbsoluteGodMode(
            GameCompatibility.Mbf21, compGod: false));
        Assert.IsFalse(MbfGodModeCompatibility.UsesAbsoluteGodMode(
            GameCompatibility.Mbf21, compGod: true));

        Assert.IsTrue(MbfGodModeCompatibility.ClearsGodModeInExitDamageSector(
            GameCompatibility.Vanilla, compGod: false));
        Assert.IsFalse(MbfGodModeCompatibility.ClearsGodModeInExitDamageSector(
            GameCompatibility.Boom, compGod: true));
        Assert.IsFalse(MbfGodModeCompatibility.ClearsGodModeInExitDamageSector(
            GameCompatibility.Mbf, compGod: false));
        Assert.IsTrue(MbfGodModeCompatibility.ClearsGodModeInExitDamageSector(
            GameCompatibility.Mbf, compGod: true));
    }

    [TestMethod]
    public void CompGodRestoresThousandDamageBypassOnlyForMbfFamily()
    {
        Assert.IsTrue(MbfGodModeCompatibility.ShouldIgnorePlayerDamage(
            GameCompatibility.Mbf, compGod: false, damage: 1000, godMode: true, invulnerable: false));
        Assert.IsFalse(MbfGodModeCompatibility.ShouldIgnorePlayerDamage(
            GameCompatibility.Mbf, compGod: true, damage: 1000, godMode: true, invulnerable: false));

        Assert.IsTrue(MbfGodModeCompatibility.ShouldIgnorePlayerDamage(
            GameCompatibility.Boom, compGod: true, damage: 1000, godMode: true, invulnerable: false));
        Assert.IsFalse(MbfGodModeCompatibility.ShouldIgnorePlayerDamage(
            GameCompatibility.Vanilla, compGod: false, damage: 1000, godMode: true, invulnerable: false));

        // comp_god only changes god mode. Invulnerability keeps Doom's 1000+ threshold.
        Assert.IsFalse(MbfGodModeCompatibility.ShouldIgnorePlayerDamage(
            GameCompatibility.Mbf, compGod: false, damage: 1000, godMode: false, invulnerable: true));
    }

    [TestMethod]
    public void DamageMobjConsumesRuntimeCompGod()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var fixedWorld = CreateWorld(content, compGod: false);
        var fixedPlayer = fixedWorld.ConsolePlayer;
        PreparePlayer(fixedPlayer);
        fixedWorld.ThingInteraction.DamageMobj(fixedPlayer.Mobj, null, null, 1000);
        Assert.AreEqual(2000, fixedPlayer.Health);
        Assert.AreEqual(2000, fixedPlayer.Mobj.Health);

        var compatibleWorld = CreateWorld(content, compGod: true);
        var compatiblePlayer = compatibleWorld.ConsolePlayer;
        PreparePlayer(compatiblePlayer);
        compatibleWorld.ThingInteraction.DamageMobj(compatiblePlayer.Mobj, null, null, 1000);
        Assert.AreEqual(1000, compatiblePlayer.Health);
        Assert.AreEqual(1000, compatiblePlayer.Mobj.Health);
    }

    [TestMethod]
    public void ExitDamageSectorConsumesRuntimeCompGod()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var fixedWorld = CreateWorld(content, compGod: false);
        var fixedPlayer = fixedWorld.ConsolePlayer;
        fixedPlayer.Cheats |= CheatFlags.GodMode;
        fixedPlayer.Mobj.Subsector.Sector.Special = (SectorSpecial)11;
        fixedPlayer.Mobj.Z = fixedPlayer.Mobj.Subsector.Sector.FloorHeight;
        fixedWorld.PlayerBehavior.PlayerThink(fixedPlayer);
        Assert.IsTrue((fixedPlayer.Cheats & CheatFlags.GodMode) != 0);

        var compatibleWorld = CreateWorld(content, compGod: true);
        var compatiblePlayer = compatibleWorld.ConsolePlayer;
        compatiblePlayer.Cheats |= CheatFlags.GodMode;
        compatiblePlayer.Mobj.Subsector.Sector.Special = (SectorSpecial)11;
        compatiblePlayer.Mobj.Z = compatiblePlayer.Mobj.Subsector.Sector.FloorHeight;
        compatibleWorld.PlayerBehavior.PlayerThink(compatiblePlayer);
        Assert.IsFalse((compatiblePlayer.Cheats & CheatFlags.GodMode) != 0);
    }

    private static World CreateWorld(GameContent content, bool compGod)
    {
        var options = new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = GameCompatibility.Mbf
        };
        options.MbfOptions.CompGod = compGod;
        return new World(content, options, null);
    }

    private static void PreparePlayer(Player player)
    {
        player.Health = 2000;
        player.Mobj.Health = 2000;
        player.Cheats |= CheatFlags.GodMode;
        player.Mobj.Subsector.Sector.Special = 0;
    }
}
