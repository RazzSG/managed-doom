using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomCrusherSpecialTest
{
    [TestMethod]
    public void TranslatorDecodesGeneralizedCrusherFields()
    {
        var special = (LineSpecial)(
            0x2F80 |
            0x0040 |
            0x0020 |
            (3 << 3) |
            (int)BoomTriggerType.PushRepeat);

        var spec = BoomCrusherTranslator.Translate(special);

        Assert.AreEqual(BoomTriggerType.PushRepeat, spec.Trigger);
        Assert.AreEqual(BoomActionSpeed.Turbo, spec.Speed);
        Assert.IsTrue(spec.Silent);
        Assert.IsTrue(spec.AllowsMonsters);
        Assert.IsTrue(spec.Repeatable);
        Assert.IsFalse(spec.UsesTagForTargeting);

        Assert.IsTrue(BoomCrusherTranslator.IsCrusherSpecial((LineSpecial)0x2F80));
        Assert.IsTrue(BoomCrusherTranslator.IsCrusherSpecial((LineSpecial)0x2FFF));
        Assert.IsFalse(BoomCrusherTranslator.IsCrusherSpecial((LineSpecial)0x2F7F));
        Assert.IsFalse(BoomCrusherTranslator.IsCrusherSpecial((LineSpecial)0x3000));
    }

    [TestMethod]
    public void GeneralizedCrusherCreatesConfiguredMoverReversesAndRestarts()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions(), null);
        var line = world.Map.Lines.First(l => l.FrontSector != null && l.BackSector != null && l.FrontSector != l.BackSector);
        var sector = line.FrontSector;

        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        line.Tag = 30000;
        sector.Tag = 30000;
        sector.FloorHeight = Fixed.Zero;
        sector.CeilingHeight = Fixed.FromInt(128);
        world.Map.BoomTags.Rebuild();

        var special = (LineSpecial)(
            0x2F80 |
            0x0040 |
            (1 << 3) |
            (int)BoomTriggerType.SwitchRepeat);
        var spec = BoomCrusherTranslator.Translate(special);

        Assert.IsTrue(world.SectorAction.DoBoomCrusher(line, spec));

        var mover = sector.SpecialData as CeilingMove;
        Assert.IsNotNull(mover);
        Assert.AreEqual(CeilingMoveType.GeneralizedSilentCrusher, mover.Type);
        Assert.AreEqual(-1, mover.Direction);
        Assert.IsTrue(mover.Crush);
        Assert.AreEqual(Fixed.FromInt(2).Data, mover.Speed.Data);
        Assert.AreEqual(Fixed.FromInt(2).Data, mover.OldSpeed.Data);
        Assert.AreEqual(Fixed.FromInt(8).Data, mover.BottomHeight.Data);
        Assert.AreEqual(Fixed.FromInt(128).Data, mover.TopHeight.Data);

        sector.CeilingHeight = mover.BottomHeight;
        mover.Run();

        Assert.AreEqual(1, mover.Direction);
        Assert.AreEqual(mover.OldSpeed.Data, mover.Speed.Data);

        Assert.IsTrue(world.SectorAction.CeilingCrushStop(line));
        Assert.AreEqual(0, mover.Direction);
        Assert.IsTrue(world.SectorAction.DoBoomCrusher(line, spec));
        Assert.AreEqual(1, mover.Direction);
        Assert.AreSame(mover, sector.SpecialData);
    }
}
