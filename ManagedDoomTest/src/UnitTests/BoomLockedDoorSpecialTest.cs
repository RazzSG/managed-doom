using System;
using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomLockedDoorSpecialTest
{
    [TestMethod]
    public void TranslatorDecodesGeneralizedLockedDoorFields()
    {
        var special = (LineSpecial)(
            0x3800 |
            0x0200 |
            (7 << 6) |
            (1 << 5) |
            (3 << 3) |
            (int)BoomTriggerType.PushRepeat);

        var spec = BoomLockedDoorTranslator.Translate(special);

        Assert.AreEqual(BoomTriggerType.PushRepeat, spec.Trigger);
        Assert.AreEqual(BoomActionSpeed.Turbo, spec.Speed);
        Assert.AreEqual(BoomLockedDoorKind.OpenStay, spec.Kind);
        Assert.AreEqual(BoomLockedDoorKey.All, spec.Key);
        Assert.IsTrue(spec.SkullIsCard);
        Assert.IsTrue(spec.Repeatable);
        Assert.IsFalse(spec.UsesTagForTargeting);

        Assert.IsTrue(BoomLockedDoorTranslator.IsLockedDoorSpecial((LineSpecial)0x3800));
        Assert.IsTrue(BoomLockedDoorTranslator.IsLockedDoorSpecial((LineSpecial)0x3BFF));
        Assert.IsFalse(BoomLockedDoorTranslator.IsLockedDoorSpecial((LineSpecial)0x37FF));
        Assert.IsFalse(BoomLockedDoorTranslator.IsLockedDoorSpecial((LineSpecial)0x3C00));
    }

    [TestMethod]
    public void LockedDoorRespectsSkullCardEquivalenceAndCreatesDoor()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions(), null);
        var line = world.Map.Lines.First(l => l.BackSector != null && l.FrontSector != l.BackSector);
        var sector = line.BackSector;
        var player = world.ConsolePlayer;

        Array.Clear(player.Cards, 0, player.Cards.Length);
        player.Cards[(int)CardType.RedSkull] = true;

        line.Tag = 0;
        line.Special = (LineSpecial)(
            0x3800 |
            (1 << 6) |
            (1 << 5) |
            (2 << 3) |
            (int)BoomTriggerType.PushRepeat);

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, player.Mobj, out var result));
        Assert.IsFalse(result);
        Assert.IsNull(sector.SpecialData);
        Assert.AreEqual((string)DoomInfo.Strings.PD_REDC, player.Message);

        line.Special = (LineSpecial)((int)line.Special | 0x0200);

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, player.Mobj, out result));
        Assert.IsTrue(result);

        var door = sector.SpecialData as VerticalDoor;
        Assert.IsNotNull(door);
        Assert.AreEqual(VerticalDoorType.GeneralizedOpen, door.Type);
        Assert.AreEqual(1, door.Direction);
        Assert.AreEqual(Fixed.FromInt(8).Data, door.Speed.Data);
        Assert.AreEqual(150, door.TopWait);
    }
}
