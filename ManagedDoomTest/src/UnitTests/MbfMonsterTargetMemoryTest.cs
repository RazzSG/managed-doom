using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfMonsterTargetMemoryTest
{
    [TestMethod]
    public void MbfHostileMonstersRemainValidInfightingTargets()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);

        var first = CreateShootableActor(world);
        var second = CreateShootableActor(world);

        Assert.IsFalse(MbfFriendTargeting.AreOnSameSide(GameCompatibility.Mbf, first, second));
        Assert.IsTrue(MbfFriendTargeting.CanAcquireTarget(GameCompatibility.Mbf, first, second));
    }

    [TestMethod]
    public void MbfRemembersCurrentEnemyBeforeRetaliating()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);

        var actor = CreateShootableActor(world);
        var previous = world.ConsolePlayer.Mobj;
        var retaliation = CreateShootableActor(world);
        actor.Target = previous;

        MbfMonsterTargetMemory.RememberCurrentEnemy(
            GameCompatibility.Mbf,
            actor,
            retaliation);

        Assert.AreSame(previous, actor.LastEnemy);
    }

    [TestMethod]
    public void BoomDoesNotPopulateMbfPreviousEnemyMemory()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);

        var actor = CreateShootableActor(world);
        actor.Target = world.ConsolePlayer.Mobj;
        var retaliation = CreateShootableActor(world);

        MbfMonsterTargetMemory.RememberCurrentEnemy(
            GameCompatibility.Boom,
            actor,
            retaliation);

        Assert.IsNull(actor.LastEnemy);
    }

    [TestMethod]
    public void MbfRestoresRememberedEnemyAndConsumesMemorySlot()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);

        var actor = CreateShootableActor(world);
        var previous = world.ConsolePlayer.Mobj;
        actor.Target = null;
        actor.LastEnemy = previous;

        Assert.IsTrue(MbfMonsterTargetMemory.TryRestorePreviousEnemy(GameCompatibility.Mbf, actor));
        Assert.AreSame(previous, actor.Target);
        Assert.IsNull(actor.LastEnemy);
    }

    [TestMethod]
    public void MbfRejectsFriendlyActorAsRememberedEnemy()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);

        var actor = CreateShootableActor(world);
        actor.Flags |= MobjFlags.Friend;
        actor.Target = null;
        actor.LastEnemy = world.ConsolePlayer.Mobj;

        Assert.IsFalse(MbfMonsterTargetMemory.TryRestorePreviousEnemy(GameCompatibility.Mbf, actor));
        Assert.IsNull(actor.Target);
        Assert.IsNull(actor.LastEnemy);
    }

    [TestMethod]
    public void MbfChaseRestoresPreviousEnemyBeforeNormalTargetSearch()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);

        var player = world.ConsolePlayer.Mobj;
        var actor = world.ThingAllocation.SpawnMobj(
            player.X + Fixed.FromInt(64),
            player.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);
        var remembered = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(512),
            actor.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);
        var expired = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(256),
            actor.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);

        expired.Flags &= ~MobjFlags.Shootable;
        actor.Target = expired;
        actor.LastEnemy = remembered;
        actor.MoveDir = Direction.None;

        world.MonsterBehavior.Chase(actor);

        Assert.AreSame(remembered, actor.Target);
        Assert.IsNull(actor.LastEnemy);
    }

    [TestMethod]
    public void MbfDamageRetaliationStoresPreviousEnemy()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);

        var victim = world.ThingAllocation.SpawnMobj(
            world.ConsolePlayer.Mobj.X + Fixed.FromInt(64),
            world.ConsolePlayer.Mobj.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);
        var attacker = world.ThingAllocation.SpawnMobj(
            victim.X + Fixed.FromInt(64),
            victim.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);

        var previous = world.ConsolePlayer.Mobj;
        victim.Target = previous;
        victim.Threshold = 0;

        world.ThingInteraction.DamageMobj(victim, attacker, attacker, 1);

        Assert.AreSame(attacker, victim.Target);
        Assert.AreSame(previous, victim.LastEnemy);
    }

    [TestMethod]
    public void MbfFriendlyDamageDoesNotTurnFriendsAgainstEachOther()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);

        var victim = world.ThingAllocation.SpawnMobj(
            world.ConsolePlayer.Mobj.X + Fixed.FromInt(64),
            world.ConsolePlayer.Mobj.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);
        var attacker = world.ThingAllocation.SpawnMobj(
            victim.X + Fixed.FromInt(64),
            victim.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);

        victim.Flags |= MobjFlags.Friend;
        attacker.Flags |= MobjFlags.Friend;
        victim.Target = world.ConsolePlayer.Mobj;
        victim.Threshold = 0;

        world.ThingInteraction.DamageMobj(victim, attacker, attacker, 1);

        Assert.AreSame(world.ConsolePlayer.Mobj, victim.Target);
        Assert.IsNull(victim.LastEnemy);
    }

    private static World CreateWorld(GameContent content, GameCompatibility compatibility)
    {
        var options = new GameOptions { Compatibility = compatibility };
        options.Players[0].Reborn();
        return new World(content, options, null);
    }

    private static Mobj CreateShootableActor(World world) =>
        new Mobj(world)
        {
            Flags = MobjFlags.Shootable,
            Health = 100
        };
}
