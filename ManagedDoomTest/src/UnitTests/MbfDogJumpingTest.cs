using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfDogJumpingTest
{
    [TestMethod]
    public void FeatureStartsAtMbfCompatibility()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var dog = CreateDog(world);
        var target = world.ConsolePlayer.Mobj;
        target.Flags |= MobjFlags.Friend;

        Assert.IsFalse(MbfDogJumping.ShouldAttempt(
            GameCompatibility.Boom, true, dog, target, 0));
        Assert.IsTrue(MbfDogJumping.ShouldAttempt(
            GameCompatibility.Mbf, true, dog, target, 0));
        Assert.IsTrue(MbfDogJumping.ShouldAttempt(
            GameCompatibility.Mbf21, true, dog, target, 0));
    }

    [TestMethod]
    public void OptionCanDisableDogJumping()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var dog = CreateDog(world);
        var target = world.ConsolePlayer.Mobj;
        target.Flags |= MobjFlags.Friend;

        Assert.IsFalse(MbfDogJumping.ShouldAttempt(
            GameCompatibility.Mbf, false, dog, target, 0));
    }

    [TestMethod]
    public void OnlyDogFollowingSameSideTargetCanUseSpecialDrop()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var dog = CreateDog(world);
        var target = world.ConsolePlayer.Mobj;
        target.Flags |= MobjFlags.Friend;

        Assert.IsTrue(MbfDogJumping.ShouldAttempt(
            GameCompatibility.Mbf, true, dog, target, 0));

        target.Flags &= ~MobjFlags.Friend;
        Assert.IsFalse(MbfDogJumping.ShouldAttempt(
            GameCompatibility.Mbf, true, dog, target, 0));

        var imp = world.ThingAllocation.SpawnMobj(
            dog.X,
            dog.Y,
            dog.Z,
            MobjType.Troop);
        imp.Flags |= MobjFlags.Friend;
        Assert.IsFalse(MbfDogJumping.ShouldAttempt(
            GameCompatibility.Mbf, true, imp, target, 0));
    }

    [TestMethod]
    public void FollowDistanceUsesStrict144UnitBoundary()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var dog = CreateDog(world);
        var target = world.ConsolePlayer.Mobj;
        target.Flags |= MobjFlags.Friend;

        target.X = dog.X + Fixed.FromInt(143);
        target.Y = dog.Y;
        Assert.IsTrue(MbfDogJumping.ShouldAttempt(
            GameCompatibility.Mbf, true, dog, target, 0));

        target.X = dog.X + Fixed.FromInt(144);
        Assert.IsFalse(MbfDogJumping.ShouldAttempt(
            GameCompatibility.Mbf, true, dog, target, 0));
    }

    [TestMethod]
    public void RandomThresholdMatchesOriginal235Of256Rule()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var dog = CreateDog(world);
        var target = world.ConsolePlayer.Mobj;
        target.Flags |= MobjFlags.Friend;

        Assert.IsTrue(MbfDogJumping.ShouldAttempt(
            GameCompatibility.Mbf, true, dog, target, 234));
        Assert.IsFalse(MbfDogJumping.ShouldAttempt(
            GameCompatibility.Mbf, true, dog, target, 235));
        Assert.IsFalse(MbfDogJumping.ShouldAttempt(
            GameCompatibility.Mbf, true, dog, target, 255));
    }

    [TestMethod]
    public void TargetedDropIsCappedAt128AndRequiresTargetOnLowerSide()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var dog = CreateDog(world);
        var target = world.ConsolePlayer.Mobj;
        dog.Target = target;

        var destinationFloor = Fixed.FromInt(128);
        var lowerFloor = Fixed.Zero;

        target.Z = lowerFloor;
        Assert.IsTrue(MbfDogJumping.AllowsTargetedDropoff(
            dog,
            destinationFloor,
            lowerFloor));

        Assert.IsFalse(MbfDogJumping.AllowsTargetedDropoff(
            dog,
            Fixed.FromInt(129),
            lowerFloor));

        target.Z = Fixed.FromInt(1);
        Assert.IsFalse(MbfDogJumping.AllowsTargetedDropoff(
            dog,
            destinationFloor,
            lowerFloor));
    }

    private static Mobj CreateDog(World world)
    {
        var player = world.ConsolePlayer.Mobj;
        var dog = world.ThingAllocation.SpawnMobj(
            player.X,
            player.Y,
            player.Z,
            MobjType.Dog);
        dog.Flags |= MobjFlags.Friend;
        dog.Target = player;
        return dog;
    }
}
