using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using ManagedDoom.Compatibility.Boom.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManagedDoom.Compatibility.Detection;

namespace ManagedDoomTest.CompatibilityTests;

/// <summary>
/// Phase 4.69 external-reference verification against Jim Flynn's BOOMEDIT.WAD.
/// The reference WAD is intentionally not redistributed with this repository.
/// Put a local copy in data/reference/BOOMEDIT.WAD or set
/// MANAGEDDOOM_BOOMEDIT_WAD to its full path.
/// </summary>
[TestClass]
public sealed class BoomOfficialReferenceMapTest
{
    private const string EnvironmentVariable = "MANAGEDDOOM_BOOMEDIT_WAD";
    private const string ReferenceFileName = "BOOMEDIT.WAD";

    [TestMethod]
    [TestCategory("ExternalReference")]
    public void BoomEditExercisesOfficialBoomFeatureFingerprintThroughRealMapLoad()
    {
        var boomEditPath = RequireBoomEditPath();

        // Use the real runtime content pipeline here. BOOMEDIT.WAD has its own
        // flat namespace markers, which the intentionally simplified
        // DummyFlatLookup does not merge with the IWAD flat namespace.
        // The production FlatLookup does, matching the way the WAD is loaded
        // during an actual game launch.
        using var content = CreateRuntimeContent(boomEditPath);

        Assert.AreNotEqual(-1, content.Flats.GetNumber("F_SKY1"),
            "The runtime flat loader should preserve the IWAD sky flat while merging BOOMEDIT's flat namespace.");

        Assert.IsTrue(
            CompatibilityFeatureScanner.TryDetect(content.Wad, out var detectedCompatibility),
            "BOOMEDIT.WAD should be detected as an extended-compatibility PWAD.");
        Assert.AreEqual(
            GameCompatibility.Boom,
            detectedCompatibility,
            "The historical BOOMEDIT.WAD is a Boom 2.x reference map, not an MBF/MBF21 map.");

        AssertReferenceResourceLumps(content);

        var harness = CreateBoomGame(content);
        var world = harness.Game.World;
        var map = world.Map;

        Assert.IsTrue(map.Lines.Any(IsGeneralizedLine),
            "BOOMEDIT.WAD should contain generalized linedefs.");
        Assert.IsTrue(map.Lines.Any(IsBoomExtendedLine),
            "BOOMEDIT.WAD should contain extended Boom linedefs.");
        Assert.IsTrue(map.Lines.Any(line => (line.Flags & LineFlags.PassThru) != 0),
            "BOOMEDIT.WAD should exercise the Boom PassThru linedef flag.");
        Assert.IsTrue(map.Sectors.Any(IsGeneralizedSector),
            "BOOMEDIT.WAD should contain generalized sector bits.");
        Assert.IsTrue(map.Things.Any(thing => thing.Type is 5001 or 5002),
            "BOOMEDIT.WAD should contain a Boom point wind/current source thing.");

        Assert.IsTrue(map.Lines.Any(line => (int)line.Special == 223),
            "BOOMEDIT.WAD's ice/sludge demonstration should exercise Boom friction.");
        Assert.IsTrue(map.Lines.Any(line => (int)line.Special == 242),
            "BOOMEDIT.WAD's deep-water/colormap demonstration should exercise transfer heights.");
        Assert.IsTrue(map.Lines.Any(line => (int)line.Special == BoomTranslucentLineResolver.Special),
            "BOOMEDIT.WAD should exercise linedef 260 translucency.");

        AssertResolvedCustomTranslucency(content, map);

        // Run the actual game loop long enough for startup-created Boom thinkers
        // (scrollers, pushers, lighting, animations, etc.) to receive many ticks.
        // BOOMEDIT has no monsters, so this is deterministic and inexpensive.
        for (var i = 0; i < 1024; i++)
            harness.Game.Update(harness.Commands);

        Assert.IsNotNull(harness.Game.World);
        Assert.IsNotNull(harness.Game.World.Map);
        Assert.AreEqual(1, harness.Game.World.Options.Map);
    }

    [TestMethod]
    [TestCategory("ExternalReference")]
    public void BoomEditPassThruMultipleSwitchRunsFloorAndCeilingActionsTogether()
    {
        var boomEditPath = RequireBoomEditPath();
        using var content = CreateRuntimeContent(boomEditPath);
        var harness = CreateBoomGame(content);
        var world = harness.Game.World;
        var map = world.Map;

        Assert.IsTrue(map.Lines.Length > 626,
            "The official BOOMEDIT MAP01 linedef layout is required for this reference check.");

        var liftLine = map.Lines[258];
        var crusherLine = map.Lines[626];

        Assert.AreEqual(0x371A, (int)liftLine.Special);
        Assert.AreEqual(37, liftLine.Tag);
        Assert.IsTrue((liftLine.Flags & LineFlags.PassThru) != 0);
        Assert.AreEqual(0x2FDA, (int)crusherLine.Special);
        Assert.AreEqual(37, crusherLine.Tag);

        var target = map.Sectors.Single(sector => sector.Tag == 37);
        Assert.IsNull(target.FloorData);
        Assert.IsNull(target.CeilingData);

        Assert.IsTrue(BoomLineSpecials.TryUse(
            world, liftLine, 0, world.ConsolePlayer.Mobj, out var liftResult));
        Assert.IsTrue(liftResult);
        Assert.IsNotNull(target.FloorData as Platform);

        Assert.IsTrue(BoomLineSpecials.TryUse(
            world, crusherLine, 0, world.ConsolePlayer.Mobj, out var crusherResult));
        Assert.IsTrue(crusherResult);

        Assert.IsNotNull(target.FloorData as Platform,
            "BOOMEDIT line 258 should keep the floor lift active.");
        Assert.IsNotNull(target.CeilingData as CeilingMove,
            "BOOMEDIT line 626 should start its ceiling crusher even while the floor lift is active.");
    }

