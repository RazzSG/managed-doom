using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Doors;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomDoorCompatibilityFixesTest
{
    [TestMethod]
    public void DoorFixesEnableAtBoomWhileGradualLightingStartsAtMbf()
    {
        Assert.IsFalse(BoomDoorCompatibility.FixesBlazingDoorSounds(GameCompatibility.Vanilla));
        Assert.IsFalse(BoomDoorCompatibility.UsesTaggedManualDoorLighting(GameCompatibility.Vanilla));
        Assert.IsFalse(BoomDoorCompatibility.UsesGradualDoorLighting(GameCompatibility.Vanilla));

        Assert.IsTrue(BoomDoorCompatibility.FixesBlazingDoorSounds(GameCompatibility.Boom));
        Assert.IsTrue(BoomDoorCompatibility.UsesTaggedManualDoorLighting(GameCompatibility.Boom));
        Assert.IsFalse(BoomDoorCompatibility.UsesGradualDoorLighting(GameCompatibility.Boom));

        foreach (var compatibility in new[] { GameCompatibility.Mbf, GameCompatibility.Mbf21 })
        {
            Assert.IsTrue(BoomDoorCompatibility.FixesBlazingDoorSounds(compatibility));
            Assert.IsTrue(BoomDoorCompatibility.UsesTaggedManualDoorLighting(compatibility));
            Assert.IsTrue(BoomDoorCompatibility.UsesGradualDoorLighting(compatibility));
        }
    }

    [TestMethod]
    public void DoorstuckDecisionMatchesVanillaBoomAndMbfRules()
    {
        var blocking = BoomDoorCompatibility.BlockingLineActivated;
        var other = BoomDoorCompatibility.OtherLineActivated;

        Assert.IsFalse(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Vanilla, 0, 0));
        Assert.IsTrue(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Vanilla, blocking, 0));

        Assert.IsFalse(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Boom, blocking, 0));
        Assert.IsTrue(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Boom, blocking, 1));
        Assert.IsTrue(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Boom, blocking, 2));
        Assert.IsTrue(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Boom, blocking, 3));
        Assert.IsFalse(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Boom, blocking, 4));

        Assert.IsTrue(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Mbf, blocking, 229));
        Assert.IsFalse(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Mbf, blocking, 230));
        Assert.IsFalse(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Mbf, other, 229));
        Assert.IsTrue(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Mbf, other, 230));

        Assert.AreEqual(
            BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(GameCompatibility.Mbf, blocking | other, 230),
            BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(GameCompatibility.Mbf21, blocking | other, 230));
    }

    [TestMethod]
    public void GeneralizedDoorLightingUsesTagOnlyForManualPushTriggers()
    {
        const int tag = 30000;

        Assert.AreEqual(0, BoomDoorCompatibility.GetGeneralizedDoorLightTag(
            GameCompatibility.Vanilla, BoomTriggerType.PushOnce, tag));
        Assert.AreEqual(tag, BoomDoorCompatibility.GetGeneralizedDoorLightTag(
            GameCompatibility.Boom, BoomTriggerType.PushOnce, tag));
        Assert.AreEqual(tag, BoomDoorCompatibility.GetGeneralizedDoorLightTag(
            GameCompatibility.Boom, BoomTriggerType.PushRepeat, tag));
        Assert.AreEqual(0, BoomDoorCompatibility.GetGeneralizedDoorLightTag(
            GameCompatibility.Boom, BoomTriggerType.SwitchRepeat, tag));
        Assert.AreEqual(0, BoomDoorCompatibility.GetGeneralizedDoorLightTag(
            GameCompatibility.Boom, BoomTriggerType.WalkRepeat, tag));
        Assert.AreEqual(0, BoomDoorCompatibility.GetGeneralizedDoorLightTag(
            GameCompatibility.Boom, BoomTriggerType.PushRepeat, 0));
        Assert.AreEqual(tag, BoomDoorCompatibility.GetGeneralizedDoorLightTag(
            GameCompatibility.Mbf21, BoomTriggerType.PushRepeat, tag));
    }

    [TestMethod]
    public void ClassicManualDoorStoresLightingTagOnlyAtBoomCompatibility()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var vanillaWorld = CreateWorld(content, GameCompatibility.Vanilla);
        var vanillaLine = FindUsableTwoSidedLine(vanillaWorld);
        vanillaLine.Special = (LineSpecial)1;
        vanillaLine.Tag = 30000;
        vanillaWorld.SectorAction.DoLocalDoor(vanillaLine, vanillaWorld.ConsolePlayer.Mobj);
        var vanillaDoor = vanillaLine.BackSector.SpecialData as VerticalDoor;
        Assert.IsNotNull(vanillaDoor);
        Assert.AreEqual(0, vanillaDoor.LightTag);

        var boomWorld = CreateWorld(content, GameCompatibility.Boom);
        var boomLine = FindUsableTwoSidedLine(boomWorld);
        boomLine.Special = (LineSpecial)1;
        boomLine.Tag = 30000;
        boomWorld.SectorAction.DoLocalDoor(boomLine, boomWorld.ConsolePlayer.Mobj);
        var boomDoor = boomLine.BackSector.SpecialData as VerticalDoor;
        Assert.IsNotNull(boomDoor);
        Assert.AreEqual(30000, boomDoor.LightTag);
        Assert.AreSame(boomLine, boomDoor.LightLine);
    }

    [TestMethod]
    public void GeneralizedPushDoorStoresLightTagButTaggedSwitchDoorDoesNot()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);
        var line = FindUsableTwoSidedLine(world);
        var pushSector = line.BackSector;

        line.Tag = 30000;
        var pushSpec = new BoomDoorSpecial(
            new BoomActionSpecification(BoomTriggerType.PushRepeat, BoomActionSpeed.Normal, true),
            BoomDoorKind.OpenWaitClose,
            150);

        Assert.IsTrue(world.SectorAction.DoBoomDoor(line, pushSpec));
        var pushDoor = pushSector.SpecialData as VerticalDoor;
        Assert.IsNotNull(pushDoor);
        Assert.AreEqual(30000, pushDoor.LightTag);
        Assert.AreSame(line, pushDoor.LightLine);

        var switchWorld = CreateWorld(content, GameCompatibility.Boom);
        var switchLine = FindUsableTwoSidedLine(switchWorld);
        var switchSector = switchLine.FrontSector;

        foreach (var sector in switchWorld.Map.Sectors)
            sector.Tag = 0;

        switchLine.Tag = 30000;
        switchSector.Tag = 30000;
        switchWorld.Map.BoomTags.Rebuild();

        var switchSpec = new BoomDoorSpecial(
            new BoomActionSpecification(BoomTriggerType.SwitchRepeat, BoomActionSpeed.Normal, true),
            BoomDoorKind.OpenWaitClose,
            150);

        Assert.IsTrue(switchWorld.SectorAction.DoBoomDoor(switchLine, switchSpec));
        var switchDoor = switchSector.SpecialData as VerticalDoor;
        Assert.IsNotNull(switchDoor);
        Assert.AreEqual(0, switchDoor.LightTag);
    }

    [TestMethod]
    public void LightTurnOnPartwayInterpolatesAndClampsNeighborExtremes()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);
        var setup = PrepareLightingSector(world);

        setup.Sector.LightLevel = 100;
        world.SectorAction.LightTurnOnPartway(setup.Line, Fixed.FromDouble(0.25));
        Assert.AreEqual(80, setup.Sector.LightLevel);

        world.SectorAction.LightTurnOnPartway(setup.Line, Fixed.FromInt(-1));
        Assert.AreEqual(32, setup.Sector.LightLevel);

        world.SectorAction.LightTurnOnPartway(setup.Line, Fixed.FromInt(2));
        Assert.AreEqual(224, setup.Sector.LightLevel);
    }

    [TestMethod]
    public void BoomUpdatesDoorLightingAtEndpointWhileMbfInterpolatesDuringMovement()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var boomWorld = CreateWorld(content, GameCompatibility.Boom);
        var boomSetup = PrepareLightingSector(boomWorld, requireEmptySector: true);
        var boomDoor = CreateLightingDoor(boomWorld, boomSetup.Sector, boomSetup.Line, Fixed.FromInt(2));

        boomDoor.Run();

        Assert.AreEqual(Fixed.FromInt(66).Data, boomSetup.Sector.CeilingHeight.Data);
        Assert.AreEqual(100, boomSetup.Sector.LightLevel);

        var endpointWorld = CreateWorld(content, GameCompatibility.Boom);
        var endpointSetup = PrepareLightingSector(endpointWorld, requireEmptySector: true);
        var endpointDoor = CreateLightingDoor(endpointWorld, endpointSetup.Sector, endpointSetup.Line, Fixed.FromInt(65));

        endpointDoor.Run();

        Assert.AreEqual(Fixed.FromInt(128).Data, endpointSetup.Sector.CeilingHeight.Data);
        Assert.AreEqual(224, endpointSetup.Sector.LightLevel);

        var mbfWorld = CreateWorld(content, GameCompatibility.Mbf);
        var mbfSetup = PrepareLightingSector(mbfWorld, requireEmptySector: true);
        var mbfDoor = CreateLightingDoor(mbfWorld, mbfSetup.Sector, mbfSetup.Line, Fixed.FromInt(2));

        mbfDoor.Run();

        Assert.AreEqual(Fixed.FromInt(66).Data, mbfSetup.Sector.CeilingHeight.Data);
        Assert.AreEqual(131, mbfSetup.Sector.LightLevel);
    }

    private static World CreateWorld(GameContent content, GameCompatibility compatibility) =>
        new(content, new GameOptions { Compatibility = compatibility }, null);

    private static LineDef FindUsableTwoSidedLine(World world) =>
        world.Map.Lines.First(line =>
            line.FrontSector != null &&
            line.BackSector != null &&
            line.FrontSector != line.BackSector &&
            line.BackSector.SpecialData == null);

    private static (Sector Sector, LineDef Line) PrepareLightingSector(
        World world,
        bool requireEmptySector = false)
    {
        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        var sector = world.Map.Sectors.First(candidate =>
        {
            if (requireEmptySector && (candidate.ThingList != null || candidate.TouchingThingList != null))
                return false;

            var neighbors = candidate.Lines
                .Select(line => line.FrontSector == candidate ? line.BackSector : line.FrontSector)
                .Where(other => other != null && other != candidate)
                .Distinct()
                .ToArray();

            return neighbors.Length >= 2 && candidate.SpecialData == null;
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
        LineDef line,
        Fixed speed)
    {
        sector.FloorHeight = Fixed.Zero;
        sector.CeilingHeight = Fixed.FromInt(64);

        var door = new VerticalDoor(world)
        {
            Sector = sector,
            Type = VerticalDoorType.Open,
            Direction = 1,
            Speed = speed,
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
