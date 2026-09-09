using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfFriendAssistanceTest
{
    [TestMethod]
    public void FeatureRequiresMbfAndEnabledOption()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = SpawnFriend(world, MobjType.Troop);

        Assert.IsFalse(MbfFriendAssistance.TryAcquireThreat(world, false, actor));
        Assert.IsTrue(GameCompatibilityFeatures.SupportsMbfHelpFriends(GameCompatibility.Mbf));
        Assert.IsFalse(GameCompatibilityFeatures.SupportsMbfHelpFriends(GameCompatibility.Boom));
    }

    [TestMethod]
    public void HelperBelowOneThirdHealthUsesSelfPreservation()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = SpawnFriend(world, MobjType.Troop);
        actor.Health = (actor.Info.SpawnHealth - 1) / 3;

        Assert.IsFalse(MbfFriendAssistance.TryAcquireThreat(world, true, actor));
    }

    [TestMethod]
    public void BadlyHurtFriendCanRedirectActorToVisibleOpposingMonster()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var origin = world.ConsolePlayer.Mobj;

        var actor = world.ThingAllocation.SpawnMobj(
            origin.X + Fixed.FromInt(8),
            origin.Y,
            origin.Z,
            MobjType.Troop);
        actor.Flags |= MobjFlags.Friend;

        var current = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(16),
            actor.Y,
            actor.Z,
            MobjType.Sergeant);
        actor.Target = current;

        var friend = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(1),
            actor.Y,
            actor.Z,
            MobjType.Troop);
        friend.Flags |= MobjFlags.Friend | MobjFlags.JustHit;
        friend.Health = System.Math.Max(1, (friend.Info.SpawnHealth - 1) / 2);

        var threat = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(2),
            actor.Y,
            actor.Z,
            MobjType.Sergeant);
        friend.Target = threat;

        Assert.IsTrue(MbfFriendAssistance.TryAcquireThreat(world, true, actor));
        Assert.AreSame(threat, actor.Target);
        Assert.AreEqual(MbfFriendAssistance.RescueThreshold, actor.Threshold);
        Assert.AreSame(current, actor.LastEnemy);
    }

    [TestMethod]
    public void RescueDoesNotTreatOrdinaryHostileInfightingAsOpposingFaction()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var origin = world.ConsolePlayer.Mobj;

        var actor = world.ThingAllocation.SpawnMobj(origin.X, origin.Y, origin.Z, MobjType.Troop);
        var friend = world.ThingAllocation.SpawnMobj(actor.X + Fixed.FromInt(1), actor.Y, actor.Z, MobjType.Sergeant);
        friend.Flags |= MobjFlags.JustHit;
        friend.Health = System.Math.Max(1, (friend.Info.SpawnHealth - 1) / 2);

        var hostileInfighter = world.ThingAllocation.SpawnMobj(actor.X + Fixed.FromInt(2), actor.Y, actor.Z, MobjType.Troop);
        friend.Target = hostileInfighter;

        Assert.IsFalse(MbfFriendAssistance.CanAssistAgainst(
            GameCompatibility.Mbf,
            actor,
            hostileInfighter));
        Assert.IsFalse(MbfFriendAssistance.TryAcquireThreat(world, true, actor));
    }

    [TestMethod]
    public void FriendlyActorCanAssistAgainstHostileMonsterButNotPlayer()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = SpawnFriend(world, MobjType.Troop);
        var hostile = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(1), actor.Y, actor.Z, MobjType.Sergeant);

        Assert.IsTrue(MbfFriendAssistance.CanAssistAgainst(GameCompatibility.Mbf, actor, hostile));
        Assert.IsFalse(MbfFriendAssistance.CanAssistAgainst(
            GameCompatibility.Mbf,
            actor,
            world.ConsolePlayer.Mobj));
    }

    [TestMethod]
    public void HealthyFriendStopGateMatchesMbfThreshold()
    {
        Assert.IsTrue(MbfFriendAssistance.ShouldStopAtHealthyFriend(0));
        Assert.IsTrue(MbfFriendAssistance.ShouldStopAtHealthyFriend(179));
        Assert.IsFalse(MbfFriendAssistance.ShouldStopAtHealthyFriend(180));
        Assert.IsFalse(MbfFriendAssistance.ShouldStopAtHealthyFriend(255));
    }

    [TestMethod]
    public void EngagedThreatSkipMatchesMbfReciprocalFightRule()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = SpawnFriend(world, MobjType.Troop);
        var threat = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(4), actor.Y, actor.Z, MobjType.Sergeant);
        var opponent = SpawnFriend(world, MobjType.Troop);

        threat.Target = opponent;
        opponent.Target = threat;
        opponent.Health = opponent.Info.SpawnHealth;

        Assert.IsTrue(MbfFriendAssistance.IsReciprocallyEngaged(threat));
        Assert.IsFalse(MbfFriendAssistance.ShouldSkipEngagedThreat(
            GameCompatibility.Boom, threat, 255));
        Assert.IsFalse(MbfFriendAssistance.ShouldSkipEngagedThreat(
            GameCompatibility.Mbf, threat, 100));
        Assert.IsTrue(MbfFriendAssistance.ShouldSkipEngagedThreat(
            GameCompatibility.Mbf, threat, 101));
        Assert.IsTrue(MbfFriendAssistance.ShouldSkipEngagedThreat(
            GameCompatibility.Mbf, threat, 255));

        opponent.Health = System.Math.Max(1, (opponent.Info.SpawnHealth - 1) / 2);
        Assert.IsFalse(MbfFriendAssistance.ShouldSkipEngagedThreat(
            GameCompatibility.Mbf, threat, 255));

        opponent.Health = opponent.Info.SpawnHealth;
        opponent.Target = null;
        Assert.IsFalse(MbfFriendAssistance.IsReciprocallyEngaged(threat));
        Assert.IsFalse(MbfFriendAssistance.ShouldSkipEngagedThreat(
            GameCompatibility.Mbf, threat, 255));
    }

    [TestMethod]
    public void HelpFriendsDoesNotRunWhileTargetThresholdIsActive()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var origin = world.ConsolePlayer.Mobj;

        var actor = world.ThingAllocation.SpawnMobj(
            origin.X + Fixed.FromInt(8), origin.Y, origin.Z, MobjType.Troop);
        actor.Flags |= MobjFlags.Friend;

        var current = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(256), actor.Y, actor.Z, MobjType.Sergeant);
        actor.Target = current;
        actor.Threshold = 2;
        actor.MoveCount = 1;

        var friend = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(1), actor.Y, actor.Z, MobjType.Troop);
        friend.Flags |= MobjFlags.Friend | MobjFlags.JustHit;
        friend.Health = System.Math.Max(1, (friend.Info.SpawnHealth - 1) / 2);

        var threat = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(2), actor.Y, actor.Z, MobjType.Sergeant);
        friend.Target = threat;

        world.Options.MbfOptions.HelpFriends = true;
        world.MonsterBehavior.Chase(actor);

        Assert.AreSame(current, actor.Target);
        Assert.AreEqual(1, actor.Threshold);
    }

    [TestMethod]
    public void ChaseUsesHelpFriendsOnlyWhenOptionIsEnabled()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var origin = world.ConsolePlayer.Mobj;

        var actor = world.ThingAllocation.SpawnMobj(
            origin.X + Fixed.FromInt(8), origin.Y, origin.Z, MobjType.Troop);
        actor.Flags |= MobjFlags.Friend;

        var current = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(256), actor.Y, actor.Z, MobjType.Sergeant);
        actor.Target = current;

        var friend = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(1), actor.Y, actor.Z, MobjType.Troop);
        friend.Flags |= MobjFlags.Friend | MobjFlags.JustHit;
        friend.Health = System.Math.Max(1, (friend.Info.SpawnHealth - 1) / 2);

        var threat = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(2), actor.Y, actor.Z, MobjType.Sergeant);
        friend.Target = threat;

        world.Options.MbfOptions.HelpFriends = false;
        world.MonsterBehavior.Chase(actor);
        Assert.AreNotSame(threat, actor.Target);

        actor.Target = current;
        actor.LastEnemy = null;
        actor.Threshold = 0;
        actor.MoveCount = 1;
        actor.Flags &= ~MobjFlags.JustAttacked;
        world.Options.MbfOptions.HelpFriends = true;
        world.MonsterBehavior.Chase(actor);

        Assert.AreSame(threat, actor.Target);
        Assert.AreEqual(MbfFriendAssistance.RescueThreshold, actor.Threshold);
    }

    private static World CreateWorld(GameContent content)
    {
        var options = new GameOptions
        {
            Compatibility = GameCompatibility.Mbf
        };
        options.Players[0].Reborn();
        return new World(content, options, null);
    }

    private static Mobj SpawnFriend(World world, MobjType type)
    {
        var origin = world.ConsolePlayer.Mobj;
        var actor = world.ThingAllocation.SpawnMobj(origin.X, origin.Y, origin.Z, type);
        actor.Flags |= MobjFlags.Friend;
        return actor;
    }
}
