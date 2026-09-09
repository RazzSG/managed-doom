using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfFriendDistanceTest
{
    [TestMethod]
    public void DefaultsMatchOriginalMbfRange()
    {
        Assert.AreEqual(128, MbfFriendDistance.DefaultDistanceUnits);
        Assert.AreEqual(999, MbfFriendDistance.MaxDistanceUnits);
    }

    [TestMethod]
    public void CloseFriendlyActorsMoveAwayFromEachOther()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var player = world.ConsolePlayer.Mobj;
        var friend = SpawnFriend(world, player.X + Fixed.FromInt(64), player.Y);

        Assert.IsTrue(MbfFriendDistance.ShouldMoveAway(
            world,
            friend,
            player));
    }

    [TestMethod]
    public void ExactComfortDistanceDoesNotMoveAway()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var player = world.ConsolePlayer.Mobj;
        var friend = SpawnFriend(world, player.X + Fixed.FromInt(128), player.Y);

        Assert.IsFalse(MbfFriendDistance.ShouldMoveAway(
            world,
            friend,
            player));
    }

    [TestMethod]
    public void FriendlyMonsterTargetAlsoUsesComfortDistance()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var first = SpawnFriend(world, world.ConsolePlayer.Mobj.X, world.ConsolePlayer.Mobj.Y);
        var second = SpawnFriend(world, first.X + Fixed.FromInt(64), first.Y);

        Assert.IsTrue(MbfFriendDistance.ShouldMoveAway(
            world,
            first,
            second));
    }

    [TestMethod]
    public void HostileTargetNeverTriggersFriendSpacing()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var player = world.ConsolePlayer.Mobj;
        var friend = SpawnFriend(world, player.X + Fixed.FromInt(64), player.Y);
        var hostile = world.ThingAllocation.SpawnMobj(
            friend.X + Fixed.FromInt(32),
            friend.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);

        Assert.IsFalse(MbfFriendDistance.ShouldMoveAway(
            world,
            friend,
            hostile));
    }

    [TestMethod]
    public void BoomDoesNotEnableMbfFriendSpacing()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);
        var player = world.ConsolePlayer.Mobj;
        var friend = SpawnFriend(world, player.X + Fixed.FromInt(64), player.Y);

        Assert.IsFalse(MbfFriendDistance.ShouldMoveAway(
            world,
            friend,
            player));
    }

    [TestMethod]
    public void TargetOnActivePlatformSuppressesSpacing()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var player = world.ConsolePlayer.Mobj;
        var friend = SpawnFriend(world, player.X + Fixed.FromInt(64), player.Y);
        var sector = player.Subsector.Sector;
        var previous = sector.FloorData;

        try
        {
            sector.FloorData = new Platform(world) { Sector = sector };

            Assert.IsTrue(MbfFriendDistance.IsOnLift(world, player));
            Assert.IsFalse(MbfFriendDistance.ShouldMoveAway(
                world,
                friend,
                player));
        }
        finally
        {
            sector.FloorData = previous;
        }
    }

    [TestMethod]
    public void TaggedLiftSpecialSuppressesSpacingEvenBeforeActivation()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var player = world.ConsolePlayer.Mobj;
        var friend = SpawnFriend(world, player.X + Fixed.FromInt(64), player.Y);
        var sector = player.Subsector.Sector;
        var line = world.Map.Lines[0];
        var oldSectorTag = sector.Tag;
        var oldLineTag = line.Tag;
        var oldSpecial = line.Special;

        try
        {
            sector.Tag = 731;
            line.Tag = 731;
            line.Special = (LineSpecial)62;
            world.Map.BoomTags.Rebuild();

            Assert.IsTrue(MbfFriendDistance.IsOnLift(world, player));
            Assert.IsFalse(MbfFriendDistance.ShouldMoveAway(
                world,
                friend,
                player));
        }
        finally
        {
            sector.Tag = oldSectorTag;
            line.Tag = oldLineTag;
            line.Special = oldSpecial;
            world.Map.BoomTags.Rebuild();
        }
    }

    [TestMethod]
    public void ActorUnderDamageSuppressesSpacing()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var player = world.ConsolePlayer.Mobj;
        var friend = SpawnFriend(world, player.X + Fixed.FromInt(64), player.Y);

        Assert.IsFalse(MbfFriendDistance.ShouldMoveAway(
            GameCompatibility.Mbf,
            friend,
            player,
            Fixed.FromInt(128),
            targetOnLift: false,
            actorUnderDamage: true));
    }

    [TestMethod]
    public void CustomComfortDistanceControlsDecision()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var player = world.ConsolePlayer.Mobj;
        var friend = SpawnFriend(world, player.X + Fixed.FromInt(96), player.Y);

        Assert.IsFalse(MbfFriendDistance.ShouldMoveAway(
            GameCompatibility.Mbf,
            friend,
            player,
            Fixed.FromInt(64),
            targetOnLift: false,
            actorUnderDamage: false));

        Assert.IsTrue(MbfFriendDistance.ShouldMoveAway(
            GameCompatibility.Mbf,
            friend,
            player,
            Fixed.FromInt(128),
            targetOnLift: false,
            actorUnderDamage: false));
    }

    [TestMethod]
    public void LiftSpecialTableMatchesMbfSet()
    {
        Assert.IsTrue(MbfFriendDistance.IsLiftSpecial((LineSpecial)10));
        Assert.IsTrue(MbfFriendDistance.IsLiftSpecial((LineSpecial)143));
        Assert.IsTrue(MbfFriendDistance.IsLiftSpecial((LineSpecial)211));
        Assert.IsTrue(MbfFriendDistance.IsLiftSpecial((LineSpecial)236));
        Assert.IsFalse(MbfFriendDistance.IsLiftSpecial((LineSpecial)1));
        Assert.IsFalse(MbfFriendDistance.IsLiftSpecial((LineSpecial)242));
    }

    private static World CreateWorld(GameContent content, GameCompatibility compatibility)
    {
        var options = new GameOptions { Compatibility = compatibility };
        options.Players[0].Reborn();
        return new World(content, options, null);
    }

    private static Mobj SpawnFriend(World world, Fixed x, Fixed y)
    {
        var friend = world.ThingAllocation.SpawnMobj(
            x,
            y,
            Mobj.OnFloorZ,
            MobjType.Troop);
        friend.Flags |= MobjFlags.Friend;
        friend.MoveDir = Direction.None;
        friend.MoveCount = 0;
        return friend;
    }
}