    private static void AssertReferenceResourceLumps(GameContent content)
    {
        var wad = content.Wad;

        Assert.AreNotEqual(-1, wad.GetLumpNumber("MAP01"));
        Assert.AreNotEqual(-1, wad.GetLumpNumber("SWITCHES"),
            "BOOMEDIT.WAD demonstrates the Boom SWITCHES resource table.");
        Assert.AreNotEqual(-1, wad.GetLumpNumber("ANIMATED"),
            "BOOMEDIT.WAD demonstrates the Boom ANIMATED resource table.");
        Assert.AreNotEqual(-1, wad.GetLumpNumber("C_START"),
            "BOOMEDIT.WAD contains custom colormaps between C_START/C_END markers.");
        Assert.AreNotEqual(-1, wad.GetLumpNumber("C_END"));

        Assert.IsTrue(content.BoomSwitches.HasCustomTable,
            "The BOOMEDIT SWITCHES lump should be parsed instead of falling back to the built-in table.");
        Assert.IsTrue(content.BoomAnimated.HasCustomTable,
            "The BOOMEDIT ANIMATED lump should be parsed instead of falling back to vanilla animations.");
    }

    private static void AssertResolvedCustomTranslucency(GameContent content, Map map)
    {
        var selector = map.Lines
            .Select(line => line.TranslucencyMapName)
            .FirstOrDefault(name =>
                !string.IsNullOrWhiteSpace(name) &&
                !string.Equals(name, BoomTranslucentLineResolver.DefaultMapName, StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(selector,
            "BOOMEDIT.WAD should resolve its documented custom translucency filter through linedef 260.");
        Assert.IsTrue(content.BoomTranslucencyMaps.TryGetCustomMap(selector, out var table),
            $"The custom translucency selector '{selector}' should resolve to a 64K lookup table.");
        Assert.AreEqual(BoomTranslucencyMapLookup.TableSize, table.Length);
    }

    private static GameContent CreateRuntimeContent(string boomEditPath)
    {
        var args = new CommandLineArgs(new[]
        {
            "-iwad", WadPath.Doom2,
            "-file", boomEditPath,
            // BOOMEDIT does not need a DeHackEd patch for this reference test.
            // Disabling it also keeps this external test from mutating shared
            // static DoomInfo state if a different local copy adds one.
            "-nodeh"
        });

        return new GameContent(args);
    }

    private static (DoomGame Game, TicCmd[] Commands) CreateBoomGame(GameContent content)
    {
        var options = new GameOptions
        {
            Compatibility = GameCompatibility.Boom,
            GameVersion = GameVersion.Version109,
            GameMode = GameMode.Commercial,
            Map = 1,
            NoMonsters = true
        };

        var game = new DoomGame(content, options);
        var commands = CreateCommands();
        game.DeferedInitNew();
        game.Update(commands);
        return (game, commands);
    }

    private static TicCmd[] CreateCommands()
    {
        var commands = new TicCmd[Player.MaxPlayerCount];
        for (var i = 0; i < commands.Length; i++)
            commands[i] = new TicCmd();

        return commands;
    }

    private static bool IsGeneralizedLine(LineDef line)
    {
        var special = (int)line.Special;
        return special is >= 0x2F80 and <= 0x7FFF;
    }

    private static bool IsBoomExtendedLine(LineDef line)
    {
        var special = (int)line.Special;
        return special is 78 or 85 || special is >= 142 and <= 269;
    }

    private static bool IsGeneralizedSector(Sector sector)
    {
        return ((int)sector.Special & 0x0FE0) != 0;
    }

    private static string RequireBoomEditPath()
    {
        foreach (var path in GetBoomEditCandidates())
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                return Path.GetFullPath(path);
        }

        Assert.Inconclusive(
            "Phase 4.69 external reference test skipped: BOOMEDIT.WAD was not found. " +
            $"Set {EnvironmentVariable} to your local BOOMEDIT.WAD, or place it at " +
            "ManagedDoomTest/data/reference/BOOMEDIT.WAD. The file is intentionally not redistributed by this project.");
        return null;
    }

    private static IEnumerable<string> GetBoomEditCandidates()
    {
        yield return Environment.GetEnvironmentVariable(EnvironmentVariable);
        yield return Path.Combine("data", "reference", ReferenceFileName);
        yield return Path.Combine(AppContext.BaseDirectory, "data", "reference", ReferenceFileName);
        yield return Path.Combine(AppContext.BaseDirectory, ReferenceFileName);
    }
}
