using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomExtendedPlaneSpecialTest
{
    [TestMethod]
    public void TranslatorsMapExtendedFloorAndCeilingActions()
    {
        var lowerNext = BoomExtendedFloorTranslator.Translate((LineSpecial)219);
        Assert.AreEqual(BoomTriggerType.WalkOnce, lowerNext.Trigger);
        Assert.AreEqual(BoomPlaneDirection.Down, lowerNext.Direction);
        Assert.AreEqual(BoomFloorTarget.NextNeighborFloor, lowerNext.Target);
        Assert.AreEqual(BoomChangeType.None, lowerNext.Change);

        var lowerAndChange = BoomExtendedFloorTranslator.Translate((LineSpecial)177);
        Assert.AreEqual(BoomTriggerType.SwitchRepeat, lowerAndChange.Trigger);
        Assert.AreEqual(BoomFloorTarget.LowestNeighborFloor, lowerAndChange.Target);
        Assert.AreEqual(BoomChangeType.TextureAndSpecial, lowerAndChange.Change);
        Assert.AreEqual(BoomModelType.Numeric, lowerAndChange.Model);

        var raise512 = BoomExtendedFloorTranslator.Translate((LineSpecial)142);
        Assert.AreEqual(BoomTriggerType.WalkOnce, raise512.Trigger);
        Assert.AreEqual(BoomFloorTarget.By512, raise512.Target);

        var changeOnly = BoomExtendedFloorTranslator.Translate((LineSpecial)241);
        Assert.AreEqual(BoomTriggerType.SwitchOnce, changeOnly.Trigger);
        Assert.AreEqual(BoomFloorTarget.None, changeOnly.Target);
        Assert.AreEqual(BoomModelType.Numeric, changeOnly.Model);

        var fastToFloor = BoomExtendedCeilingTranslator.Translate((LineSpecial)152);
        Assert.AreEqual(BoomTriggerType.WalkRepeat, fastToFloor.Trigger);
        Assert.AreEqual(BoomPlaneDirection.Down, fastToFloor.Direction);
        Assert.AreEqual(BoomActionSpeed.Fast, fastToFloor.Speed);
        Assert.AreEqual(BoomCeilingTarget.Floor, fastToFloor.Target);

        var floorPlus8 = BoomExtendedCeilingTranslator.Translate((LineSpecial)167);
        Assert.AreEqual(BoomTriggerType.SwitchOnce, floorPlus8.Trigger);
        Assert.AreEqual(BoomCeilingTarget.FloorPlus8, floorPlus8.Target);

        Assert.IsFalse(BoomExtendedFloorTranslator.TryTranslate((LineSpecial)141, out _));
        Assert.IsFalse(BoomExtendedCeilingTranslator.TryTranslate((LineSpecial)207, out _));
    }

    [TestMethod]
    public void ExtendedChangesAreImmediateAndCeilingUsesSharedMover()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions(), null);
        var line = world.Map.Lines.First(l => l.FrontSector != null && l.BackSector != null && l.FrontSector != l.BackSector);
        var model = line.FrontSector;
        var sector = line.BackSector;

        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        line.Tag = 30000;
        sector.Tag = 30000;
        world.Map.BoomTags.Rebuild();

        model.FloorFlat = sector.FloorFlat + 1;
        model.Special = (SectorSpecial)7;
        sector.Special = SectorSpecial.Normal;

        var change = BoomExtendedFloorTranslator.Translate((LineSpecial)189);
        Assert.IsTrue(world.SectorAction.DoBoomFloor(line, change));
        Assert.IsNull(sector.SpecialData);
        Assert.AreEqual(model.FloorFlat, sector.FloorFlat);
        Assert.AreEqual(model.Special, sector.Special);

        sector.FloorHeight = Fixed.FromInt(64);
        sector.CeilingHeight = Fixed.FromInt(128);

        var ceilingSpec = BoomExtendedCeilingTranslator.Translate((LineSpecial)167);
        Assert.IsTrue(world.SectorAction.DoBoomCeiling(line, ceilingSpec));

        var mover = sector.SpecialData as CeilingMove;
        Assert.IsNotNull(mover);
        Assert.AreEqual(-1, mover.Direction);
        Assert.AreEqual(Fixed.One.Data, mover.Speed.Data);
        Assert.AreEqual(Fixed.FromInt(72).Data, mover.BottomHeight.Data);
    }
}
