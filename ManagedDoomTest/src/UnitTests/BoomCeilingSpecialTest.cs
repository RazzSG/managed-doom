using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomCeilingSpecialTest
{
    [TestMethod]
    public void TranslatorDecodesGeneralizedCeilingFields()
    {
        var special = (LineSpecial)(
            0x4000 |
            0x1000 |
            (3 << 10) |
            (2 << 7) |
            0x0040 |
            0x0020 |
            (3 << 3) |
            (int)BoomTriggerType.PushRepeat);

        var spec = BoomCeilingTranslator.Translate(special);

        Assert.AreEqual(BoomTriggerType.PushRepeat, spec.Trigger);
        Assert.AreEqual(BoomActionSpeed.Turbo, spec.Speed);
        Assert.AreEqual(BoomPlaneDirection.Up, spec.Direction);
        Assert.AreEqual(BoomCeilingTarget.NextNeighborCeiling, spec.Target);
        Assert.AreEqual(BoomChangeType.TextureAndSpecial, spec.Change);
        Assert.AreEqual(BoomModelType.Numeric, spec.Model);
        Assert.IsTrue(spec.Crush);
        Assert.IsFalse(spec.AllowsMonsters);
        Assert.IsTrue(spec.Repeatable);
        Assert.IsFalse(spec.UsesTagForTargeting);

        var monsterEnabled = BoomCeilingTranslator.Translate((LineSpecial)(0x4000 | 0x0020));
        Assert.AreEqual(BoomChangeType.None, monsterEnabled.Change);
        Assert.AreEqual(BoomModelType.Trigger, monsterEnabled.Model);
        Assert.IsTrue(monsterEnabled.AllowsMonsters);

        Assert.IsTrue(BoomCeilingTranslator.IsCeilingSpecial((LineSpecial)0x4000));
        Assert.IsTrue(BoomCeilingTranslator.IsCeilingSpecial((LineSpecial)0x5FFF));
        Assert.IsFalse(BoomCeilingTranslator.IsCeilingSpecial((LineSpecial)0x3FFF));
        Assert.IsFalse(BoomCeilingTranslator.IsCeilingSpecial((LineSpecial)0x6000));
    }

    [TestMethod]
    public void GeneralizedCeilingCreatesConfiguredMoverWithoutApplyingChangeEarly()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions(), null);
        var line = world.Map.Lines.First(l => l.FrontSector != null && l.BackSector != null && l.FrontSector != l.BackSector);
        var sector = line.FrontSector;
        var model = line.BackSector;

        line.Tag = 30000;
        sector.Tag = 30000;
        world.Map.BoomTags.Rebuild();

        var originalFlat = sector.CeilingFlat;
        var destination = sector.CeilingHeight - Fixed.FromInt(16);
        sector.FloorHeight = destination;
        model.CeilingHeight = destination;
        model.CeilingFlat = originalFlat + 1;

        var special = (LineSpecial)(
            0x4000 |
            0x1000 |
            (2 << 10) |
            (4 << 7) |
            0x0020 |
            (3 << 3) |
            (int)BoomTriggerType.SwitchRepeat);
        var spec = BoomCeilingTranslator.Translate(special);

        Assert.IsTrue(world.SectorAction.DoBoomCeiling(line, spec));

        var mover = sector.SpecialData as CeilingMove;
        Assert.IsNotNull(mover);
        Assert.AreEqual(CeilingMoveType.GeneralizedChangeTexture, mover.Type);
        Assert.AreEqual(-1, mover.Direction);
        Assert.AreEqual(Fixed.FromInt(8).Data, mover.Speed.Data);
        Assert.AreEqual(destination.Data, mover.BottomHeight.Data);
        Assert.IsTrue(mover.Crush);
        Assert.AreEqual(model.CeilingFlat, mover.Texture);
        Assert.AreEqual(originalFlat, sector.CeilingFlat);

        for (var i = 0; i < 4 && sector.SpecialData != null; i++)
            mover.Run();

        Assert.IsNull(sector.SpecialData);
        Assert.AreEqual(model.CeilingFlat, sector.CeilingFlat);
    }

    [TestMethod]
    public void Special16771KeepsRawHighestNeighborFloorTargetButStopsAtCurrentFloorInBoom()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = GameCompatibility.Boom
        }, null);

        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        // Reproduce the relevant boomphysics.WAD geometry:
        // target sector floor/ceiling = 128/128, neighboring floor = 0.
        // Keep the sector empty so object collision cannot influence the first move.
        var sector = world.Map.Sectors.First(s =>
            s.FloorData == null &&
            s.CeilingData == null &&
            s.ThingList == null &&
            s.TouchingThingList == null &&
            GetNeighbors(s).Any());

        var neighbors = GetNeighbors(sector).ToArray();
        Assert.IsTrue(neighbors.Length > 0);

        sector.FloorHeight = Fixed.FromInt(128);
        sector.CeilingHeight = Fixed.FromInt(128);

        foreach (var neighbor in neighbors)
        {
            neighbor.FloorHeight = Fixed.Zero;
            neighbor.CeilingHeight = Fixed.FromInt(128);
        }

        const int tag = 30001;
        sector.Tag = tag;

        var line = world.Map.Lines.First(l => l.FrontSide != null);
        line.Tag = tag;
        world.Map.BoomTags.Rebuild();

        // 16771 / 0x4183: SR generalized ceiling, slow, down,
        // target = CtoHnF (highest neighboring floor).
        var spec = BoomCeilingTranslator.Translate((LineSpecial)16771);

        Assert.AreEqual(BoomTriggerType.SwitchRepeat, spec.Trigger);
        Assert.AreEqual(BoomActionSpeed.Slow, spec.Speed);
        Assert.AreEqual(BoomPlaneDirection.Down, spec.Direction);
        Assert.AreEqual(BoomCeilingTarget.HighestNeighborFloor, spec.Target);
        Assert.AreEqual(BoomChangeType.None, spec.Change);
        Assert.IsFalse(spec.Crush);
        Assert.IsTrue(spec.UsesTagForTargeting);

        Assert.IsTrue(world.SectorAction.DoBoomCeiling(line, spec));

        var mover = sector.CeilingData as CeilingMove;
        Assert.IsNotNull(mover);
        Assert.IsNull(sector.FloorData);
        Assert.AreEqual(-1, mover.Direction);
        Assert.AreEqual(Fixed.FromInt(1).Data, mover.Speed.Data);

        // EV_DoGenCeiling keeps the raw CtoHnF action destination at the
        // highest neighboring floor. For this geometry that raw target is 0.
        Assert.AreEqual(Fixed.Zero.Data, mover.BottomHeight.Data);

        // PrBoom T_MovePlane applies a separate Boom floor/ceiling crossing
        // guard at movement time. Because this sector already starts closed
        // (floor == ceiling == 128), the effective lowering destination is
        // clamped to the current floor. The mover therefore completes without
        // moving the ceiling through the floor.
        mover.Run();

        Assert.AreEqual(Fixed.FromInt(128).Data, sector.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(128).Data, sector.CeilingHeight.Data);
        Assert.IsNull(sector.CeilingData);
    }

    private static System.Collections.Generic.IEnumerable<Sector> GetNeighbors(Sector sector)
    {
        return sector.Lines
            .Select(line => line.FrontSector == sector ? line.BackSector : line.FrontSector)
            .Where(other => other != null && other != sector)
            .Distinct();
    }

}
