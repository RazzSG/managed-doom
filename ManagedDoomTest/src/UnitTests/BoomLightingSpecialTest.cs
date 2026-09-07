using System.Collections.Generic;
using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomLightingSpecialTest
{
    [TestMethod]
    public void TranslatorMapsAllExtendedLightingActions()
    {
        var expected = new Dictionary<int, (BoomLightingTarget Target, BoomTriggerType Trigger)>
        {
            [156] = (BoomLightingTarget.Blinking, BoomTriggerType.WalkRepeat),
            [157] = (BoomLightingTarget.MinimumNeighbor, BoomTriggerType.WalkRepeat),
            [169] = (BoomLightingTarget.MaximumNeighbor, BoomTriggerType.SwitchOnce),
            [170] = (BoomLightingTarget.Light35, BoomTriggerType.SwitchOnce),
            [171] = (BoomLightingTarget.Light255, BoomTriggerType.SwitchOnce),
            [172] = (BoomLightingTarget.Blinking, BoomTriggerType.SwitchOnce),
            [173] = (BoomLightingTarget.MinimumNeighbor, BoomTriggerType.SwitchOnce),
            [192] = (BoomLightingTarget.MaximumNeighbor, BoomTriggerType.SwitchRepeat),
            [193] = (BoomLightingTarget.Blinking, BoomTriggerType.SwitchRepeat),
            [194] = (BoomLightingTarget.MinimumNeighbor, BoomTriggerType.SwitchRepeat)
        };

        foreach (var pair in expected)
        {
            var specification = BoomLightingTranslator.Translate((LineSpecial)pair.Key);
            Assert.AreEqual(pair.Value.Target, specification.Target);
            Assert.AreEqual(pair.Value.Trigger, specification.Trigger);
            Assert.AreEqual(pair.Value.Trigger.IsRepeatable(), specification.Repeatable);
        }

        Assert.IsFalse(BoomLightingTranslator.TryTranslate((LineSpecial)155, out _));
        Assert.IsFalse(BoomLightingTranslator.TryTranslate((LineSpecial)158, out _));
        Assert.IsFalse(BoomLightingTranslator.TryTranslate((LineSpecial)168, out _));
        Assert.IsFalse(BoomLightingTranslator.TryTranslate((LineSpecial)195, out _));
    }

    [TestMethod]
    public void ExtendedLightingUsesBoomLineDispatcher()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Boom };
        var world = new World(content, options, null);
        var line = world.Map.Lines.First(l => l.FrontSector != null && l.BackSector != null && l.FrontSector != l.BackSector);
        var sector = line.FrontSector;

        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        line.Tag = 30000;
        line.Special = (LineSpecial)170;
        sector.Tag = 30000;
        sector.LightLevel = 160;
        world.Map.BoomTags.Rebuild();

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var result));
        Assert.IsTrue(result);
        Assert.AreEqual(35, sector.LightLevel);
        Assert.AreEqual(0, (int)line.Special);
    }

    [TestMethod]
    public void BoomBlinkingUsesIndependentLightingSlot()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Boom };
        var world = new World(content, options, null);
        var line = world.Map.Lines.First(l => l.FrontSector != null && l.BackSector != null && l.FrontSector != l.BackSector);
        var sector = line.FrontSector;
        var planeMover = new Thinker();

        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        line.Tag = 30000;
        sector.Tag = 30000;
        world.Map.BoomTags.Rebuild();

        sector.SpecialData = planeMover;
        sector.LightingData = null;
        sector.LightLevel = 160;

        foreach (var neighborLine in sector.Lines)
        {
            var other = neighborLine.FrontSector == sector ? neighborLine.BackSector : neighborLine.FrontSector;
            if (other != null && other != sector)
                other.LightLevel = 64;
        }

        var blink = BoomLightingTranslator.Translate((LineSpecial)172);
        Assert.IsTrue(world.SectorAction.DoBoomLighting(line, blink));

        var strobe = sector.LightingData as StrobeFlash;
        Assert.IsNotNull(strobe);
        Assert.AreSame(planeMover, sector.SpecialData);
        Assert.AreEqual(160, strobe.MaxLight);
        Assert.AreEqual(64, strobe.MinLight);
        Assert.AreEqual(StrobeFlash.SlowDark, strobe.DarkTime);
        Assert.AreEqual(StrobeFlash.StrobeBright, strobe.BrightTime);

        var firstStrobe = sector.LightingData;
        Assert.IsTrue(world.SectorAction.DoBoomLighting(line, blink));
        Assert.AreSame(firstStrobe, sector.LightingData);
    }

    [TestMethod]
    public void BoomImmediateLightingUsesNeighborExtremes()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Boom };
        var world = new World(content, options, null);
        var line = world.Map.Lines.First(l => l.FrontSector != null && l.BackSector != null && l.FrontSector != l.BackSector);
        var sector = line.FrontSector;
        var neighbors = sector.Lines
            .Select(l => l.FrontSector == sector ? l.BackSector : l.FrontSector)
            .Where(s => s != null && s != sector)
            .Distinct()
            .ToArray();

        Assert.IsTrue(neighbors.Length > 0);

        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        line.Tag = 30000;
        sector.Tag = 30000;
        world.Map.BoomTags.Rebuild();

        sector.LightLevel = 32;
        foreach (var neighbor in neighbors)
            neighbor.LightLevel = 80;
        neighbors[0].LightLevel = 192;

        var maximum = BoomLightingTranslator.Translate((LineSpecial)169);
        Assert.IsTrue(world.SectorAction.DoBoomLighting(line, maximum));
        Assert.AreEqual(192, sector.LightLevel);

        sector.LightLevel = 200;
        foreach (var neighbor in neighbors)
            neighbor.LightLevel = 96;
        neighbors[0].LightLevel = 48;

        var minimum = BoomLightingTranslator.Translate((LineSpecial)173);
        Assert.IsTrue(world.SectorAction.DoBoomLighting(line, minimum));
        Assert.AreEqual(48, sector.LightLevel);
    }
}
