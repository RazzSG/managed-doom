using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomPlatformSpecialTest
{
    // Phase 4.24 platform regression tests — test-fix v2.
    [TestMethod]
    public void TranslatorMapsAllExtendedPlatformActions()
    {
        AssertPlatform(143, BoomPlatformAction.RaiseAndChange, BoomTriggerType.WalkOnce, 24, false);
        AssertPlatform(144, BoomPlatformAction.RaiseAndChange, BoomTriggerType.WalkOnce, 32, false);
        AssertPlatform(148, BoomPlatformAction.RaiseAndChange, BoomTriggerType.WalkRepeat, 24, true);
        AssertPlatform(149, BoomPlatformAction.RaiseAndChange, BoomTriggerType.WalkRepeat, 32, true);
        AssertPlatform(162, BoomPlatformAction.Perpetual, BoomTriggerType.SwitchOnce, 0, false);
        AssertPlatform(163, BoomPlatformAction.Stop, BoomTriggerType.SwitchOnce, 0, false);
        AssertPlatform(181, BoomPlatformAction.Perpetual, BoomTriggerType.SwitchRepeat, 0, true);
        AssertPlatform(182, BoomPlatformAction.Stop, BoomTriggerType.SwitchRepeat, 0, true);
        AssertPlatform(211, BoomPlatformAction.Toggle, BoomTriggerType.SwitchRepeat, 0, true);
        AssertPlatform(212, BoomPlatformAction.Toggle, BoomTriggerType.WalkRepeat, 0, true);

        Assert.IsFalse(BoomPlatformTranslator.TryTranslate((LineSpecial)142, out _));
        Assert.IsFalse(BoomPlatformTranslator.TryTranslate((LineSpecial)145, out _));
        Assert.IsFalse(BoomPlatformTranslator.TryTranslate((LineSpecial)147, out _));
        Assert.IsFalse(BoomPlatformTranslator.TryTranslate((LineSpecial)150, out _));
        Assert.IsFalse(BoomPlatformTranslator.TryTranslate((LineSpecial)210, out _));
        Assert.IsFalse(BoomPlatformTranslator.TryTranslate((LineSpecial)213, out _));
    }

    [TestMethod]
    public void WalkOnceRaise24UsesPlatformMoverAndConsumesOnSuccess()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world);
        var startHeight = sector.FloorHeight;
        var sourceFlat = line.FrontSide.Sector.FloorFlat;

        sector.Special = (SectorSpecial)7;
        line.Special = (LineSpecial)143;

        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 0, world.ConsolePlayer.Mobj));
        Assert.AreEqual(0, (int)line.Special);

        var platform = sector.SpecialData as Platform;
        Assert.IsNotNull(platform);
        Assert.AreEqual(PlatformType.RaiseAndChange, platform.Type);
        Assert.AreEqual(PlatformState.Up, platform.Status);
        Assert.AreEqual((startHeight + Fixed.FromInt(24)).Data, platform.High.Data);
        Assert.AreEqual((Fixed.One / 2).Data, platform.Speed.Data);
        Assert.AreEqual(sourceFlat, sector.FloorFlat);
        Assert.AreEqual((SectorSpecial)7, sector.Special);
    }

    [TestMethod]
    public void WalkRepeatRaise32KeepsLineAndSectorSpecial()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world);
        var startHeight = sector.FloorHeight;

        sector.Special = (SectorSpecial)5;
        line.Special = (LineSpecial)149;

        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 1, world.ConsolePlayer.Mobj));
        Assert.AreEqual(149, (int)line.Special);

        var platform = sector.SpecialData as Platform;
        Assert.IsNotNull(platform);
        Assert.AreEqual((startHeight + Fixed.FromInt(32)).Data, platform.High.Data);
        Assert.AreEqual((SectorSpecial)5, sector.Special);
    }

    [TestMethod]
    public void PerpetualPlatformUsesBoomBoundsAndRestartsFromStasis()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world);

        // Make the surrounding floor bounds deterministic. PerpetualRaise uses the
        // lowest and highest surrounding floors, not an arbitrary first neighbor.
        sector.FloorHeight = Fixed.FromInt(64);
        var neighborCount = 0;
        foreach (var candidate in sector.Lines)
        {
            var neighbor = GetOtherSector(candidate, sector);
            if (neighbor == null)
                continue;

            neighbor.FloorHeight = Fixed.FromInt(32);
            neighborCount++;
        }
        Assert.IsTrue(neighborCount > 0);

        line.Special = (LineSpecial)181;

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var started));
        Assert.IsTrue(started);

        var platform = sector.SpecialData as Platform;
        Assert.IsNotNull(platform);
        Assert.AreEqual(PlatformType.PerpetualRaise, platform.Type);
        Assert.AreEqual(Fixed.FromInt(32).Data, platform.Low.Data);
        Assert.AreEqual(Fixed.FromInt(64).Data, platform.High.Data);

        line.Special = (LineSpecial)182;
        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var stopped));
        Assert.IsTrue(stopped);
        Assert.AreEqual(PlatformState.InStasis, platform.Status);
        Assert.AreEqual(ThinkerState.InStasis, platform.ThinkerState);
        Assert.AreEqual(182, (int)line.Special);

        line.Special = (LineSpecial)181;
        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var restarted));
        Assert.IsTrue(restarted);
        Assert.AreNotEqual(PlatformState.InStasis, platform.Status);
        Assert.AreEqual(ThinkerState.Active, platform.ThinkerState);
        Assert.AreEqual(181, (int)line.Special);
    }

    [TestMethod]
    public void InstantToggleMovesToCeilingAndBackThenWaitsInStasis()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world, requireEmptySector: true);

        sector.FloorHeight = Fixed.FromInt(0);
        sector.CeilingHeight = Fixed.FromInt(32);

        // The helper selects a sector with neither resident nor radius-touching
        // things. Boom P_CheckSector uses TouchingThingList rather than BlockBox,
        // so mutating BlockBox here would no longer isolate the mover.

        line.Special = (LineSpecial)211;

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var firstUse));
        Assert.IsTrue(firstUse);

        var platform = sector.SpecialData as Platform;
        Assert.IsNotNull(platform);
        Assert.AreEqual(PlatformType.ToggleUpDown, platform.Type);
        Assert.AreEqual(Fixed.FromInt(32).Data, platform.Low.Data);
        Assert.AreEqual(Fixed.FromInt(0).Data, platform.High.Data);
        Assert.IsTrue(platform.Crush);
        Assert.AreEqual(PlatformState.Down, platform.Status);

        platform.Run();

        Assert.AreEqual(Fixed.FromInt(32).Data, sector.FloorHeight.Data);
        Assert.AreEqual(PlatformState.InStasis, platform.Status);
        Assert.AreEqual(PlatformState.Down, platform.OldStatus);
        Assert.AreEqual(ThinkerState.InStasis, platform.ThinkerState);

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var secondUse));
        Assert.IsTrue(secondUse);
        Assert.AreEqual(PlatformState.Up, platform.Status);
        Assert.AreEqual(ThinkerState.Active, platform.ThinkerState);

        platform.Run();

        Assert.AreEqual(Fixed.FromInt(0).Data, sector.FloorHeight.Data);
        Assert.AreEqual(PlatformState.InStasis, platform.Status);
        Assert.AreEqual(PlatformState.Up, platform.OldStatus);
        Assert.AreEqual(211, (int)line.Special);
    }

    [TestMethod]
    public void Special13323RunsLowestNeighborFloorLiftCycleByItself()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world, requireEmptySector: true);

        sector.FloorHeight = Fixed.FromInt(128);
        sector.CeilingHeight = Fixed.FromInt(128);

        var neighborCount = 0;
        foreach (var candidate in sector.Lines)
        {
            var neighbor = GetOtherSector(candidate, sector);
            if (neighbor == null)
                continue;

            neighbor.FloorHeight = Fixed.Zero;
            neighborCount++;
        }
        Assert.IsTrue(neighborCount > 0);

        const int specialValue = 13323;
        var specification = BoomLiftTranslator.Translate((LineSpecial)specialValue);
        Assert.AreEqual(BoomTriggerType.SwitchRepeat, specification.Trigger);
        Assert.AreEqual(BoomActionSpeed.Normal, specification.Speed);
        Assert.AreEqual(BoomLiftTarget.LowestNeighborFloor, specification.Target);
        Assert.AreEqual(35, specification.WaitTics);
        Assert.IsFalse(specification.AllowsMonsters);

        line.Special = (LineSpecial)specialValue;

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var started));
        Assert.IsTrue(started);
        Assert.AreEqual(specialValue, (int)line.Special);

        var platform = sector.FloorData as Platform;
        Assert.IsNotNull(platform);
        Assert.AreEqual(PlatformType.GeneralizedLift, platform.Type);
        Assert.AreEqual(PlatformState.Down, platform.Status);
        Assert.AreEqual(Fixed.Zero.Data, platform.Low.Data);
        Assert.AreEqual(Fixed.FromInt(128).Data, platform.High.Data);
        Assert.AreEqual(Fixed.FromInt(4).Data, platform.Speed.Data);
        Assert.AreEqual(35, platform.Wait);

        platform.Run();

        Assert.AreEqual(Fixed.FromInt(124).Data, sector.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(128).Data, sector.CeilingHeight.Data);
        Assert.AreEqual(PlatformState.Down, platform.Status);

        // 31 more four-unit steps reach the low destination exactly. The mover
        // reports PastDestination on the following tic, matching Doom platform
        // semantics where reaching the exact height and changing state are separate.
        for (var i = 0; i < 31; i++)
            platform.Run();

        Assert.AreEqual(Fixed.Zero.Data, sector.FloorHeight.Data);
        Assert.AreEqual(PlatformState.Down, platform.Status);

        platform.Run();
        Assert.AreEqual(PlatformState.Waiting, platform.Status);
        Assert.AreEqual(35, platform.Count);

        for (var i = 0; i < 35; i++)
            platform.Run();

        Assert.AreEqual(PlatformState.Up, platform.Status);
        Assert.AreEqual(Fixed.Zero.Data, sector.FloorHeight.Data);

        for (var i = 0; i < 32; i++)
            platform.Run();

        Assert.AreEqual(Fixed.FromInt(128).Data, sector.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(128).Data, sector.CeilingHeight.Data);
        Assert.AreEqual(PlatformState.Up, platform.Status);

        platform.Run();

        Assert.IsNull(sector.FloorData);
        Assert.AreEqual(Fixed.FromInt(128).Data, sector.FloorHeight.Data);
        Assert.AreEqual(specialValue, (int)line.Special);
    }

    [TestMethod]
    public void Special13323And16771RunTogetherWithoutCrossingPlanes()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world, requireEmptySector: true);

        // Reproduce the relevant boomphysics.WAD sector geometry.
        sector.FloorHeight = Fixed.FromInt(128);
        sector.CeilingHeight = Fixed.FromInt(128);

        var neighborCount = 0;
        foreach (var candidate in sector.Lines)
        {
            var neighbor = GetOtherSector(candidate, sector);
            if (neighbor == null)
                continue;

            neighbor.FloorHeight = Fixed.Zero;
            neighbor.CeilingHeight = Fixed.FromInt(128);
            neighborCount++;
        }
        Assert.IsTrue(neighborCount > 0);

        // Start 13323 first. Let it reach the low point, wait, and begin
        // returning upward so FloorData remains occupied by the live platform.
        line.Special = (LineSpecial)13323;
        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var liftStarted));
        Assert.IsTrue(liftStarted);

        var platform = sector.FloorData as Platform;
        Assert.IsNotNull(platform);

        for (var i = 0; i < 32; i++)
            platform.Run();

        Assert.AreEqual(Fixed.Zero.Data, sector.FloorHeight.Data);
        platform.Run();
        Assert.AreEqual(PlatformState.Waiting, platform.Status);

        for (var i = 0; i < 35; i++)
            platform.Run();

        Assert.AreEqual(PlatformState.Up, platform.Status);

        platform.Run();
        platform.Run();
        Assert.AreEqual(Fixed.FromInt(8).Data, sector.FloorHeight.Data);
        Assert.AreSame(platform, sector.FloorData);
        Assert.IsNull(sector.CeilingData);

        // Activate 16771 while the lift is already moving upward. Boom allows
        // independent floor and ceiling thinkers in the same sector.
        line.Special = (LineSpecial)16771;
        var ceilingSpecification = BoomCeilingTranslator.Translate(line.Special);
        Assert.IsTrue(world.SectorAction.DoBoomCeiling(line, ceilingSpecification));

        var ceiling = sector.CeilingData as CeilingMove;
        Assert.IsNotNull(ceiling);
        Assert.AreSame(platform, sector.FloorData);
        Assert.AreSame(ceiling, sector.CeilingData);
        Assert.AreNotSame<object>(platform, ceiling);
        Assert.AreEqual(Fixed.Zero.Data, ceiling.BottomHeight.Data);

        // The platform was created first, so run it first on each simulated tic,
        // then the ceiling. This mirrors their thinker insertion order. PrBoom's
        // T_MovePlane effective-destination rule must prevent either plane from
        // crossing the other after each individual move.
        var ticks = 0;
        while ((sector.FloorData != null || sector.CeilingData != null) && ticks < 64)
        {
            if (ReferenceEquals(sector.FloorData, platform))
            {
                platform.Run();
                Assert.IsTrue(sector.FloorHeight <= sector.CeilingHeight,
                    $"Floor crossed ceiling after platform step at simulated tic {ticks}: " +
                    $"floor={sector.FloorHeight.Data}, ceiling={sector.CeilingHeight.Data}.");
            }

            if (ReferenceEquals(sector.CeilingData, ceiling))
            {
                ceiling.Run();
                Assert.IsTrue(sector.FloorHeight <= sector.CeilingHeight,
                    $"Ceiling crossed floor after ceiling step at simulated tic {ticks}: " +
                    $"floor={sector.FloorHeight.Data}, ceiling={sector.CeilingHeight.Data}.");
            }

            ticks++;
        }

        Assert.IsTrue(ticks < 64, "Both plane movers should finish after meeting.");
        Assert.IsNull(sector.FloorData);
        Assert.IsNull(sector.CeilingData);
        Assert.AreEqual(Fixed.FromInt(104).Data, sector.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(104).Data, sector.CeilingHeight.Data);
    }

    [TestMethod]
    public void ExtendedPlatformsRequirePlayerAndNonZeroTag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world);
        var nonPlayer = new Mobj(world);

        line.Special = (LineSpecial)212;
        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 0, nonPlayer));
        Assert.IsNull(sector.SpecialData);
        Assert.AreEqual(212, (int)line.Special);

        line.Tag = 0;
        line.Special = (LineSpecial)143;
        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 0, world.ConsolePlayer.Mobj));
        Assert.IsNull(sector.SpecialData);
        Assert.AreEqual(143, (int)line.Special);
    }

    private static void AssertPlatform(
        int special,
        BoomPlatformAction action,
        BoomTriggerType trigger,
        int amount,
        bool repeatable)
    {
        var specification = BoomPlatformTranslator.Translate((LineSpecial)special);
        Assert.AreEqual(action, specification.Action);
        Assert.AreEqual(trigger, specification.Trigger);
        Assert.AreEqual(amount, specification.Amount);
        Assert.AreEqual(repeatable, specification.Repeatable);
    }

    private static World CreateBoomWorld(GameContent content)
    {
        return new World(content, new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = GameCompatibility.Boom
        }, null);
    }

    private static (LineDef line, Sector sector) PrepareTaggedSector(World world, bool requireEmptySector = false)
    {
        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        var sector = world.Map.Sectors.First(s =>
            s.SpecialData == null &&
            (!requireEmptySector ||
                (s.ThingList == null && s.TouchingThingList == null)) &&
            s.CeilingHeight > s.FloorHeight &&
            s.Lines.Any(l => GetOtherSector(l, s) != null));

        var line = world.Map.Lines.First(l => l.FrontSide != null && l.FrontSide.Sector != null);
        const int tag = 30000;
        line.Tag = tag;
        sector.Tag = tag;
        world.Map.BoomTags.Rebuild();

        return (line, sector);
    }

    private static Sector GetOtherSector(LineDef line, Sector sector)
    {
        if (line.FrontSector == sector)
            return line.BackSector;

        if (line.BackSector == sector)
            return line.FrontSector;

        return null;
    }
}
