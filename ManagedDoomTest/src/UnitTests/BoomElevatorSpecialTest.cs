using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomElevatorSpecialTest
{
    [TestMethod]
    public void TranslatorMapsAllRegularElevatorSpecials()
    {
        var expectedTargets = new[]
        {
            BoomElevatorTarget.Up,
            BoomElevatorTarget.Down,
            BoomElevatorTarget.Current
        };

        var expectedTriggers = new[]
        {
            BoomTriggerType.WalkOnce,
            BoomTriggerType.WalkRepeat,
            BoomTriggerType.SwitchOnce,
            BoomTriggerType.SwitchRepeat
        };

        for (var target = 0; target < expectedTargets.Length; target++)
        {
            for (var trigger = 0; trigger < expectedTriggers.Length; trigger++)
            {
                var special = (LineSpecial)(227 + target * 4 + trigger);
                var spec = BoomElevatorTranslator.Translate(special);

                Assert.AreEqual(expectedTargets[target], spec.Target);
                Assert.AreEqual(expectedTriggers[trigger], spec.Trigger);
                Assert.AreEqual(trigger == 1 || trigger == 3, spec.Repeatable);
            }
        }

        Assert.IsFalse(BoomElevatorTranslator.TryTranslate((LineSpecial)226, out _));
        Assert.IsFalse(BoomElevatorTranslator.TryTranslate((LineSpecial)239, out _));
    }

    [TestMethod]
    public void RegularElevatorMovesFloorAndCeilingTogether()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions(), null);
        var line = world.Map.Lines.First(l => l.FrontSector != null && l.BackSector != null && l.FrontSector != l.BackSector);
        var sector = line.FrontSector;

        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        sector.FloorHeight = Fixed.FromInt(64);
        sector.CeilingHeight = Fixed.FromInt(128);

        foreach (var neighborLine in sector.Lines)
        {
            var other = neighborLine.FrontSector == sector ? neighborLine.BackSector : neighborLine.FrontSector;
            if (other != null && other != sector)
                other.FloorHeight = sector.FloorHeight;
        }

        line.BackSector.FloorHeight = Fixed.FromInt(96);
        line.Tag = 30000;
        sector.Tag = 30000;
        world.Map.BoomTags.Rebuild();

        var spec = BoomElevatorTranslator.Translate((LineSpecial)229);
        Assert.IsTrue(world.SectorAction.DoBoomElevator(line, spec));

        var elevator = sector.SpecialData as Elevator;
        Assert.IsNotNull(elevator);
        Assert.AreEqual(1, elevator.Direction);
        Assert.AreEqual(Fixed.FromInt(4).Data, elevator.Speed.Data);
        Assert.AreEqual(Fixed.FromInt(96).Data, elevator.FloorDestHeight.Data);
        Assert.AreEqual(Fixed.FromInt(160).Data, elevator.CeilingDestHeight.Data);

        elevator.Run();

        Assert.AreEqual(Fixed.FromInt(68).Data, sector.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(132).Data, sector.CeilingHeight.Data);
    }
}
