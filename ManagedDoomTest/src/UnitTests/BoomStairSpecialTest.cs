using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomStairSpecialTest
{
    [TestMethod]
    public void TranslatorDecodesGeneralizedStairFields()
    {
        var special = (LineSpecial)(
            0x3000 |
            0x0200 |
            0x0100 |
            (3 << 6) |
            0x0020 |
            (1 << 3) |
            (int)BoomTriggerType.PushRepeat);

        var spec = BoomStairTranslator.Translate(special);

        Assert.AreEqual(BoomTriggerType.PushRepeat, spec.Trigger);
        Assert.AreEqual(BoomActionSpeed.Normal, spec.Speed);
        Assert.AreEqual(BoomPlaneDirection.Up, spec.Direction);
        Assert.AreEqual(24, spec.StepSize);
        Assert.IsTrue(spec.IgnoreTexture);
        Assert.IsTrue(spec.AllowsMonsters);
        Assert.IsTrue(spec.Repeatable);
        Assert.IsFalse(spec.UsesTagForTargeting);

        var stepSizes = new[] { 4, 8, 16, 24 };
        for (var i = 0; i < stepSizes.Length; i++)
        {
            var stepped = BoomStairTranslator.Translate((LineSpecial)(0x3000 | (i << 6)));
            Assert.AreEqual(stepSizes[i], stepped.StepSize);
        }

        Assert.IsTrue(BoomStairTranslator.IsStairSpecial((LineSpecial)0x3000));
        Assert.IsTrue(BoomStairTranslator.IsStairSpecial((LineSpecial)0x33FF));
        Assert.IsFalse(BoomStairTranslator.IsStairSpecial((LineSpecial)0x2FFF));
        Assert.IsFalse(BoomStairTranslator.IsStairSpecial((LineSpecial)0x3400));
    }

    [TestMethod]
    public void GeneralizedStairsCreateOrderedFloorMoverChainAndToggleDirection()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions(), null);

        var first = world.Map.Sectors.First(s =>
            s.Lines.Any(l => l.FrontSector == s && l.BackSector != null && l.BackSector != s));
        var chainLine = first.Lines.First(l =>
            l.FrontSector == first && l.BackSector != null && l.BackSector != first);
        var second = chainLine.BackSector;

        foreach (var sector in world.Map.Sectors)
        {
            sector.Tag = 0;
            sector.FloorFlat = 1000 + sector.Number;
        }

        first.Tag = 30000;
        first.FloorFlat = 32000;
        second.FloorFlat = 32000;
        first.FloorHeight = Fixed.Zero;
        second.FloorHeight = Fixed.Zero;
        first.CeilingHeight = Fixed.FromInt(128);
        second.CeilingHeight = Fixed.FromInt(128);

        chainLine.Tag = 30000;
        world.Map.BoomTags.Rebuild();

        var special = (LineSpecial)(
            0x3000 |
            0x0100 |
            (1 << 6) |
            (1 << 3) |
            (int)BoomTriggerType.SwitchRepeat);
        chainLine.Special = special;
        var spec = BoomStairTranslator.Translate(special);

        Assert.IsTrue(world.SectorAction.DoBoomStairs(chainLine, spec));

        var firstMover = first.SpecialData as FloorMove;
        var secondMover = second.SpecialData as FloorMove;
        Assert.IsNotNull(firstMover);
        Assert.IsNotNull(secondMover);

        Assert.AreEqual(FloorMoveType.GeneralizedStair, firstMover.Type);
        Assert.AreEqual(FloorMoveType.GeneralizedStair, secondMover.Type);
        Assert.AreEqual(1, firstMover.Direction);
        Assert.AreEqual(1, secondMover.Direction);
        Assert.AreEqual((Fixed.One / 2).Data, firstMover.Speed.Data);
        Assert.AreEqual((Fixed.One / 2).Data, secondMover.Speed.Data);
        Assert.AreEqual(Fixed.FromInt(8).Data, firstMover.FloorDestHeight.Data);
        Assert.AreEqual(Fixed.FromInt(16).Data, secondMover.FloorDestHeight.Data);
        Assert.IsFalse(firstMover.Crush);
        Assert.IsFalse(secondMover.Crush);

        Assert.AreEqual(-2, first.StairLock);
        Assert.AreEqual(-2, second.StairLock);
        Assert.AreEqual(second.Number, first.StairNextSector);
        Assert.AreEqual(first.Number, second.StairPreviousSector);

        Assert.AreEqual((int)special ^ BoomStairTranslator.DirectionMask, (int)chainLine.Special);
    }
}
