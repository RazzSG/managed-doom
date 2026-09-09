using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfMonsterBackingTest
{
    [TestMethod]
    public void FeatureRequiresMbfAndEnabledOption()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var (actor, target) = SpawnOpposingPair(world, Fixed.FromInt(96));

        Assert.IsFalse(MbfMonsterBacking.ShouldBackAway(GameCompatibility.Boom, true, actor, target));
        Assert.IsFalse(MbfMonsterBacking.ShouldBackAway(GameCompatibility.Mbf, false, actor, target));
        Assert.IsTrue(MbfMonsterBacking.ShouldBackAway(GameCompatibility.Mbf, true, actor, target));
    }

    [TestMethod]
    public void RangedActorBacksAwayFromCloseMeleeOnlyOpponent()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var (actor, target) = SpawnOpposingPair(world, Fixed.FromInt(127));

        Assert.AreNotEqual((MobjState)0, actor.Info.MissileState);
        Assert.AreEqual((MobjState)0, target.Info.MissileState);
        Assert.IsTrue(MbfMonsterBacking.ShouldBackAway(GameCompatibility.Mbf, true, actor, target));

        target.X = actor.X + Fixed.FromInt(128);
        Assert.IsFalse(MbfMonsterBacking.ShouldBackAway(GameCompatibility.Mbf, true, actor, target));
    }

    [TestMethod]
    public void OrdinaryHostileInfightingDoesNotUseMbfBacking()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = world.ThingAllocation.SpawnMobj(
            world.ConsolePlayer.Mobj.X,
            world.ConsolePlayer.Mobj.Y,
            world.ConsolePlayer.Mobj.Z,
            MobjType.Troop);
        var target = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(64),
            actor.Y,
            actor.Z,
            MobjType.Sergeant);

        Assert.IsFalse(MbfMonsterBacking.ShouldBackAway(GameCompatibility.Mbf, true, actor, target));
    }

    [TestMethod]
    public void SkullNeverUsesBackingBehavior()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = world.ThingAllocation.SpawnMobj(
            world.ConsolePlayer.Mobj.X,
            world.ConsolePlayer.Mobj.Y,
            world.ConsolePlayer.Mobj.Z,
            MobjType.Skull);
        actor.Flags |= MobjFlags.Friend;
        var target = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(32),
            actor.Y,
            actor.Z,
            MobjType.Sergeant);

        Assert.IsFalse(MbfMonsterBacking.ShouldBackAway(GameCompatibility.Mbf, true, actor, target));
    }

    [TestMethod]
    public void PlayerMeleeStateExtendsBackingDistanceToThreeMeleeRanges()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var player = world.ConsolePlayer;
        var actor = world.ThingAllocation.SpawnMobj(
            player.Mobj.X + Fixed.FromInt(160),
            player.Mobj.Y,
            player.Mobj.Z,
            MobjType.Troop);

        player.ReadyWeapon = WeaponType.Fist;
        Assert.IsTrue(MbfMonsterBacking.ShouldBackAway(GameCompatibility.Mbf, true, actor, player.Mobj));

        player.ReadyWeapon = WeaponType.Pistol;
        Assert.IsFalse(MbfMonsterBacking.ShouldBackAway(GameCompatibility.Mbf, true, actor, player.Mobj));
    }

    [TestMethod]
    public void RuntimeBackingCreatesSeparateStrafeLifetime()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var (actor, target) = SpawnOpposingPair(world, Fixed.FromInt(64));

        world.Options.MbfOptions.MonsterBacking = true;
        world.Random.Clear();

        actor.Target = target;
        actor.Flags |= MobjFlags.JustAttacked;
        actor.MoveCount = 0;
        actor.StrafeCount = 0;

        world.MonsterBehavior.Chase(actor);

        // The first deterministic Doom RNG value after Clear() is 8. MBF
        // stores it in strafecount and then mirrors it to movecount only
        // after choosing the retreat direction.
        Assert.AreEqual(8, actor.StrafeCount);
        Assert.AreEqual(8, actor.MoveCount);
    }

    [TestMethod]
    public void ActiveMbfStrafeKeepsFacingTargetDuringAttackEarlyOut()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var (actor, target) = SpawnOpposingPair(world, Fixed.FromInt(32));

        actor.Target = target;
        actor.MoveDir = Direction.west;
        actor.Angle = Angle.Ang180;
        actor.MoveCount = 5;
        actor.StrafeCount = 2;

        world.MonsterBehavior.Chase(actor);

        Assert.AreEqual(Angle.Ang0.Data, actor.Angle.Data);
        Assert.AreEqual(2, actor.StrafeCount);
    }

    [TestMethod]
    public void ActiveMbfStrafeCountsDownWhenChaseReachesMovement()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var (actor, target) = SpawnOpposingPair(world, Fixed.FromInt(256));

        actor.Target = target;
        actor.MoveDir = Direction.East;
        actor.MoveCount = 5;
        actor.StrafeCount = 2;
        // Phase 19.19 added MBF's periodic target re-evaluation. Keep the
        // pursuit window active so this test specifically reaches the movement
        // portion whose responsibility is to count strafecount down.
        actor.PursueCount = 1;
        actor.Flags |= MobjFlags.NoClip;

        world.MonsterBehavior.Chase(actor);

        Assert.AreEqual(0, actor.PursueCount);
        Assert.AreEqual(1, actor.StrafeCount);
    }

    [TestMethod]
    public void PreMbfCompatibilityIgnoresStaleStrafeFacingState()
    {
        using var controlContent = GameContent.CreateDummy(WadPath.Doom2);
        var controlWorld = CreateWorld(controlContent, GameCompatibility.Boom);
        var (controlActor, controlTarget) = SpawnOpposingPair(controlWorld, Fixed.FromInt(32));

        controlActor.Target = controlTarget;
        controlActor.MoveDir = Direction.west;
        controlActor.Angle = Angle.Ang0;
        controlActor.MoveCount = 5;
        controlActor.StrafeCount = 0;

        controlWorld.Random.Clear();
        controlWorld.MonsterBehavior.Chase(controlActor);

        using var staleContent = GameContent.CreateDummy(WadPath.Doom2);
        var staleWorld = CreateWorld(staleContent, GameCompatibility.Boom);
        var (staleActor, staleTarget) = SpawnOpposingPair(staleWorld, Fixed.FromInt(32));

        staleActor.Target = staleTarget;
        staleActor.MoveDir = Direction.west;
        staleActor.Angle = Angle.Ang0;
        staleActor.MoveCount = 5;
        staleActor.StrafeCount = 2;

        staleWorld.Random.Clear();
        staleWorld.MonsterBehavior.Chase(staleActor);

        Assert.AreEqual(GameCompatibility.Boom, staleWorld.Options.Compatibility);
        Assert.IsFalse(GameCompatibilityFeatures.SupportsMbfMonsterBacking(staleWorld.Options.Compatibility));
        Assert.AreEqual(controlActor.Angle.Data, staleActor.Angle.Data);
        Assert.AreEqual(controlActor.MoveDir, staleActor.MoveDir);
        Assert.AreEqual(controlActor.MoveCount, staleActor.MoveCount);
    }

    [TestMethod]
    public void DeadTargetDoesNotTriggerBacking()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var (actor, target) = SpawnOpposingPair(world, Fixed.FromInt(64));
        target.Health = 0;

        Assert.IsFalse(MbfMonsterBacking.ShouldBackAway(GameCompatibility.Mbf, true, actor, target));
    }

    private static World CreateWorld(
        GameContent content,
        GameCompatibility compatibility = GameCompatibility.Mbf)
    {
        var options = new GameOptions
        {
            Compatibility = compatibility
        };
        options.Players[0].Reborn();
        return new World(content, options, null);
    }

    private static (Mobj Actor, Mobj Target) SpawnOpposingPair(World world, Fixed distance)
    {
        var origin = world.ConsolePlayer.Mobj;
        var actor = world.ThingAllocation.SpawnMobj(
            origin.X,
            origin.Y,
            origin.Z,
            MobjType.Troop);
        actor.Flags |= MobjFlags.Friend;

        var target = world.ThingAllocation.SpawnMobj(
            actor.X + distance,
            actor.Y,
            actor.Z,
            MobjType.Sergeant);

        return (actor, target);
    }
}
