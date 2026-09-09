using System;
using System.Collections.Generic;
using System.IO;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Detection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

/// <summary>
/// Optional real-WAD MBF loading corpus.
///
/// Normal unit-test runs do not depend on external corpus files. Put
/// MBFEDIT.WAD / MBFEDIT!.WAD beside the test runner, or set
/// MANAGEDDOOM_MBF_WAD_CORPUS to a Path.PathSeparator-separated list.
/// Set MANAGEDDOOM_REQUIRE_MBF_WAD_CORPUS=1 on dedicated corpus machines
/// to turn missing configured files into failures.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MbfWadCorpusTest
{
    private const string CorpusEnvironmentVariable = "MANAGEDDOOM_MBF_WAD_CORPUS";
    private const string RequireCorpusEnvironmentVariable = "MANAGEDDOOM_REQUIRE_MBF_WAD_CORPUS";

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MBF")]
    [TestCategory("WadCompatibility")]
    public void AvailableMbfCorpusWadsLoadEveryClassicMap()
    {
        var candidates = FindCorpusCandidates();
        var requireCorpus = string.Equals(
            Environment.GetEnvironmentVariable(RequireCorpusEnvironmentVariable),
            "1",
            StringComparison.OrdinalIgnoreCase);

        if (candidates.Count == 0)
        {
            if (requireCorpus)
            {
                Assert.Fail(
                    $"No MBF corpus WADs were configured. Set {CorpusEnvironmentVariable} " +
                    "or place MBFEDIT.WAD beside the test runner.");
            }

            Console.WriteLine(
                "MBF CORPUS SKIP: no corpus WADs are available. " +
                $"Set {CorpusEnvironmentVariable} to enable the real-WAD sweep.");
            return;
        }

        var failures = new List<string>();
        var loadedWads = 0;
        var loadedMaps = 0;

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
            {
                var message = $"{path}: file does not exist.";

                if (requireCorpus)
                    failures.Add(message);
                else
                    Console.WriteLine("MBF CORPUS SKIP: " + message);

                continue;
            }

            try
            {
                var args = new CommandLineArgs(new[]
                {
                    "-iwad", WadPath.Doom2,
                    "-file", path,
                    "-compatibility", "mbf",
                    "-nomonsters"
                });

                using var content = new GameContent(args);
                var maps = FindMapsFromTargetWad(content.Wad);

                if (maps.Count == 0)
                {
                    failures.Add($"{path}: no classic Doom map markers were found.");
                    continue;
                }

                var detectionArgs = new CommandLineArgs(new[]
                {
                    "-iwad", WadPath.Doom2,
                    "-file", path
                });
                var detected = CompatibilityDetector.Detect(content.Wad, detectionArgs);
                var caseLoaded = 0;

                foreach (var mapName in maps)
                {
                    try
                    {
                        var options = new GameOptions(args, content)
                        {
                            NoMonsters = true
                        };

                        SelectMap(options, mapName);

                        var world = new World(content, options, null);
                        ValidateMap(world.Map, $"{Path.GetFileName(path)} {mapName}");

                        caseLoaded++;
                        loadedMaps++;
                    }
                    catch (Exception e)
                    {
                        failures.Add(
                            $"{path} {mapName}:{Environment.NewLine}{e}");
                    }
                }

                loadedWads++;

                Console.WriteLine(
                    $"MBF CORPUS: {Path.GetFileName(path)}: " +
                    $"{caseLoaded}/{maps.Count} maps OK; " +
                    $"auto={detected.Compatibility} ({detected.Source})");
            }
            catch (Exception e)
            {
                failures.Add(
                    $"{path}: bootstrap/resource loading failed:" +
                    Environment.NewLine +
                    e);
            }
            finally
            {
                ResetDefinitions();
            }
        }

        if (failures.Count != 0)
        {
            Assert.Fail(
                $"MBF corpus sweep found {failures.Count} failure(s)." +
                Environment.NewLine +
                Environment.NewLine +
                string.Join(
                    Environment.NewLine +
                    Environment.NewLine +
                    "--------------------------------------------------" +
                    Environment.NewLine,
                    failures));
        }

        if (loadedWads == 0)
        {
            if (requireCorpus)
            {
                Assert.Fail("No configured MBF corpus WAD could be loaded.");
            }

            Console.WriteLine("MBF CORPUS SKIP: configured files were unavailable.");
            return;
        }

        Assert.IsTrue(loadedMaps > 0, "The MBF corpus sweep did not load any maps.");
    }

    private static List<string> FindCorpusCandidates()
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var configured = Environment.GetEnvironmentVariable(CorpusEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            foreach (var item in configured.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var path = item.Trim();
                if (path.Length != 0)
                    AddCandidate(result, seen, path);
            }
        }

        AddExistingLocalCandidate(result, seen, "MBFEDIT.WAD");
        AddExistingLocalCandidate(result, seen, "MBFEDIT!.WAD");

        return result;
    }

    private static void AddExistingLocalCandidate(
        ICollection<string> result,
        ISet<string> seen,
        string fileName)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        if (File.Exists(path))
            AddCandidate(result, seen, path);
    }

    private static void AddCandidate(
        ICollection<string> result,
        ISet<string> seen,
        string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (seen.Add(fullPath))
            result.Add(fullPath);
    }

    private static IReadOnlyList<string> FindMapsFromTargetWad(Wad wad)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (wad.LumpInfos.Count == 0)
            return result;

        var targetStream = wad.LumpInfos[^1].Stream;

        for (var i = wad.LumpInfos.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(wad.LumpInfos[i].Stream, targetStream))
                continue;

            if (!IsClassicMapMarker(wad, i))
                continue;

            if (seen.Add(wad.LumpInfos[i].Name))
                result.Add(wad.LumpInfos[i].Name);
        }

        result.Sort(CompareMapNames);
        return result;
    }

    private static bool IsClassicMapMarker(Wad wad, int lump)
    {
        var lumps = wad.LumpInfos;

        if ((uint)lump >= (uint)lumps.Count)
            return false;

        var name = lumps[lump].Name;
        if (!TryParseCommercialMap(name, out _) &&
            !TryParseEpisodeMap(name, out _, out _))
        {
            return false;
        }

        return lump + 4 < lumps.Count &&
               string.Equals(lumps[lump + 1].Name, "THINGS", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(lumps[lump + 2].Name, "LINEDEFS", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(lumps[lump + 3].Name, "SIDEDEFS", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(lumps[lump + 4].Name, "VERTEXES", StringComparison.OrdinalIgnoreCase);
    }

    private static void SelectMap(GameOptions options, string mapName)
    {
        if (TryParseCommercialMap(mapName, out var map))
        {
            options.GameMode = GameMode.Commercial;
            options.Map = map;
            return;
        }

        if (TryParseEpisodeMap(mapName, out var episode, out map))
        {
            options.GameMode = GameMode.Registered;
            options.Episode = episode;
            options.Map = map;
            return;
        }

        throw new InvalidOperationException($"Unsupported map marker '{mapName}'.");
    }

    private static bool TryParseCommercialMap(string name, out int map)
    {
        map = 0;

        return name != null &&
               name.Length > 3 &&
               name.StartsWith("MAP", StringComparison.OrdinalIgnoreCase) &&
               int.TryParse(name.AsSpan(3), out map) &&
               map > 0;
    }

    private static bool TryParseEpisodeMap(string name, out int episode, out int map)
    {
        episode = 0;
        map = 0;

        if (string.IsNullOrEmpty(name) || char.ToUpperInvariant(name[0]) != 'E')
            return false;

        var separator = name.IndexOf('M', 1);

        return separator >= 2 &&
               separator < name.Length - 1 &&
               int.TryParse(name.AsSpan(1, separator - 1), out episode) &&
               int.TryParse(name.AsSpan(separator + 1), out map) &&
               episode > 0 &&
               map > 0;
    }

    private static int CompareMapNames(string x, string y)
    {
        if (TryParseCommercialMap(x, out var xMap) &&
            TryParseCommercialMap(y, out var yMap))
        {
            return xMap.CompareTo(yMap);
        }

        if (TryParseEpisodeMap(x, out var xEpisode, out xMap) &&
            TryParseEpisodeMap(y, out var yEpisode, out yMap))
        {
            var episode = xEpisode.CompareTo(yEpisode);
            return episode != 0 ? episode : xMap.CompareTo(yMap);
        }

        return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateMap(Map map, string context)
    {
        Assert.IsNotNull(map, $"{context}: map is null.");
        Assert.IsTrue(map.Vertices.Length > 0, $"{context}: no vertices.");
        Assert.IsTrue(map.Sectors.Length > 0, $"{context}: no sectors.");
        Assert.IsTrue(map.Lines.Length > 0, $"{context}: no linedefs.");
        Assert.IsTrue(map.Segs.Length > 0, $"{context}: no segs.");
        Assert.IsTrue(map.Subsectors.Length > 0, $"{context}: no subsectors.");
        Assert.IsNotNull(map.BlockMap, $"{context}: BLOCKMAP is null.");
        Assert.IsTrue(map.BlockMap.Width > 0, $"{context}: BLOCKMAP width <= 0.");
        Assert.IsTrue(map.BlockMap.Height > 0, $"{context}: BLOCKMAP height <= 0.");

        for (var i = 0; i < map.Subsectors.Length; i++)
        {
            var subsector = map.Subsectors[i];

            Assert.IsTrue(subsector.SegCount > 0, $"{context}: subsector {i} has no segs.");
            Assert.IsTrue(
                subsector.FirstSeg >= 0 &&
                (long)subsector.FirstSeg + subsector.SegCount <= map.Segs.Length,
                $"{context}: subsector {i} has invalid seg range.");
        }

        for (var i = 0; i < map.Nodes.Length; i++)
        {
            var children = map.Nodes[i].Children;

            for (var side = 0; side < 2; side++)
            {
                var child = children[side];

                if (Node.IsSubsector(child))
                {
                    var subsector = Node.GetSubsector(child);
                    Assert.IsTrue(
                        (uint)subsector < (uint)map.Subsectors.Length,
                        $"{context}: node {i} child {side} references invalid subsector {subsector}.");
                }
                else
                {
                    Assert.IsTrue(
                        (uint)child < (uint)map.Nodes.Length,
                        $"{context}: node {i} child {side} references invalid node {child}.");
                }
            }
        }
    }

    private static void ResetDefinitions()
    {
        using var wad = new Wad(WadPath.Doom2);
        DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
    }
}
