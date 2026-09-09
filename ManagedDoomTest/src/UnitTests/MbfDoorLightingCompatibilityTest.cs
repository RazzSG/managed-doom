using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using ManagedDoom.Compatibility.Mbf.Doors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfDoorLightingCompatibilityTest
{
    [TestMethod]
    public void CompatibilityGatePreservesVanillaAndBoomAndAddsMbfToggle()
    {
        Assert.IsFalse(MbfDoorLightingCompatibility.UsesTaggedManualDoorLighting(
            GameCompatibility.Vanilla, compDoorLight: false));
        Assert.IsFalse(MbfDoorLightingCompatibility.UsesGradualDoorLighting(
            GameCompatibility.Vanilla, compDoorLight: false));

        Assert.IsTrue(MbfDoorLightingCompatibility.UsesTaggedManualDoorLighting(
            GameCompatibility.Boom, compDoorLight: true));
        Assert.IsFalse(MbfDoorLightingCompatibility.UsesGradualDoorLighting(
            GameCompatibility.Boom, compDoorLight: true));

        Assert.IsTrue(MbfDoorLightingCompatibility.UsesTaggedManualDoorLighting(
            GameCompatibility.Mbf, compDoorLight: false));
        Assert.IsTrue(MbfDoorLightingCompatibility.UsesGradualDoorLighting(
            GameCompatibility.Mbf, compDoorLight: false));
        Assert.IsFalse(MbfDoorLightingCompatibility.UsesTaggedManualDoorLighting(
            GameCompatibility.Mbf, compDoorLight: true));
        Assert.IsFalse(MbfDoorLightingCompatibility.UsesGradualDoorLighting(
            GameCompatibility.Mbf, compDoorLight: true));

        Assert.IsTrue(MbfDoorLightingCompatibility.UsesTaggedManualDoorLighting(
            GameCompatibility.Mbf21, compDoorLight: false));
        Assert.IsTrue(MbfDoorLightingCompatibility.UsesGradualDoorLighting(
            GameCompatibility.Mbf21, compDoorLight: false));
        Assert.IsFalse(MbfDoorLightingCompatibility.UsesTaggedManualDoorLighting(
            GameCompatibility.Mbf21, compDoorLight: true));
        Assert.IsFalse(MbfDoorLightingCompatibility.UsesGradualDoorLighting(
            GameCompatibility.Mbf21, compDoorLight: true));
    }

    [TestMethod]
    public void GeneralizedDoorLightingHonorsCompDoorlightOnlyAtMbfBoundary()
    {
        const int tag = 30000;

        Assert.AreEqual(tag, MbfDoorLightingCompatibility.GetGeneralizedDoorLightTag(
            GameCompatibility.Boom,
            compDoorLight: true,
            BoomTriggerType.PushRepeat,
            tag));

        Assert.AreEqual(tag, MbfDoorLightingCompatibility.GetGeneralizedDoorLightTag(
            GameCompatibility.Mbf,
            compDoorLight: false,
            BoomTriggerType.PushOnce,
            tag));
        Assert.AreEqual(0, MbfDoorLightingCompatibility.GetGeneralizedDoorLightTag(
            GameCompatibility.Mbf,
            compDoorLight: true,
            BoomTriggerType.PushOnce,
            tag));
        Assert.AreEqual(0, MbfDoorLightingCompatibility.GetGeneralizedDoorLightTag(
            GameCompatibility.Mbf,
            compDoorLight: false,
            BoomTriggerType.SwitchRepeat,
            tag));
    }

    [TestMethod]
    public void ClassicManualDoorStoresLightTagAccordingToCompDoorlight()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var fixedWorld = CreateWorld(content, GameCompatibility.Mbf, compDoorLight: false);
        var fixedLine = FindUsableTwoSidedLine(fixedWorld);
        fixedLine.Special = (LineSpecial)1;
        fixedLine.Tag = 30000;
        fixedWorld.SectorAction.DoLocalDoor(fixedLine, fixedWorld.ConsolePlayer.Mobj);
        var fixedDoor = fixedLine.BackSector.SpecialData as VerticalDoor;
        Assert.IsNotNull(fixedDoor);
        Assert.AreEqual(30000, fixedDoor.LightTag);

        var compatWorld = CreateWorld(content, GameCompatibility.Mbf, compDoorLight: true);
        var compatLine = FindUsableTwoSidedLine(compatWorld);
        compatLine.Special = (LineSpecial)1;
        compatLine.Tag = 30000;
        compatWorld.SectorAction.DoLocalDoor(compatLine, compatWorld.ConsolePlayer.Mobj);
        var compatDoor = compatLine.BackSector.SpecialData as VerticalDoor;
        Assert.IsNotNull(compatDoor);
        Assert.AreEqual(0, compatDoor.LightTag);
    }

    [TestMethod]
    public void VerticalDoorConsumesCompDoorlightDuringMovement()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var fixedWorld = CreateWorld(content, GameCompatibility.Mbf, compDoorLight: false);
        var fixedSetup = PrepareLightingSector(fixedWorld);
        var fixedDoor = CreateLightingDoor(fixedWorld, fixedSetup.Sector, fixedSetup.Line);
        fixedDoor.Run();

        Assert.AreEqual(Fixed.FromInt(66).Data, fixedSetup.Sector.CeilingHeight.Data);
        Assert.AreEqual(131, fixedSetup.Sector.LightLevel);

        var compatWorld = CreateWorld(content, GameCompatibility.Mbf, compDoorLight: true);
        var compatSetup = PrepareLightingSector(compatWorld);
        var compatDoor = CreateLightingDoor(compatWorld, compatSetup.Sector, compatSetup.Line);
        compatDoor.Run();

        Assert.AreEqual(Fixed.FromInt(66).Data, compatSetup.Sector.CeilingHeight.Data);
        Assert.AreEqual(100, compatSetup.Sector.LightLevel);
    }

    private static World CreateWorld(
        GameContent content,
        GameCompatibility compatibility,
        bool compDoorLight)
    {
        var options = new GameOptions { Compatibility = compatibility };
        options.MbfOptions.CompDoorLight = compDoorLight;
        return new World(content, options, null);
    }

    private static LineDef FindUsableTwoSidedLine(World world) =>
        world.Map.Lines.First(line =>
            line.FrontSector != null &&
            line.BackSector != null &&
            line.FrontSector != line.BackSector &&
            line.BackSector.SpecialData == null);

    private static (Sector Sector, LineDef Line) PrepareLightingSector(World world)
    {
        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        var sector = world.Map.Sectors.First(candidate =>
        {
            if (candidate.ThingList != null ||
                candidate.TouchingThingList != null ||
                candidate.SpecialData != null)
            {
                return false;
            }

            var neighbors = candidate.Lines
                .Select(line => line.FrontSector == candidate ? line.BackSector : line.FrontSector)
                .Where(other => other != null && other != candidate)
                .Distinct()
                .ToArray();

            return neighbors.Length >= 2;
        });

        var neighbors = sector.Lines
            .Select(line => line.FrontSector == sector ? line.BackSector : line.FrontSector)
            .Where(other => other != null && other != sector)
            .Distinct()
            .ToArray();

        foreach (var neighbor in neighbors)
            neighbor.LightLevel = 32;
        neighbors[0].LightLevel = 224;

        var triggerLine = sector.Lines.First(line =>
            line.FrontSector == sector || line.BackSector == sector);

        triggerLine.Tag = 30000;
        sector.Tag = 30000;
        sector.LightLevel = 100;
        world.Map.BoomTags.Rebuild();

        return (sector, triggerLine);
    }

    private static VerticalDoor CreateLightingDoor(
        World world,
        Sector sector,
        LineDef line)
    {
        sector.FloorHeight = Fixed.Zero;
        sector.CeilingHeight = Fixed.FromInt(64);

        var door = new VerticalDoor(world)
        {
            Sector = sector,
            Type = VerticalDoorType.Open,
            Direction = 1,
            Speed = Fixed.FromInt(2),
            TopHeight = Fixed.FromInt(128),
            TopWait = 150,
            LightLine = line,
            LightTag = 30000
        };

        world.Thinkers.Add(door);
        sector.SpecialData = door;
        return door;
    }
}
