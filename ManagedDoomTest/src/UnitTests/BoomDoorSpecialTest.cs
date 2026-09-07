using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomDoorSpecialTest
{
    [TestMethod]
    public void TranslatorDecodesGeneralizedDoorFields()
    {
        var special = (LineSpecial)(
            0x3C00 |
            (3 << 8) |
            0x0080 |
            (2 << 5) |
            (3 << 3) |
            (int)BoomTriggerType.PushRepeat);

        var spec = BoomDoorTranslator.Translate(special);

        Assert.AreEqual(BoomTriggerType.PushRepeat, spec.Trigger);
        Assert.AreEqual(BoomActionSpeed.Turbo, spec.Speed);
        Assert.AreEqual(BoomDoorKind.CloseWaitOpen, spec.Kind);
        Assert.AreEqual(1050, spec.WaitTics);
        Assert.IsTrue(spec.AllowsMonsters);
        Assert.IsTrue(spec.Repeatable);
        Assert.IsFalse(spec.UsesTagForTargeting);

        var delays = new[] { 35, 150, 300, 1050 };
        for (var i = 0; i < delays.Length; i++)
        {
            var delayed = BoomDoorTranslator.Translate((LineSpecial)(0x3C00 | (i << 8)));
            Assert.AreEqual(delays[i], delayed.WaitTics);
        }

        Assert.IsTrue(BoomDoorTranslator.IsDoorSpecial((LineSpecial)0x3C00));
        Assert.IsTrue(BoomDoorTranslator.IsDoorSpecial((LineSpecial)0x3FFF));
        Assert.IsFalse(BoomDoorTranslator.IsDoorSpecial((LineSpecial)0x3BFF));
        Assert.IsFalse(BoomDoorTranslator.IsDoorSpecial((LineSpecial)0x4000));
    }

    [TestMethod]
    public void GeneralizedDoorCreatesConfiguredVerticalDoor()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions(), null);
        var line = world.Map.Lines.First(l => l.FrontSector != null && l.BackSector != null && l.FrontSector != l.BackSector);
        var sector = line.FrontSector;

        line.Tag = 30000;
        sector.Tag = 30000;
        world.Map.BoomTags.Rebuild();

        var originalCeiling = sector.CeilingHeight;
        var special = (LineSpecial)(
            0x3C00 |
            (2 << 8) |
            (2 << 5) |
            (2 << 3) |
            (int)BoomTriggerType.SwitchRepeat);
        var spec = BoomDoorTranslator.Translate(special);

        Assert.IsTrue(world.SectorAction.DoBoomDoor(line, spec));

        var door = sector.SpecialData as VerticalDoor;
        Assert.IsNotNull(door);
        Assert.AreEqual(VerticalDoorType.GeneralizedCloseThenOpen, door.Type);
        Assert.AreEqual(-1, door.Direction);
        Assert.AreEqual(Fixed.FromInt(8).Data, door.Speed.Data);
        Assert.AreEqual(300, door.TopWait);
        Assert.AreEqual(originalCeiling.Data, door.TopHeight.Data);
    }
}
