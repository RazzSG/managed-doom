using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfMonkeyClimbingTest
{
    [TestMethod]
    public void FeatureStartsAtMbfAndDefaultsOff()
    {
        Assert.IsFalse(MbfMonkeyClimbing.Applies(GameCompatibility.Boom, true));
        Assert.IsFalse(MbfMonkeyClimbing.Applies(GameCompatibility.Mbf, false));
        Assert.IsTrue(MbfMonkeyClimbing.Applies(GameCompatibility.Mbf, true));
        Assert.IsTrue(MbfMonkeyClimbing.Applies(GameCompatibility.Mbf21, true));
    }

    [TestMethod]
    public void ClassicRuleBlocksWideDestinationDropoff()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var actor = SpawnActor(world);

        actor.FloorZ = Fixed.FromInt(64);
        actor.DropoffZ = Fixed.FromInt(64);

        Assert.IsFalse(MbfMonkeyClimbing.BlocksMove(
            GameCompatibility.Mbf, false, actor, Fixed.FromInt(64), Fixed.FromInt(40)));
        Assert.IsTrue(MbfMonkeyClimbing.BlocksMove(
            GameCompatibility.Mbf, false, actor, Fixed.FromInt(64), Fixed.FromInt(39)));
    }

    [TestMethod]
    public void MonkeysAllowsWideStairSpanWhenEachEdgeChangesByAtMost24()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var actor = SpawnActor(world);

        actor.FloorZ = Fixed.FromInt(48);
        actor.DropoffZ = Fixed.FromInt(24);

        // Destination spans 48 units from its highest to lowest contacted
        // floors. Doom's old span check rejects this, while MBF monkeys lets
        // the monster continue: the normal step-up guard handles the +24 floor,
        // while the remembered low edge does not drop at all.
        Assert.IsTrue(MbfMonkeyClimbing.BlocksMove(
            GameCompatibility.Mbf, false, actor, Fixed.FromInt(72), Fixed.FromInt(24)));
        Assert.IsFalse(MbfMonkeyClimbing.BlocksMove(
            GameCompatibility.Mbf, true, actor, Fixed.FromInt(72), Fixed.FromInt(24)));
    }

    [TestMethod]
    public void MonkeysStillBlocksDownwardChangeGreaterThan24()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var actor = SpawnActor(world);

        actor.FloorZ = Fixed.FromInt(64);
        actor.DropoffZ = Fixed.FromInt(40);

        Assert.IsFalse(MbfMonkeyClimbing.BlocksMove(
            GameCompatibility.Mbf, true, actor, Fixed.FromInt(40), Fixed.FromInt(16)));
        Assert.IsTrue(MbfMonkeyClimbing.BlocksMove(
            GameCompatibility.Mbf, true, actor, Fixed.FromInt(39), Fixed.FromInt(16)));

        actor.FloorZ = Fixed.FromInt(64);
        actor.DropoffZ = Fixed.FromInt(40);
        Assert.IsTrue(MbfMonkeyClimbing.BlocksMove(
            GameCompatibility.Mbf, true, actor, Fixed.FromInt(64), Fixed.FromInt(15)));
    }

    [TestMethod]
    public void SpawnInitializesRememberedDropoffToFloor()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var actor = SpawnActor(world);

        Assert.AreEqual(actor.FloorZ.Data, actor.DropoffZ.Data);
    }

    private static Mobj SpawnActor(World world)
    {
        var player = world.ConsolePlayer.Mobj;
        return world.ThingAllocation.SpawnMobj(
            player.X,
            player.Y,
            player.Z,
            MobjType.Troop);
    }
}
