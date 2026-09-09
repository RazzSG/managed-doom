using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfMonsterInfightingTest
{
    [TestMethod]
    public void DisabledOptionBlocksRetaliationBetweenHostileMonsters()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var victim = SpawnTroop(world, 64);
        var attacker = SpawnTroop(world, 128);

        Assert.IsFalse(MbfMonsterInfighting.CanRetaliate(
            GameCompatibility.Mbf,
            false,
            victim,
            attacker));
    }

    [TestMethod]
    public void EnabledOptionAllowsRetaliationBetweenHostileMonsters()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var victim = SpawnTroop(world, 64);
        var attacker = SpawnTroop(world, 128);

        Assert.IsTrue(MbfMonsterInfighting.CanRetaliate(
            GameCompatibility.Mbf,
            true,
            victim,
            attacker));
    }

    [TestMethod]
    public void DisabledOptionStillAllowsCombatAcrossFriendBoundary()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var hostile = SpawnTroop(world, 64);
        var friend = SpawnTroop(world, 128);
        friend.Flags |= MobjFlags.Friend;

        Assert.IsTrue(MbfMonsterInfighting.CanRetaliate(
            GameCompatibility.Mbf,
            false,
            hostile,
            friend));
        Assert.IsTrue(MbfMonsterInfighting.CanRetaliate(
            GameCompatibility.Mbf,
            false,
            friend,
            hostile));
    }

    [TestMethod]
    public void BoomIgnoresMbfInfightingOption()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);
        var victim = SpawnTroop(world, 64);
        var attacker = SpawnTroop(world, 128);

        Assert.IsTrue(MbfMonsterInfighting.CanRetaliate(
            GameCompatibility.Boom,
            false,
            victim,
            attacker));
    }

    [TestMethod]
    public void DamageMobjKeepsCurrentEnemyWhenMbfInfightingIsDisabled()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        world.Options.MbfOptions.MonsterInfighting = false;

        var player = world.ConsolePlayer.Mobj;
        var victim = SpawnTroop(world, 64);
        var attacker = SpawnTroop(world, 128);
        victim.Target = player;
        victim.Threshold = 0;

        world.ThingInteraction.DamageMobj(victim, attacker, attacker, 1);

        Assert.AreSame(player, victim.Target);
        Assert.IsNull(victim.LastEnemy);
    }

    [TestMethod]
    public void DamageMobjRetaliatesNormallyWithDefaultMbfInfighting()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);

        var player = world.ConsolePlayer.Mobj;
        var victim = SpawnTroop(world, 64);
        var attacker = SpawnTroop(world, 128);
        victim.Target = player;
        victim.Threshold = 0;

        world.ThingInteraction.DamageMobj(victim, attacker, attacker, 1);

        Assert.AreSame(attacker, victim.Target);
        Assert.AreSame(player, victim.LastEnemy);
    }

    [TestMethod]
    public void DamageMobjStillRetaliatesAgainstFriendlyEnemyWhenInfightingIsDisabled()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        world.Options.MbfOptions.MonsterInfighting = false;

        var victim = SpawnTroop(world, 64);
        var attacker = SpawnTroop(world, 128);
        attacker.Flags |= MobjFlags.Friend;
        victim.Target = world.ConsolePlayer.Mobj;
        victim.Threshold = 0;

        world.ThingInteraction.DamageMobj(victim, attacker, attacker, 1);

        Assert.AreSame(attacker, victim.Target);
    }

    private static World CreateWorld(GameContent content, GameCompatibility compatibility)
    {
        var options = new GameOptions { Compatibility = compatibility };
        options.Players[0].Reborn();
        return new World(content, options, null);
    }

    private static Mobj SpawnTroop(World world, int xOffset)
    {
        var player = world.ConsolePlayer.Mobj;
        return world.ThingAllocation.SpawnMobj(
            player.X + Fixed.FromInt(xOffset),
            player.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);
    }
}
