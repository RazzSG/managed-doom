using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ManagedDoom;
using ManagedDoom.Compatibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManagedDoom.Compatibility.Detection;

namespace ManagedDoomTest.CompatibilityTests;

/// <summary>
/// Phase 4.70 external-corpus verification against multiple real Boom PWADs.
/// The WADs are intentionally not redistributed with this repository.
/// Configure a local manifest at data/reference/boom-corpus.json or set
/// MANAGEDDOOM_BOOM_CORPUS_MANIFEST to its full path.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class BoomRealWadCorpusTest
{
    private const string EnvironmentVariable = "MANAGEDDOOM_BOOM_CORPUS_MANIFEST";
    private const string ManifestFileName = "boom-corpus.json";
    private const int MinimumCorpusEntries = 3;
    private const int MinimumMapsPerEntry = 2;
    private const int DefaultTicksPerMap = 256;
    private const int MaximumTicksPerMap = 4096;

    [TestMethod]
    [TestCategory("ExternalReference")]
    [TestCategory("BoomCorpus")]
    public void RealBoomCorpusLoadsRepresentativeMapsThroughRuntimePipeline()
    {
        var manifestPath = RequireManifestPath();
        var manifest = LoadManifest(manifestPath);
        var manifestDirectory = Path.GetDirectoryName(manifestPath) ?? Directory.GetCurrentDirectory();

        ValidateManifest(manifest);

        var failures = new List<string>();

        foreach (var entry in manifest.Entries)
        {
            try
            {
                RunCorpusEntry(entry, manifestDirectory, manifest.DefaultTicksPerMap);
                Console.WriteLine($"[Boom corpus] {entry.Name}: PASS");
            }
            catch (Exception e)
            {
                failures.Add($"{entry.Name}: {e}");
                Console.WriteLine($"[Boom corpus] {entry.Name}: FAIL - {e.GetType().Name}: {e.Message}");
            }
        }

        if (failures.Count != 0)
        {
            Assert.Fail(
                $"Boom corpus verification completed all {manifest.Entries.Count} entries with {failures.Count} failure(s):" +
                Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine + Environment.NewLine, failures));
        }
    }

    private static void RunCorpusEntry(CorpusEntry entry, string manifestDirectory, int manifestDefaultTicks)
    {
        var iwadPath = ResolveRequiredPath(entry.Iwad, manifestDirectory, entry.Name, "IWAD");
        var pwadPaths = entry.Pwads
            .Select(path => ResolveRequiredPath(path, manifestDirectory, entry.Name, "PWAD"))
            .ToArray();

        var args = CreateRuntimeArgs(iwadPath, pwadPaths);

        using var content = new GameContent(args);
        var detectedOptions = new GameOptions(args, content);

        Assert.AreEqual(
            GameCompatibilityMode.Auto,
            detectedOptions.CompatibilityMode,
            $"{entry.Name}: corpus verification must exercise auto compatibility detection.");
        Assert.IsTrue(
            GameCompatibilityFeatures.SupportsBoom(detectedOptions.Compatibility),
            $"{entry.Name}: the configured corpus entry must resolve to Boom-family compatibility, " +
            $"but auto-detection returned {detectedOptions.Compatibility} from {detectedOptions.CompatibilitySource}.");

        Console.WriteLine(
            $"[Boom corpus] {entry.Name}: auto-detected {detectedOptions.Compatibility} " +
            $"via {detectedOptions.CompatibilitySource}.");
        Assert.IsTrue(
            detectedOptions.CompatibilitySource is CompatibilityDetectionSource.Complvl or CompatibilityDetectionSource.FeatureScan,
            $"{entry.Name}: Boom compatibility should be discovered from COMPLVL or loaded WAD features, not forced by the test.");

        if (detectedOptions.CompatibilitySource == CompatibilityDetectionSource.FeatureScan)
        {
            Assert.IsTrue(
                CompatibilityFeatureScanner.TryDetect(content.Wad, out var scannedCompatibility),
                $"{entry.Name}: the Boom feature scanner should recognize the loaded WAD set.");
            Assert.IsTrue(
                GameCompatibilityFeatures.SupportsBoom(scannedCompatibility),
                $"{entry.Name}: the feature scanner should classify this corpus entry as Boom-family, " +
                $"but returned {scannedCompatibility}.");
        }

        using (var pwadSet = new Wad(pwadPaths))
        {
            foreach (var mapName in entry.Maps)
            {
                var selection = ParseMapSelection(mapName);
                Assert.IsTrue(
                    HasDoomMapMarker(pwadSet, selection.CanonicalName),
                    $"{entry.Name}: representative map {selection.CanonicalName} must come from the configured PWAD set, not the base IWAD.");
            }
        }

        var ticksPerMap = entry.TicksPerMap ?? manifestDefaultTicks;

        foreach (var mapName in entry.Maps)
            RunRepresentativeMap(entry.Name, args, content, mapName, ticksPerMap);
    }

    private static void RunRepresentativeMap(
        string entryName,
        CommandLineArgs args,
        GameContent content,
        string mapName,
        int ticksPerMap)
    {
        var selection = ParseMapSelection(mapName);
        var canonicalMapName = selection.CanonicalName;

        Assert.AreNotEqual(
            -1,
            content.Wad.GetLumpNumber(canonicalMapName),
            $"{entryName}: configured representative map {canonicalMapName} was not found in the merged WAD set.");

        var options = new GameOptions(args, content)
        {
            Episode = selection.Episode,
            Map = selection.Map,
            NoMonsters = true,
            Skill = GameSkill.Medium
        };

        Assert.IsTrue(
            GameCompatibilityFeatures.SupportsBoom(options.Compatibility),
            $"{entryName} {canonicalMapName}: a fresh runtime options object should retain Boom-family auto-detection, " +
            $"but returned {options.Compatibility}.");

        var game = new DoomGame(content, options);
        var commands = CreateCommands();

        game.DeferedInitNew();
        game.Update(commands);

        Assert.IsNotNull(game.World, $"{entryName} {canonicalMapName}: world creation failed.");
        Assert.IsNotNull(game.World.Map, $"{entryName} {canonicalMapName}: map creation failed.");
        Assert.AreEqual(selection.Map, game.World.Options.Map,
            $"{entryName} {canonicalMapName}: DoomGame loaded a different map than requested.");

        if (content.Wad.GameMode != GameMode.Commercial)
        {
            Assert.AreEqual(selection.Episode, game.World.Options.Episode,
                $"{entryName} {canonicalMapName}: DoomGame loaded a different episode than requested.");
        }

        var startGameTic = game.GameTic;

        for (var i = 0; i < ticksPerMap; i++)
            game.Update(commands);

        Assert.IsNotNull(game.World, $"{entryName} {canonicalMapName}: world disappeared while ticking the corpus map.");
        Assert.IsNotNull(game.World.Map, $"{entryName} {canonicalMapName}: map disappeared while ticking the corpus map.");
        Assert.AreEqual(
            startGameTic + ticksPerMap,
            game.GameTic,
            $"{entryName} {canonicalMapName}: the real game loop did not advance for all requested corpus ticks.");
    }

    private static CommandLineArgs CreateRuntimeArgs(string iwadPath, IReadOnlyList<string> pwadPaths)
    {
        var values = new List<string>
        {
            "-iwad", iwadPath,
            "-file"
        };

        values.AddRange(pwadPaths);

        // External corpus WADs may contain embedded DEHACKED/BEX data. Applying
        // those patches mutates shared DoomInfo state, which would make later
        // corpus entries and unrelated tests order-dependent. Phase 4.70 is a
        // Boom map/runtime compatibility corpus, so keep DeHackEd disabled here.
        values.Add("-nodeh");

        return new CommandLineArgs(values.ToArray());
    }

    private static TicCmd[] CreateCommands()
    {
        var commands = new TicCmd[Player.MaxPlayerCount];
        for (var i = 0; i < commands.Length; i++)
            commands[i] = new TicCmd();

        return commands;
    }


    private static bool HasDoomMapMarker(Wad wad, string mapName)
    {
        var lumps = wad.LumpInfos;

        for (var i = lumps.Count - 1; i >= 0; i--)
        {
            if (!string.Equals(lumps[i].Name, mapName, StringComparison.OrdinalIgnoreCase))
                continue;

            var stream = lumps[i].Stream;

            return i + 4 < lumps.Count &&
                   ReferenceEquals(lumps[i + 1].Stream, stream) &&
                   ReferenceEquals(lumps[i + 2].Stream, stream) &&
                   ReferenceEquals(lumps[i + 3].Stream, stream) &&
                   ReferenceEquals(lumps[i + 4].Stream, stream) &&
                   string.Equals(lumps[i + 1].Name, "THINGS", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(lumps[i + 2].Name, "LINEDEFS", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(lumps[i + 3].Name, "SIDEDEFS", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(lumps[i + 4].Name, "VERTEXES", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static MapSelection ParseMapSelection(string value)
    {
        var name = (value ?? string.Empty).Trim().ToUpperInvariant();

        if (name.StartsWith("MAP", StringComparison.Ordinal) &&
            int.TryParse(name.AsSpan(3), out var commercialMap) &&
            commercialMap > 0)
        {
            return new MapSelection(1, commercialMap, $"MAP{commercialMap:00}");
        }

        if (EpisodeCatalog.TryParseMapName(name, out var episode, out var map))
            return new MapSelection(episode, map, $"E{episode}M{map}");

        Assert.Fail($"Invalid corpus map name '{value}'. Use MAP01-style or E1M1-style map markers.");
        throw new InvalidDataException();
    }

    private static CorpusManifest LoadManifest(string manifestPath)
    {
        try
        {
            var json = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<CorpusManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            Assert.IsNotNull(manifest, "Boom corpus manifest was empty.");
            return manifest;
        }
        catch (JsonException e)
        {
            Assert.Fail($"Failed to parse Boom corpus manifest '{manifestPath}': {e.Message}");
            throw;
        }
    }

    private static void ValidateManifest(CorpusManifest manifest)
    {
        Assert.IsNotNull(manifest.Entries, "Boom corpus manifest must contain an 'entries' array.");
        Assert.IsTrue(
            manifest.Entries.Count >= MinimumCorpusEntries,
            $"Phase 4.70 requires at least {MinimumCorpusEntries} independent real Boom WAD entries; " +
            $"the manifest currently contains {manifest.Entries.Count}.");

        Assert.IsTrue(
            manifest.DefaultTicksPerMap is >= 1 and <= MaximumTicksPerMap,
            $"defaultTicksPerMap must be between 1 and {MaximumTicksPerMap}.");

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in manifest.Entries)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Name), "Every Boom corpus entry must have a non-empty name.");
            Assert.IsTrue(names.Add(entry.Name), $"Boom corpus entry name '{entry.Name}' is duplicated.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Iwad), $"{entry.Name}: 'iwad' is required.");
            Assert.IsNotNull(entry.Pwads, $"{entry.Name}: 'pwads' is required.");
            Assert.IsTrue(entry.Pwads.Count > 0, $"{entry.Name}: at least one PWAD is required.");
            Assert.IsFalse(entry.Pwads.Any(string.IsNullOrWhiteSpace), $"{entry.Name}: PWAD paths must not be empty.");
            Assert.IsNotNull(entry.Maps, $"{entry.Name}: 'maps' is required.");
            Assert.IsTrue(
                entry.Maps.Count >= MinimumMapsPerEntry,
                $"{entry.Name}: choose at least {MinimumMapsPerEntry} representative maps for corpus verification.");
            Assert.IsFalse(entry.Maps.Any(string.IsNullOrWhiteSpace), $"{entry.Name}: map names must not be empty.");

            if (entry.TicksPerMap.HasValue)
            {
                Assert.IsTrue(
                    entry.TicksPerMap.Value is >= 1 and <= MaximumTicksPerMap,
                    $"{entry.Name}: ticksPerMap must be between 1 and {MaximumTicksPerMap}.");
            }
        }
    }

    private static string ResolveRequiredPath(string path, string manifestDirectory, string entryName, string kind)
    {
        if (Path.IsPathRooted(path) && File.Exists(path))
            return Path.GetFullPath(path);

        if (File.Exists(path))
            return Path.GetFullPath(path);

        var relativeToManifest = Path.GetFullPath(Path.Combine(manifestDirectory, path));
        if (File.Exists(relativeToManifest))
            return relativeToManifest;

        var projectRoot = FindTestProjectRoot();
        if (projectRoot != null)
        {
            var relativeToProject = Path.GetFullPath(Path.Combine(projectRoot, path));
            if (File.Exists(relativeToProject))
                return relativeToProject;

            var relativeToReferenceDirectory = Path.GetFullPath(
                Path.Combine(projectRoot, "data", "reference", path));
            if (File.Exists(relativeToReferenceDirectory))
                return relativeToReferenceDirectory;
        }

        Assert.Fail(
            $"{entryName}: {kind} '{path}' was not found. Paths may be absolute, relative to the test working " +
            "directory, or relative to the corpus manifest.");
        return null;
    }

    private static string RequireManifestPath()
    {
        foreach (var path in GetManifestCandidates())
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                return Path.GetFullPath(path);
        }

        Assert.Inconclusive(
            "Phase 4.70 external Boom corpus test skipped: boom-corpus.json was not found. " +
            $"Set {EnvironmentVariable} to your local manifest, or copy " +
            "ManagedDoomTest/data/reference/boom-corpus.example.json to boom-corpus.json and point it at your local WADs. " +
            "The external WADs are intentionally not redistributed by this project.");
        return null;
    }

    private static IEnumerable<string> GetManifestCandidates()
    {
        yield return Environment.GetEnvironmentVariable(EnvironmentVariable);
        yield return Path.Combine("data", "reference", ManifestFileName);
        yield return Path.Combine(AppContext.BaseDirectory, "data", "reference", ManifestFileName);
        yield return Path.Combine(AppContext.BaseDirectory, ManifestFileName);

        var projectRoot = FindTestProjectRoot();
        if (projectRoot != null)
            yield return Path.Combine(projectRoot, "data", "reference", ManifestFileName);
    }

    private static string FindTestProjectRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ManagedDoomTest.csproj")))
                return directory.FullName;
        }

        return null;
    }

    public sealed class CorpusManifest
    {
        public int DefaultTicksPerMap { get; set; } = BoomRealWadCorpusTest.DefaultTicksPerMap;
        public List<CorpusEntry> Entries { get; set; } = new();
    }

    public sealed class CorpusEntry
    {
        public string Name { get; set; }
        public string Iwad { get; set; }
        public List<string> Pwads { get; set; } = new();
        public List<string> Maps { get; set; } = new();
        public int? TicksPerMap { get; set; }
    }

    private readonly record struct MapSelection(int Episode, int Map, string CanonicalName);
}
