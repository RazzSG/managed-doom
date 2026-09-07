using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomSimultaneousPlaneMoverTest
{
    [TestMethod]
    public void BoomCanRunPerpetualLiftAndCrusherOnTheSameSector()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = GameCompatibility.Boom
        }, null);

        var sector = world.Map.Sectors.First(s =>
            s.FloorData == null &&
            s.CeilingData == null &&
            s.ThingList == null &&
            s.TouchingThingList == null &&
            GetNeighbors(s).Any());

        var neighbors = GetNeighbors(sector).ToArray();
        Assert.IsTrue(neighbors.Length > 0);

        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        var startFloor = Fixed.FromInt(128);
        var startCeiling = Fixed.FromInt(256);
        sector.FloorHeight = startFloor;
        sector.CeilingHeight = startCeiling;

        foreach (var neighbor in neighbors)
            neighbor.FloorHeight = startFloor;
        neighbors[0].FloorHeight = Fixed.FromInt(64);

        const int tag = 30010;
        sector.Tag = tag;

        var triggerLines = world.Map.Lines
            .Where(line => line.FrontSide != null)
            .Take(2)
            .ToArray();
        Assert.AreEqual(2, triggerLines.Length);

        triggerLines[0].Tag = tag;
        triggerLines[1].Tag = tag;
        world.Map.BoomTags.Rebuild();

        // These are the same generalized specials used by BOOMEDIT.WAD's
        // "SIMULTANEOUS MULTIPLE SWITCH ACTION" demonstration.
        var lift = BoomLiftTranslator.Translate((LineSpecial)0x371A);
        var crusher = BoomCrusherTranslator.Translate((LineSpecial)0x2FDA);

        Assert.IsTrue(world.SectorAction.DoBoomLift(triggerLines[0], lift));
        Assert.IsTrue(world.SectorAction.DoBoomCrusher(triggerLines[1], crusher));

        var platform = sector.FloorData as Platform;
        var ceiling = sector.CeilingData as CeilingMove;

        Assert.IsNotNull(platform);
        Assert.IsNotNull(ceiling);
        Assert.AreNotSame<object>(platform, ceiling);
        Assert.AreEqual(PlatformType.GeneralizedPerpetual, platform.Type);
        Assert.AreEqual(CeilingMoveType.GeneralizedSilentCrusher, ceiling.Type);

        // Force deterministic directions and verify that both planes can actually
        // advance while both thinkers remain attached to the same sector.
        platform.Status = PlatformState.Down;
        ceiling.Direction = -1;

        platform.Run();
        ceiling.Run();

        Assert.IsTrue(sector.FloorHeight < startFloor,
            "The perpetual lift should move the floor while the crusher is active.");
        Assert.IsTrue(sector.CeilingHeight < startCeiling,
            "The crusher should move the ceiling while the lift is active.");
        Assert.AreSame(platform, sector.FloorData);
        Assert.AreSame(ceiling, sector.CeilingData);
    }

    [TestMethod]
    public void VanillaStillTreatsTheOppositePlaneMoverAsBusy()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = GameCompatibility.Vanilla
        }, null);

        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        var sector = world.Map.Sectors.First(s => s.FloorData == null && s.CeilingData == null);
        var line = world.Map.Lines.First(l => l.FrontSide != null);
        const int tag = 30011;
        sector.Tag = tag;
        line.Tag = tag;

        sector.FloorData = new FloorMove(world);

        Assert.IsFalse(world.SectorAction.DoCeiling(line, CeilingMoveType.RaiseToHighest));
        Assert.IsNull(sector.CeilingData,
            "Vanilla must retain its original single reversible-plane-mover lock.");
    }

    private static System.Collections.Generic.IEnumerable<Sector> GetNeighbors(Sector sector)
    {
        return sector.Lines
            .Select(line => line.FrontSector == sector ? line.BackSector : line.FrontSector)
            .Where(other => other != null && other != sector)
            .Distinct();
    }
}
