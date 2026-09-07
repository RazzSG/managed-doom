using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomDonutSpecialTest
{
    [TestMethod]
    public void TranslatorMapsAllExtendedDonutActions()
    {
        var walkOnce = BoomDonutTranslator.Translate((LineSpecial)146);
        Assert.AreEqual(BoomTriggerType.WalkOnce, walkOnce.Trigger);
        Assert.IsFalse(walkOnce.Repeatable);

        var walkRepeat = BoomDonutTranslator.Translate((LineSpecial)155);
        Assert.AreEqual(BoomTriggerType.WalkRepeat, walkRepeat.Trigger);
        Assert.IsTrue(walkRepeat.Repeatable);

        var switchRepeat = BoomDonutTranslator.Translate((LineSpecial)191);
        Assert.AreEqual(BoomTriggerType.SwitchRepeat, switchRepeat.Trigger);
        Assert.IsTrue(switchRepeat.Repeatable);

        Assert.IsFalse(BoomDonutTranslator.TryTranslate((LineSpecial)145, out _));
        Assert.IsFalse(BoomDonutTranslator.TryTranslate((LineSpecial)154, out _));
        Assert.IsFalse(BoomDonutTranslator.TryTranslate((LineSpecial)190, out _));
        Assert.IsFalse(BoomDonutTranslator.TryTranslate((LineSpecial)192, out _));
    }

    [TestMethod]
    public void WalkOnceDonutUsesExistingDonutMoverAndConsumesOnSuccess()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom1);
        var options = CreateE1M2BoomOptions(content);
        var world = new World(content, options, null);
        var line = FindVanillaDonutLine(world);
        var tag = line.Tag;

        line.Special = (LineSpecial)146;

        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 0, world.ConsolePlayer.Mobj));
        Assert.AreEqual(0, (int)line.Special);
        Assert.IsTrue(world.Map.Sectors.Any(s => s.Tag == tag && s.SpecialData is FloorMove));
    }

    [TestMethod]
    public void RepeatableDonutsKeepTheirSpecialAfterSuccessfulActivation()
    {
        using (var content = GameContent.CreateDummy(WadPath.Doom1))
        {
            var world = new World(content, CreateE1M2BoomOptions(content), null);
            var line = FindVanillaDonutLine(world);

            line.Special = (LineSpecial)155;

            Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 1, world.ConsolePlayer.Mobj));
            Assert.AreEqual(155, (int)line.Special);
        }

        using (var content = GameContent.CreateDummy(WadPath.Doom1))
        {
            var world = new World(content, CreateE1M2BoomOptions(content), null);
            var line = FindVanillaDonutLine(world);

            line.Special = (LineSpecial)191;

            Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var result));
            Assert.IsTrue(result);
            Assert.AreEqual(191, (int)line.Special);
        }
    }

    [TestMethod]
    public void WalkOnceDonutIsNotConsumedWhenNoTaggedSectorCanStart()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom1);
        var world = new World(content, CreateE1M2BoomOptions(content), null);
        var line = FindVanillaDonutLine(world);
        var blocker = new Thinker();

        foreach (var sector in world.Map.Sectors.Where(s => s.Tag == line.Tag))
            sector.SpecialData = blocker;

        line.Special = (LineSpecial)146;

        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 0, world.ConsolePlayer.Mobj));
        Assert.AreEqual(146, (int)line.Special);
    }

    [TestMethod]
    public void ExtendedDonutRequiresPlayerAndNonZeroTag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom1);
        var world = new World(content, CreateE1M2BoomOptions(content), null);
        var line = FindVanillaDonutLine(world);
        var originalTag = line.Tag;
        var nonPlayer = new Mobj(world);

        line.Special = (LineSpecial)155;
        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 0, nonPlayer));
        Assert.AreEqual(155, (int)line.Special);
        Assert.IsFalse(world.Map.Sectors.Any(s => s.Tag == originalTag && s.SpecialData is FloorMove));

        line.Tag = 0;
        line.Special = (LineSpecial)146;
        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 0, world.ConsolePlayer.Mobj));
        Assert.AreEqual(146, (int)line.Special);
        Assert.IsFalse(world.Map.Sectors.Any(s => s.Tag == originalTag && s.SpecialData is FloorMove));
    }

    private static GameOptions CreateE1M2BoomOptions(GameContent content)
    {
        return new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Episode = 1,
            Map = 2,
            Compatibility = GameCompatibility.Boom
        };
    }

    private static LineDef FindVanillaDonutLine(World world)
    {
        return world.Map.Lines.First(l => (int)l.Special == 9 && l.Tag != 0);
    }
}
