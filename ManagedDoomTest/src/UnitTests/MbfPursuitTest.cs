using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfPursuitTest
{
    [TestMethod]
    public void FeatureRequiresMbf()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = SpawnActor(world, MobjType.Troop);
        var target = world.ConsolePlayer.Mobj;

        Assert.IsFalse(MbfPursuit.ShouldKeepCurrentTarget(
            GameCompatibility.Boom, true, false, true, actor, target, false));
        Assert.IsTrue(MbfPursuit.ShouldKeepCurrentTarget(
            GameCompatibility.Mbf, true, false, true, actor, target, false));
    }

    [TestMethod]
    public void CompPursuitKeepsLiveSinglePlayerTargetWithoutSight()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = SpawnActor(world, MobjType.Troop);
        var target = world.ConsolePlayer.Mobj;

        Assert.IsTrue(MbfPursuit.ShouldKeepCurrentTarget(
            GameCompatibility.Mbf, true, false, true, actor, target, false));
        Assert.IsFalse(MbfPursuit.ShouldKeepCurrentTarget(
            GameCompatibility.Mbf, false, false, true, actor, target, false));
    }

    [TestMethod]
    public void CompPursuitShortcutDoesNotApplyInNetGame()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = SpawnActor(world, MobjType.Troop);
        var target = world.ConsolePlayer.Mobj;

        Assert.IsFalse(MbfPursuit.ShouldKeepCurrentTarget(
            GameCompatibility.Mbf, true, true, true, actor, target, false));
    }

    [TestMethod]
    public void VisibleOpposingTargetRemainsValidWhenSmartPursuitIsEnabled()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = SpawnActor(world, MobjType.Troop);
        var target = world.ConsolePlayer.Mobj;

        Assert.IsTrue(MbfPursuit.ShouldKeepCurrentTarget(
            GameCompatibility.Mbf, false, false, true, actor, target, true));
    }

    [TestMethod]
    public void SameSideHostileInfightingDependsOnMonsterInfightingOption()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = SpawnActor(world, MobjType.Troop);
        var target = SpawnActor(world, MobjType.Sergeant);

        Assert.IsTrue(MbfPursuit.ShouldKeepCurrentTarget(
            GameCompatibility.Mbf, false, false, true, actor, target, true));
        Assert.IsFalse(MbfPursuit.ShouldKeepCurrentTarget(
            GameCompatibility.Mbf, false, false, false, actor, target, true));
    }

    [TestMethod]
    public void PlayerAcquireThresholdMatchesMbfPursuitMode()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = SpawnActor(world, MobjType.Troop);

        MbfPursuit.ApplyPlayerAcquireThreshold(GameCompatibility.Mbf, false, actor);
        Assert.AreEqual(MbfPursuit.PlayerAcquireThreshold, actor.Threshold);

        actor.Threshold = 0;
        MbfPursuit.ApplyPlayerAcquireThreshold(GameCompatibility.Mbf, true, actor);
        Assert.AreEqual(0, actor.Threshold);

        MbfPursuit.ApplyPlayerAcquireThreshold(GameCompatibility.Boom, false, actor);
        Assert.AreEqual(0, actor.Threshold);
    }

    [TestMethod]
    public void RuntimePursueCountUsesMbfCadence()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = SpawnActor(world, MobjType.Troop);
        actor.Flags |= MobjFlags.Friend | MobjFlags.NoClip;
        actor.Target = world.ConsolePlayer.Mobj;
        actor.PursueCount = 2;
        actor.Threshold = 0;
        actor.MoveDir = Direction.East;
        actor.MoveCount = 5;

        world.MonsterBehavior.Chase(actor);

        Assert.AreEqual(1, actor.PursueCount);
    }

    [TestMethod]
    public void RuntimeCompPursuitControlsSinglePlayerRetargeting()
    {
        using var smartContent = GameContent.CreateDummy(WadPath.Doom2);
        var smartWorld = CreateWorld(smartContent);
        var smartActor = SpawnFriendlyFollowerWithNearbyHostile(smartWorld, out var smartHostile);
        smartWorld.Options.MbfOptions.CompPursuit = false;
        smartActor.PursueCount = 0;
        smartActor.Threshold = 0;
        smartActor.ReactionTime = 10;

        smartWorld.MonsterBehavior.Chase(smartActor);

        Assert.AreSame(smartHostile, smartActor.Target);
        Assert.AreEqual(MbfPursuit.BaseThreshold, smartActor.PursueCount);

        using var compatContent = GameContent.CreateDummy(WadPath.Doom2);
        var compatWorld = CreateWorld(compatContent);
        var compatActor = SpawnFriendlyFollowerWithNearbyHostile(compatWorld, out _);
        var player = compatWorld.ConsolePlayer.Mobj;
        compatWorld.Options.MbfOptions.CompPursuit = true;
        compatActor.PursueCount = 0;
        compatActor.Threshold = 0;
        compatActor.ReactionTime = 10;

        compatWorld.MonsterBehavior.Chase(compatActor);

        Assert.AreSame(player, compatActor.Target);
        Assert.AreEqual(MbfPursuit.BaseThreshold, compatActor.PursueCount);
    }

    private static World CreateWorld(GameContent content)
    {
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf };
        options.Players[0].Reborn();
        return new World(content, options, null);
    }

    private static Mobj SpawnActor(World world, MobjType type)
    {
        var origin = world.ConsolePlayer.Mobj;
        return world.ThingAllocation.SpawnMobj(
            origin.X + Fixed.FromInt(64),
            origin.Y,
            Mobj.OnFloorZ,
            type);
    }

    private static Mobj SpawnFriendlyFollowerWithNearbyHostile(World world, out Mobj hostile)
    {
        // Neutralize MAP01's stock monsters so this test has exactly one
        // opposing-side candidate for the MBF friend search.
        foreach (var thinker in world.Thinkers)
        {
            if (thinker is Mobj mobj &&
                mobj.Player == null &&
                (((mobj.Flags & MobjFlags.CountKill) != 0) || mobj.Type == MobjType.Skull))
            {
                mobj.Flags |= MobjFlags.Friend;
            }
        }

        var player = world.ConsolePlayer.Mobj;
        var actor = world.ThingAllocation.SpawnMobj(
            player.X + Fixed.FromInt(64),
            player.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);
        actor.Flags |= MobjFlags.Friend | MobjFlags.NoClip;
        actor.Target = player;
        actor.MoveDir = Direction.East;
        actor.MoveCount = 5;

        hostile = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(64),
            actor.Y,
            Mobj.OnFloorZ,
            MobjType.Sergeant);

        return actor;
    }
}
