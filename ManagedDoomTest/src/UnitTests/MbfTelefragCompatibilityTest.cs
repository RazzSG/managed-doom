using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfTelefragCompatibilityTest
{
    [TestMethod]
    public void MbfDefaultKeepsCorrectedBossTeleportRule()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var monster = new Mobj(world);

        Assert.IsTrue(MbfTelefragCompatibility.CanTelefragAtDestination(
            monster, bossTeleport: true, mapNumber: 1, GameCompatibility.Mbf, compTelefrag: false));
        Assert.IsFalse(MbfTelefragCompatibility.CanTelefragAtDestination(
            monster, bossTeleport: false, mapNumber: 30, GameCompatibility.Mbf, compTelefrag: false));
    }

    [TestMethod]
    public void CompTelefragRestoresClassicMap30RuleForMonsters()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var monster = new Mobj(world);

        Assert.IsFalse(MbfTelefragCompatibility.CanTelefragAtDestination(
            monster, bossTeleport: true, mapNumber: 1, GameCompatibility.Mbf, compTelefrag: true));
        Assert.IsTrue(MbfTelefragCompatibility.CanTelefragAtDestination(
            monster, bossTeleport: false, mapNumber: 30, GameCompatibility.Mbf, compTelefrag: true));
        Assert.IsTrue(MbfTelefragCompatibility.CanTelefragAtDestination(
            monster, bossTeleport: true, mapNumber: 30, GameCompatibility.Mbf, compTelefrag: true));
    }

    [TestMethod]
    public void PlayerTeleportAlwaysTelefragsRegardlessOfCompTelefrag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var player = world.ConsolePlayer.Mobj;

        Assert.IsTrue(MbfTelefragCompatibility.CanTelefragAtDestination(
            player, bossTeleport: false, mapNumber: 1, GameCompatibility.Mbf, compTelefrag: false));
        Assert.IsTrue(MbfTelefragCompatibility.CanTelefragAtDestination(
            player, bossTeleport: false, mapNumber: 1, GameCompatibility.Mbf, compTelefrag: true));
    }

    [TestMethod]
    public void Mbf21InheritsCompTelefragSelection()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf21 }, null);
        var monster = new Mobj(world);

        Assert.IsFalse(MbfTelefragCompatibility.CanTelefragAtDestination(
            monster, bossTeleport: true, mapNumber: 1, GameCompatibility.Mbf21, compTelefrag: true));
        Assert.IsTrue(MbfTelefragCompatibility.CanTelefragAtDestination(
            monster, bossTeleport: false, mapNumber: 30, GameCompatibility.Mbf21, compTelefrag: true));
    }

    [TestMethod]
    public void BelowMbfCompFlagDoesNotChangeExistingBoomSelection()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var monster = new Mobj(world);

        Assert.IsTrue(MbfTelefragCompatibility.CanTelefragAtDestination(
            monster, bossTeleport: true, mapNumber: 1, GameCompatibility.Boom, compTelefrag: true));
        Assert.IsFalse(MbfTelefragCompatibility.CanTelefragAtDestination(
            monster, bossTeleport: false, mapNumber: 30, GameCompatibility.Boom, compTelefrag: true));
    }

    [TestMethod]
    public void TeleportMoveConsumesRuntimeCompTelefrag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions
        {
            Compatibility = GameCompatibility.Mbf,
            Map = 1
        };
        options.MbfOptions.CompTelefrag = true;

        var world = new World(content, options, null);
        var player = world.ConsolePlayer.Mobj;
        var monster = world.ThingAllocation.SpawnMobj(
            player.X, player.Y, Mobj.OnFloorZ, MobjType.Possessed);

        Assert.IsFalse(world.ThingMovement.TeleportMove(
            monster, monster.X, monster.Y, bossTeleport: true));
    }
}
