using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using ManagedDoom.Compatibility.Mbf.Gameplay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfZombieExitCompatibilityTest
{
    [TestMethod]
    public void CompatibilityBoundaryMatchesDoomBoomMbfAndMbf21()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var vanilla = CreateZombieWorld(content, GameCompatibility.Vanilla, compZombie: false);
        var boom = CreateZombieWorld(content, GameCompatibility.Boom, compZombie: false);
        var mbfFixed = CreateZombieWorld(content, GameCompatibility.Mbf, compZombie: false);
        var mbfCompat = CreateZombieWorld(content, GameCompatibility.Mbf, compZombie: true);
        var mbf21Fixed = CreateZombieWorld(content, GameCompatibility.Mbf21, compZombie: false);
        var mbf21Compat = CreateZombieWorld(content, GameCompatibility.Mbf21, compZombie: true);

        Assert.IsTrue(CanExit(vanilla));
        Assert.IsTrue(CanExit(boom));
        Assert.IsFalse(CanExit(mbfFixed));
        Assert.IsTrue(CanExit(mbfCompat));
        Assert.IsFalse(CanExit(mbf21Fixed));
        Assert.IsTrue(CanExit(mbf21Compat));
    }

    [TestMethod]
    public void AlivePlayerIsNeverBlockedByCompZombie()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateZombieWorld(content, GameCompatibility.Mbf, compZombie: false);

        world.ConsolePlayer.Health = 1;
        world.ConsolePlayer.Mobj.Health = 1;

        Assert.IsTrue(CanExit(world));
    }

    [TestMethod]
    public void RuntimeWalkoverExit52HonorsCompZombie()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var blocked = CreateZombieWorld(content, GameCompatibility.Mbf, compZombie: false);
        var blockedLine = blocked.Map.Lines[0];
        blockedLine.Tag = 0;
        blockedLine.Special = (LineSpecial)52;

        blocked.MapInteraction.CrossSpecialLine(blockedLine, 0, blocked.ConsolePlayer.Mobj);
        Assert.AreNotEqual(UpdateResult.Completed, blocked.Update());

        var allowed = CreateZombieWorld(content, GameCompatibility.Mbf, compZombie: true);
        var allowedLine = allowed.Map.Lines[0];
        allowedLine.Tag = 0;
        allowedLine.Special = (LineSpecial)52;

        allowed.MapInteraction.CrossSpecialLine(allowedLine, 0, allowed.ConsolePlayer.Mobj);
        Assert.AreEqual(UpdateResult.Completed, allowed.Update());
    }

    [TestMethod]
    public void RuntimeSecretWalkoverExit124HonorsCompZombie()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var blocked = CreateZombieWorld(content, GameCompatibility.Mbf, compZombie: false);
        var blockedLine = blocked.Map.Lines[0];
        blockedLine.Tag = 0;
        blockedLine.Special = (LineSpecial)124;

        blocked.MapInteraction.CrossSpecialLine(blockedLine, 0, blocked.ConsolePlayer.Mobj);
        Assert.IsFalse(blocked.SecretExit);
        Assert.AreNotEqual(UpdateResult.Completed, blocked.Update());

        var allowed = CreateZombieWorld(content, GameCompatibility.Mbf, compZombie: true);
        var allowedLine = allowed.Map.Lines[0];
        allowedLine.Tag = 0;
        allowedLine.Special = (LineSpecial)124;

        allowed.MapInteraction.CrossSpecialLine(allowedLine, 0, allowed.ConsolePlayer.Mobj);
        Assert.IsTrue(allowed.SecretExit);
        Assert.AreEqual(UpdateResult.Completed, allowed.Update());
    }

    [TestMethod]
    public void RuntimeSecretGunExit198HonorsCompZombie()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var blocked = CreateZombieWorld(content, GameCompatibility.Mbf, compZombie: false);
        var blockedLine = blocked.Map.Lines[0];
        blockedLine.Tag = 0;
        blockedLine.Special = (LineSpecial)198;

        Assert.IsTrue(BoomLineSpecials.TryShoot(blocked, blockedLine, blocked.ConsolePlayer.Mobj));
        Assert.AreEqual(198, (int)blockedLine.Special);
        Assert.IsFalse(blocked.SecretExit);
        Assert.AreNotEqual(UpdateResult.Completed, blocked.Update());

        var allowed = CreateZombieWorld(content, GameCompatibility.Mbf, compZombie: true);
        var allowedLine = allowed.Map.Lines[0];
        allowedLine.Tag = 0;
        allowedLine.Special = (LineSpecial)198;

        Assert.IsTrue(BoomLineSpecials.TryShoot(allowed, allowedLine, allowed.ConsolePlayer.Mobj));
        Assert.AreEqual(0, (int)allowedLine.Special);
        Assert.IsTrue(allowed.SecretExit);
        Assert.AreEqual(UpdateResult.Completed, allowed.Update());
    }

    [TestMethod]
    public void RuntimeGunExit197HonorsCompZombieAndDoesNotConsumeBlockedLine()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var blocked = CreateZombieWorld(content, GameCompatibility.Mbf, compZombie: false);
        var blockedLine = blocked.Map.Lines[0];
        blockedLine.Tag = 0;
        blockedLine.Special = (LineSpecial)197;

        Assert.IsTrue(BoomLineSpecials.TryShoot(blocked, blockedLine, blocked.ConsolePlayer.Mobj));
        Assert.AreEqual(197, (int)blockedLine.Special);
        Assert.AreNotEqual(UpdateResult.Completed, blocked.Update());

        var allowed = CreateZombieWorld(content, GameCompatibility.Mbf, compZombie: true);
        var allowedLine = allowed.Map.Lines[0];
        allowedLine.Tag = 0;
        allowedLine.Special = (LineSpecial)197;

        Assert.IsTrue(BoomLineSpecials.TryShoot(allowed, allowedLine, allowed.ConsolePlayer.Mobj));
        Assert.AreEqual(0, (int)allowedLine.Special);
        Assert.AreEqual(UpdateResult.Completed, allowed.Update());
    }

    private static bool CanExit(World world) =>
        MbfZombieExitCompatibility.CanTriggerLineExit(
            world.Options.Compatibility,
            world.Options.MbfOptions.CompZombie,
            world.ConsolePlayer.Mobj);

    private static World CreateZombieWorld(
        GameContent content,
        GameCompatibility compatibility,
        bool compZombie)
    {
        var options = new GameOptions { Compatibility = compatibility };
        options.MbfOptions.CompZombie = compZombie;

        var world = new World(content, options, null);
        world.ConsolePlayer.Health = 0;
        world.ConsolePlayer.Mobj.Health = 0;
        return world;
    }
}
