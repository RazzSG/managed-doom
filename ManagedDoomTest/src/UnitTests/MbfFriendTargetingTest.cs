using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfFriendTargetingTest
{
    [TestMethod]
    public void MbfTreatsPlayersAndFriendFlaggedActorsAsFriendly()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);

        var player = world.ConsolePlayer.Mobj;
        var friend = new Mobj(world)
        {
            Flags = MobjFlags.Friend
        };

        Assert.IsTrue(MbfFriendTargeting.IsFriendly(GameCompatibility.Mbf, player));
        Assert.IsTrue(MbfFriendTargeting.IsFriendly(GameCompatibility.Mbf, friend));
    }

    [TestMethod]
    public void MbfFriendAndHostileActorsAreOpposingSides()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);

        var friend = new Mobj(world)
        {
            Flags = MobjFlags.Friend | MobjFlags.Shootable,
            Health = 100
        };
        var hostile = new Mobj(world)
        {
            Flags = MobjFlags.Shootable,
            Health = 100
        };
        var secondFriend = new Mobj(world)
        {
            Flags = MobjFlags.Friend | MobjFlags.Shootable,
            Health = 100
        };

        Assert.IsFalse(MbfFriendTargeting.AreOnSameSide(GameCompatibility.Mbf, friend, hostile));
        Assert.IsTrue(MbfFriendTargeting.AreOnSameSide(GameCompatibility.Mbf, friend, secondFriend));
        Assert.IsTrue(MbfFriendTargeting.CanAcquireTarget(GameCompatibility.Mbf, friend, hostile));
        Assert.IsFalse(MbfFriendTargeting.CanAcquireTarget(GameCompatibility.Mbf, friend, secondFriend));
    }

    [TestMethod]
    public void BoomKeepsPreMbfTargetAcquisitionRules()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Boom },
            null);

        var first = new Mobj(world)
        {
            Flags = MobjFlags.Friend | MobjFlags.Shootable,
            Health = 100
        };
        var second = new Mobj(world)
        {
            Flags = MobjFlags.Friend | MobjFlags.Shootable,
            Health = 100
        };

        Assert.IsFalse(MbfFriendTargeting.IsFriendly(GameCompatibility.Boom, first));
        Assert.IsFalse(MbfFriendTargeting.AreOnSameSide(GameCompatibility.Boom, first, second));
        Assert.IsTrue(MbfFriendTargeting.CanAcquireTarget(GameCompatibility.Boom, first, second));
    }

    [TestMethod]
    public void MbfPlayerRuntimeMobjCarriesFriendFlag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);

        Assert.IsTrue((world.ConsolePlayer.Mobj.Flags & MobjFlags.Friend) != 0);
    }

    [TestMethod]
    public void BoomPlayerRuntimeMobjDoesNotGainMbfFriendFlag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Boom },
            null);

        Assert.IsFalse((world.ConsolePlayer.Mobj.Flags & MobjFlags.Friend) != 0);
    }

    [TestMethod]
    public void FriendlyMbfActorPrefersHostileMonsterOverPlayer()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);

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
            player.X + Fixed.FromInt(1),
            player.Y,
            player.Z,
            MobjType.Troop);
        actor.Flags |= MobjFlags.Friend;
        actor.Angle = Angle.Ang0;
        actor.Target = null;

        var hostile = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(1),
            actor.Y,
            actor.Z,
            MobjType.Troop);

        world.MonsterBehavior.Look(actor);

        Assert.AreSame(hostile, actor.Target);
    }

    [TestMethod]
    public void FriendlyMbfActorFallsBackToPlayerWhenNoHostileMonsterIsAvailable()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf };
        options.Players[0].Reborn();
        var world = new World(content, options, null);

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
            player.X + Fixed.FromInt(1),
            player.Y,
            player.Z,
            MobjType.Troop);
        actor.Flags |= MobjFlags.Friend;
        actor.Angle = Angle.Ang0;
        actor.Target = null;

        world.MonsterBehavior.Look(actor);

        Assert.AreSame(player, actor.Target);
    }

    [TestMethod]
    public void BoomActorStillUsesVanillaPlayerLookPath()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Boom };
        options.Players[0].Reborn();
        var world = new World(content, options, null);

        var player = world.ConsolePlayer.Mobj;
        var actor = world.ThingAllocation.SpawnMobj(
            player.X + Fixed.FromInt(1),
            player.Y,
            player.Z,
            MobjType.Troop);
        actor.Flags |= MobjFlags.Friend;
        actor.Angle = Angle.Ang0;
        actor.Target = null;

        world.MonsterBehavior.Look(actor);

        Assert.AreSame(player, actor.Target);
    }
}
