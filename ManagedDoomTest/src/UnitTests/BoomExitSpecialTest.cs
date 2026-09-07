using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomExitSpecialTest
{
    [TestMethod]
    public void TranslatorMapsExtendedExitActions()
    {
        var normal = BoomExitTranslator.Translate((LineSpecial)197);
        Assert.AreEqual(BoomExitType.Normal, normal.Type);
        Assert.AreEqual(BoomTriggerType.GunOnce, normal.Trigger);

        var secret = BoomExitTranslator.Translate((LineSpecial)198);
        Assert.AreEqual(BoomExitType.Secret, secret.Type);
        Assert.AreEqual(BoomTriggerType.GunOnce, secret.Trigger);

        Assert.IsFalse(BoomExitTranslator.TryTranslate((LineSpecial)196, out _));
        Assert.IsFalse(BoomExitTranslator.TryTranslate((LineSpecial)199, out _));
    }

    [TestMethod]
    public void NormalExitUsesBoomImpactDispatcher()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Boom };
        var world = new World(content, options, null);
        var line = world.Map.Lines[0];

        line.Tag = 0;
        line.Special = (LineSpecial)197;

        Assert.IsTrue(BoomLineSpecials.TryShoot(world, line, world.ConsolePlayer.Mobj));
        Assert.AreEqual(0, (int)line.Special);
        Assert.IsFalse(world.SecretExit);
        Assert.AreEqual(UpdateResult.Completed, world.Update());
    }

    [TestMethod]
    public void SecretExitUsesBoomImpactDispatcher()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Boom };
        var world = new World(content, options, null);
        var line = world.Map.Lines[0];

        line.Tag = 0;
        line.Special = (LineSpecial)198;

        Assert.IsTrue(BoomLineSpecials.TryShoot(world, line, world.ConsolePlayer.Mobj));
        Assert.AreEqual(0, (int)line.Special);
        Assert.IsTrue(world.SecretExit);
        Assert.AreEqual(UpdateResult.Completed, world.Update());
    }

    [TestMethod]
    public void ExtendedExitIgnoresNonPlayerActivator()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Boom };
        var world = new World(content, options, null);
        var line = world.Map.Lines[0];
        var nonPlayer = new Mobj(world);

        line.Special = (LineSpecial)197;

        Assert.IsTrue(BoomLineSpecials.TryShoot(world, line, nonPlayer));
        Assert.AreEqual(197, (int)line.Special);
        Assert.AreNotEqual(UpdateResult.Completed, world.Update());
    }
}
