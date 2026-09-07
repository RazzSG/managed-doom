using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomFloorSpecialTest
{
    [TestMethod]
    public void TranslatorDecodesGeneralizedFloorFields()
    {
        var special = (LineSpecial)(
            0x6000 |
            0x1000 |
            (3 << 10) |
            (2 << 7) |
            0x0040 |
            0x0020 |
            (3 << 3) |
            (int)BoomTriggerType.PushRepeat);

        var spec = BoomFloorTranslator.Translate(special);

        Assert.AreEqual(BoomTriggerType.PushRepeat, spec.Trigger);
        Assert.AreEqual(BoomActionSpeed.Turbo, spec.Speed);
        Assert.AreEqual(BoomPlaneDirection.Up, spec.Direction);
        Assert.AreEqual(BoomFloorTarget.NextNeighborFloor, spec.Target);
        Assert.AreEqual(BoomChangeType.TextureAndSpecial, spec.Change);
        Assert.AreEqual(BoomModelType.Numeric, spec.Model);
        Assert.IsTrue(spec.Crush);
        Assert.IsFalse(spec.AllowsMonsters);
        Assert.IsTrue(spec.Repeatable);
        Assert.IsFalse(spec.UsesTagForTargeting);

        var monsterEnabled = BoomFloorTranslator.Translate((LineSpecial)(0x6000 | 0x0020));
        Assert.AreEqual(BoomChangeType.None, monsterEnabled.Change);
        Assert.AreEqual(BoomModelType.Trigger, monsterEnabled.Model);
        Assert.IsTrue(monsterEnabled.AllowsMonsters);

        Assert.IsTrue(BoomFloorTranslator.IsFloorSpecial((LineSpecial)0x6000));
        Assert.IsTrue(BoomFloorTranslator.IsFloorSpecial((LineSpecial)0x7FFF));
        Assert.IsFalse(BoomFloorTranslator.IsFloorSpecial((LineSpecial)0x5FFF));
        Assert.IsFalse(BoomFloorTranslator.IsFloorSpecial((LineSpecial)0x8000));
    }

    [TestMethod]
    public void BoomEditSrFloorUpToCeilingTurboCrushingDecodesCorrectly()
    {
        const int boomEditSpecial = 29275; // 0x725B

        var spec = BoomFloorTranslator.Translate((LineSpecial)boomEditSpecial);

        Assert.AreEqual(BoomTriggerType.SwitchRepeat, spec.Trigger);
        Assert.AreEqual(BoomActionSpeed.Turbo, spec.Speed);
        Assert.AreEqual(BoomPlaneDirection.Up, spec.Direction);
        Assert.AreEqual(BoomFloorTarget.Ceiling, spec.Target);
        Assert.AreEqual(BoomChangeType.None, spec.Change);
        Assert.AreEqual(BoomModelType.Trigger, spec.Model);
        Assert.IsTrue(spec.Crush);
        Assert.IsFalse(spec.AllowsMonsters);
        Assert.IsTrue(spec.Repeatable);
        Assert.IsTrue(spec.UsesTagForTargeting);
    }

    [TestMethod]
    public void GeneralizedFloorCreatesConfiguredMoverWithoutApplyingChangeEarly()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions(), null);
        var line = world.Map.Lines.First(l => l.FrontSector != null && l.BackSector != null && l.FrontSector != l.BackSector);
        var sector = line.FrontSector;
        var model = line.BackSector;

        line.Tag = 30000;
        sector.Tag = 30000;
        world.Map.BoomTags.Rebuild();

        var originalFlat = sector.FloorFlat;
        var destination = Fixed.FromInt(1234);
        sector.CeilingHeight = destination;
        model.FloorHeight = destination;
        model.FloorFlat = originalFlat + 1;

        var special = (LineSpecial)(
            0x6000 |
            0x1000 |
            (2 << 10) |
            (4 << 7) |
            0x0040 |
            0x0020 |
            (3 << 3) |
            (int)BoomTriggerType.SwitchRepeat);
        var spec = BoomFloorTranslator.Translate(special);

        Assert.IsTrue(world.SectorAction.DoBoomFloor(line, spec));

        var mover = sector.SpecialData as FloorMove;
        Assert.IsNotNull(mover);
        Assert.AreEqual(FloorMoveType.GeneralizedChangeTexture, mover.Type);
        Assert.AreEqual(1, mover.Direction);
        Assert.AreEqual(Fixed.FromInt(8).Data, mover.Speed.Data);
        Assert.AreEqual(destination.Data, mover.FloorDestHeight.Data);
        Assert.IsTrue(mover.Crush);
        Assert.AreEqual(model.FloorFlat, mover.Texture);
        Assert.AreEqual(originalFlat, sector.FloorFlat);
    }
}
