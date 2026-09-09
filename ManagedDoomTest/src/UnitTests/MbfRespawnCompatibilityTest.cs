using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Things;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfRespawnCompatibilityTest
{
    [TestMethod]
    public void FeatureRequiresMbfAndFixedMode()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = SpawnSpawnlessActor(world);

        Assert.IsFalse(MbfRespawnCompatibility.TryResolveSpawnlessRespawn(
            GameCompatibility.Boom, false, actor, out _, out _, out _));
        Assert.IsFalse(MbfRespawnCompatibility.TryResolveSpawnlessRespawn(
            GameCompatibility.Mbf, true, actor, out _, out _, out _));
        Assert.IsTrue(MbfRespawnCompatibility.TryResolveSpawnlessRespawn(
            GameCompatibility.Mbf, false, actor, out _, out _, out _));
    }

    [TestMethod]
    public void FixedSpawnlessRespawnUsesDeathPositionAndCurrentAngle()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = SpawnSpawnlessActor(world);
        actor.X += Fixed.FromInt(37);
        actor.Y -= Fixed.FromInt(19);
        actor.Angle = Angle.Ang90 + Angle.Ang45;

        var resolved = MbfRespawnCompatibility.TryResolveSpawnlessRespawn(
            GameCompatibility.Mbf,
            false,
            actor,
            out var x,
            out var y,
            out var angle);

        Assert.IsTrue(resolved);
        Assert.AreEqual(actor.X.Data, x.Data);
        Assert.AreEqual(actor.Y.Data, y.Data);
        Assert.AreEqual(actor.Angle.Data, angle.Data);
    }

    [TestMethod]
    public void MapSpawnedActorKeepsNormalSpawnPointPath()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content);
        var actor = SpawnSpawnlessActor(world);
        actor.SpawnPoint = new MapThing(
            actor.X + Fixed.FromInt(64),
            actor.Y,
            Angle.Ang90,
            3001,
            ThingFlags.Normal);

        Assert.IsFalse(MbfRespawnCompatibility.TryResolveSpawnlessRespawn(
            GameCompatibility.Mbf, false, actor, out _, out _, out _));
    }

    [TestMethod]
    public void NullActorDoesNotResolve()
    {
        Assert.IsFalse(MbfRespawnCompatibility.TryResolveSpawnlessRespawn(
            GameCompatibility.Mbf, false, null, out _, out _, out _));
    }

    private static World CreateWorld(GameContent content)
    {
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf };
        options.Players[0].Reborn();
        return new World(content, options, null);
    }

    private static Mobj SpawnSpawnlessActor(World world)
    {
        var player = world.ConsolePlayer.Mobj;
        var actor = world.ThingAllocation.SpawnMobj(
            player.X + Fixed.FromInt(64),
            player.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);
        actor.SpawnPoint = null;
        return actor;
    }
}
