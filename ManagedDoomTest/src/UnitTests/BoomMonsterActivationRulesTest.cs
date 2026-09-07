using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomMonsterActivationRulesTest
{
    [TestMethod]
    public void NullActivatorIsNeverTreatedAsMonster()
    {
        Assert.IsFalse(BoomMonsterActivationRules.CanActivate(null, allowsMonsters: true));
        Assert.IsFalse(BoomMonsterActivationRules.CanActivatePlayerOnly(null));
        Assert.IsFalse(BoomMonsterActivationRules.CanActivateTeleport(
            null,
            playersAllowed: true,
            monstersAllowed: true));
    }

    [TestMethod]
    public void GeneralizedRuleAllowsPlayerRegardlessOfMonsterBit()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);

        Assert.IsTrue(BoomMonsterActivationRules.CanActivate(
            world.ConsolePlayer.Mobj,
            allowsMonsters: false));
    }

    [TestMethod]
    public void GeneralizedRuleRequiresMonsterBitForNonPlayer()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var nonPlayer = new Mobj(world);

        Assert.IsFalse(BoomMonsterActivationRules.CanActivate(nonPlayer, allowsMonsters: false));
        Assert.IsTrue(BoomMonsterActivationRules.CanActivate(nonPlayer, allowsMonsters: true));
    }

    [TestMethod]
    public void SecretDoorRejectsNonPlayerEvenWhenMonsterEnabled()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var nonPlayer = new Mobj(world);
        var line = world.Map.Lines[0];

        line.Flags |= LineFlags.Secret;

        Assert.IsFalse(BoomMonsterActivationRules.CanActivateDoor(
            line,
            nonPlayer,
            allowsMonsters: true));
    }

    [TestMethod]
    public void NonSecretDoorAllowsNonPlayerWhenMonsterEnabled()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var nonPlayer = new Mobj(world);
        var line = world.Map.Lines[0];

        line.Flags &= ~LineFlags.Secret;

        Assert.IsTrue(BoomMonsterActivationRules.CanActivateDoor(
            line,
            nonPlayer,
            allowsMonsters: true));
        Assert.IsFalse(BoomMonsterActivationRules.CanActivateDoor(
            line,
            nonPlayer,
            allowsMonsters: false));
    }

    [TestMethod]
    public void TeleportRuleSeparatesPlayerAndMonsterPermissions()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var playerThing = world.ConsolePlayer.Mobj;
        var nonPlayer = new Mobj(world);

        Assert.IsTrue(BoomMonsterActivationRules.CanActivateTeleport(
            playerThing,
            playersAllowed: true,
            monstersAllowed: false));
        Assert.IsFalse(BoomMonsterActivationRules.CanActivateTeleport(
            playerThing,
            playersAllowed: false,
            monstersAllowed: true));

        Assert.IsFalse(BoomMonsterActivationRules.CanActivateTeleport(
            nonPlayer,
            playersAllowed: true,
            monstersAllowed: false));
        Assert.IsTrue(BoomMonsterActivationRules.CanActivateTeleport(
            nonPlayer,
            playersAllowed: false,
            monstersAllowed: true));
    }

    [TestMethod]
    public void GeneralizedMonsterEnabledPushIsAcceptedByRuntimeDispatcher()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var nonPlayer = new Mobj(world);
        var line = world.Map.Lines.First(item => item.BackSector != null);

        line.Flags &= ~LineFlags.Secret;
        line.Special = (LineSpecial)(BoomDoorTranslator.Min | 0x0080 | (int)BoomTriggerType.PushRepeat);

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, nonPlayer, out var result));
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void GeneralizedMonsterDisabledPushIsRejectedByRuntimeDispatcher()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var nonPlayer = new Mobj(world);
        var line = world.Map.Lines.First(item => item.BackSector != null);

        line.Flags &= ~LineFlags.Secret;
        line.Special = (LineSpecial)(BoomDoorTranslator.Min | (int)BoomTriggerType.PushRepeat);

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, nonPlayer, out var result));
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void NullActivatorCannotActivateGeneralizedPushAtRuntime()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = world.Map.Lines[0];

        line.Special = (LineSpecial)(BoomDoorTranslator.Min | 0x0080 | (int)BoomTriggerType.PushRepeat);

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, null, out var result));
        Assert.IsFalse(result);
    }
}
