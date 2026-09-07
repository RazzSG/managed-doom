using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomLiftSpecialTest
{
    [TestMethod]
    public void TranslatorDecodesGeneralizedLiftFields()
    {
        var special = (LineSpecial)(
            0x3400 |
            (3 << 8) |
            (2 << 6) |
            0x0020 |
            (3 << 3) |
            (int)BoomTriggerType.PushRepeat);

        var spec = BoomLiftTranslator.Translate(special);

        Assert.AreEqual(BoomTriggerType.PushRepeat, spec.Trigger);
        Assert.AreEqual(BoomActionSpeed.Turbo, spec.Speed);
        Assert.AreEqual(BoomLiftTarget.LowestHighestFloorPerpetual, spec.Target);
        Assert.AreEqual(165, spec.WaitTics);
        Assert.IsTrue(spec.AllowsMonsters);
        Assert.IsTrue(spec.IsPerpetual);
        Assert.IsTrue(spec.Repeatable);
        Assert.IsFalse(spec.UsesTagForTargeting);

        var delays = new[] { 35, 105, 165, 350 };
        for (var i = 0; i < delays.Length; i++)
        {
            var delayed = BoomLiftTranslator.Translate((LineSpecial)(0x3400 | (i << 6)));
            Assert.AreEqual(delays[i], delayed.WaitTics);
        }

        Assert.IsTrue(BoomLiftTranslator.IsLiftSpecial((LineSpecial)0x3400));
        Assert.IsTrue(BoomLiftTranslator.IsLiftSpecial((LineSpecial)0x37FF));
        Assert.IsFalse(BoomLiftTranslator.IsLiftSpecial((LineSpecial)0x33FF));
        Assert.IsFalse(BoomLiftTranslator.IsLiftSpecial((LineSpecial)0x3800));
    }

    [TestMethod]
    public void GeneralizedLiftCreatesConfiguredPlatform()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions(), null);
        var line = world.Map.Lines.First(l => l.FrontSector != null && l.BackSector != null && l.FrontSector != l.BackSector);
        var sector = line.FrontSector;

        var high = Fixed.FromInt(128);
        var low = Fixed.FromInt(64);
        sector.FloorHeight = high;

        foreach (var neighborLine in sector.Lines)
        {
            var other = neighborLine.FrontSector == sector ? neighborLine.BackSector : neighborLine.FrontSector;
            if (other != null && other != sector)
                other.FloorHeight = high;
        }

        line.BackSector.FloorHeight = low;
        line.Tag = 30000;
        sector.Tag = 30000;
        world.Map.BoomTags.Rebuild();

        var special = (LineSpecial)(
            0x3400 |
            (2 << 6) |
            (3 << 3) |
            (int)BoomTriggerType.SwitchRepeat);
        var spec = BoomLiftTranslator.Translate(special);

        Assert.IsTrue(world.SectorAction.DoBoomLift(line, spec));

        var platform = sector.SpecialData as Platform;
        Assert.IsNotNull(platform);
        Assert.AreEqual(PlatformType.GeneralizedLift, platform.Type);
        Assert.AreEqual(PlatformState.Down, platform.Status);
        Assert.AreEqual(Fixed.FromInt(16).Data, platform.Speed.Data);
        Assert.AreEqual(low.Data, platform.Low.Data);
        Assert.AreEqual(high.Data, platform.High.Data);
        Assert.AreEqual(165, platform.Wait);
        Assert.IsFalse(platform.Crush);
    }
}
