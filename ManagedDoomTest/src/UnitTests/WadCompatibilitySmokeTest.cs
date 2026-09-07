// using System;
// using System.Collections.Generic;
// using System.IO;
// using Microsoft.VisualStudio.TestTools.UnitTesting;
// using ManagedDoom;
//
// namespace ManagedDoomTest.UnitTests;
//
// /// <summary>
// /// Regression corpus for real WAD loading.
// ///
// /// This is intentionally a loading/initialization test, not a demo-compatibility test:
// /// - real PNAMES/TEXTURE/patch/flat/sprite resources are parsed;
// /// - every map in the target WAD is constructed as a gameplay World;
// /// - classic/XNOD/ZNOD BSP and BLOCKMAP fallback are exercised;
// /// - map structure is validated after loading.
// ///
// /// NoMonsters keeps slaughter maps fast, but THINGS are still parsed and map setup still runs.
// /// </summary>
// [TestClass]
// public sealed class WadCompatibilitySmokeTest
// {
//     [TestMethod]
//     [TestCategory("Integration")]
//     [TestCategory("WadCompatibility")]
//     public void KnownWadsLoadResourcesAndEveryMap()
//     {
//         var failures = new List<string>();
//         var skipped = new List<string>();
//         var loadedMaps = 0;
//         var loadedWads = 0;
//         var requireCorpus =
//             string.Equals(
//                 Environment.GetEnvironmentVariable("MANAGEDDOOM_REQUIRE_WAD_CORPUS"),
//                 "1",
//                 StringComparison.OrdinalIgnoreCase);
//
//         foreach (var testCase in WadCompatibilityCatalog.Create())
//         {
//             var missing = FindMissingFiles(testCase.Paths);
//
//             if (missing.Count != 0)
//             {
//                 var message =
//                     $"{testCase.Name}: missing {string.Join(", ", missing)}";
//
//                 if (requireCorpus)
//                     failures.Add(message);
//                 else
//                     skipped.Add(message);
//
//                 continue;
//             }
//
//             try
//             {
//                 using var real = new RealResources(testCase.Paths);
//                 using var content = GameContent.CreateDummy(testCase.Paths);
//                 var maps = FindMapsFromTargetWad(content.Wad);
//
//                 if (maps.Count == 0)
//                 {
//                     failures.Add($"{testCase.Name}: no supported map markers found in target WAD.");
//                     continue;
//                 }
//
//                 var caseLoaded = 0;
//
//                 foreach (var mapName in maps)
//                 {
//                     try
//                     {
//                         var options =
//                             CreateOptions(
//                                 content.Wad,
//                                 testCase.Compatibility,
//                                 mapName);
//
//                         options.NoMonsters = true;
//
//                         // Full gameplay World construction:
//                         // map data, thing allocation, specials and runtime links.
//                         var world =
//                             new World(
//                                 content,
//                                 options,
//                                 null);
//
//                         ValidateMap(
//                             world.Map,
//                             $"{testCase.Name} {mapName} world");
//
//                         // Load the same map with real texture/flat resources.
//                         // This is what catches PNAMES collisions such as BODIES,
//                         // malformed texture composites and real flat lookup regressions.
//                         var realMap =
//                             new Map(
//                                 real.Wad,
//                                 real.Textures,
//                                 real.Flats,
//                                 real.Animation,
//                                 world);
//
//                         ValidateMap(
//                             realMap,
//                             $"{testCase.Name} {mapName} real resources");
//
//                         caseLoaded++;
//                         loadedMaps++;
//                     }
//                     catch (Exception e)
//                     {
//                         failures.Add(
//                             $"{testCase.Name} {mapName}:{Environment.NewLine}{e}");
//                     }
//                 }
//
//                 loadedWads++;
//
//                 Console.WriteLine(
//                     $"WAD COMPAT: {testCase.Name}: " +
//                     $"{caseLoaded}/{maps.Count} maps OK");
//             }
//             catch (Exception e)
//             {
//                 failures.Add(
//                     $"{testCase.Name}: resource/bootstrap loading failed:" +
//                     Environment.NewLine +
//                     e);
//             }
//         }
//
//         foreach (var item in skipped)
//             Console.WriteLine("WAD COMPAT SKIP: " + item);
//
//         if (failures.Count != 0)
//         {
//             Assert.Fail(
//                 $"WAD compatibility sweep found {failures.Count} failure(s)." +
//                 Environment.NewLine +
//                 Environment.NewLine +
//                 string.Join(
//                     Environment.NewLine +
//                     Environment.NewLine +
//                     "--------------------------------------------------" +
//                     Environment.NewLine,
//                     failures));
//         }
//
//         if (loadedWads == 0)
//         {
//             Assert.Inconclusive(
//                 "No WAD corpus files were available. " +
//                 "Place the WadPath files beside the test runner, or set " +
//                 "MANAGEDDOOM_REQUIRE_WAD_CORPUS=1 on a machine that contains the full corpus.");
//         }
//
//         Assert.IsTrue(
//             loadedMaps > 0,
//             "The WAD compatibility sweep did not load any maps.");
//     }
//
//     private static List<string> FindMissingFiles(string[] paths)
//     {
//         var result = new List<string>();
//
//         foreach (var path in paths)
//         {
//             if (!File.Exists(path))
//                 result.Add(path);
//         }
//
//         return result;
//     }
//
//     private static void ValidateMap(Map map, string context)
//     {
//         Assert.IsNotNull(map, $"{context}: map is null.");
//         Assert.IsTrue(map.Vertices.Length > 0, $"{context}: no vertices.");
//         Assert.IsTrue(map.Sectors.Length > 0, $"{context}: no sectors.");
//         Assert.IsTrue(map.Lines.Length > 0, $"{context}: no linedefs.");
//         Assert.IsTrue(map.Segs.Length > 0, $"{context}: no segs.");
//         Assert.IsTrue(map.Subsectors.Length > 0, $"{context}: no subsectors.");
//         Assert.IsNotNull(map.BlockMap, $"{context}: BLOCKMAP is null.");
//         Assert.IsTrue(map.BlockMap.Width > 0, $"{context}: BLOCKMAP width <= 0.");
//         Assert.IsTrue(map.BlockMap.Height > 0, $"{context}: BLOCKMAP height <= 0.");
//
//         ValidateSubsectors(map, context);
//         ValidateNodes(map, context);
//     }
//
//     private static void ValidateSubsectors(Map map, string context)
//     {
//         for (var i = 0; i < map.Subsectors.Length; i++)
//         {
//             var subsector = map.Subsectors[i];
//
//             Assert.IsTrue(subsector.SegCount > 0, $"{context}: subsector {i} has no segs.");
//
//             Assert.IsTrue(
//                 subsector.FirstSeg >= 0 &&
//                 (long)subsector.FirstSeg + subsector.SegCount <= map.Segs.Length,
//                 $"{context}: subsector {i} has invalid seg range " +
//                 $"[{subsector.FirstSeg}, {subsector.FirstSeg + subsector.SegCount}).");
//         }
//     }
//
//     private static void ValidateNodes(Map map, string context)
//     {
//         if (map.Nodes.Length == 0)
//             return;
//
//         for (var i = 0; i < map.Nodes.Length; i++)
//         {
//             var children = map.Nodes[i].Children;
//
//             for (var side = 0; side < 2; side++)
//             {
//                 var child = children[side];
//
//                 if (Node.IsSubsector(child))
//                 {
//                     var subsector = Node.GetSubsector(child);
//
//                     Assert.IsTrue((uint)subsector < (uint)map.Subsectors.Length, $"{context}: node {i} child {side} references invalid subsector {subsector}.");
//                 }
//                 else
//                 {
//                     Assert.IsTrue((uint)child < (uint)map.Nodes.Length, $"{context}: node {i} child {side} references invalid node {child}.");
//                 }
//             }
//         }
//     }
//
//     private static GameOptions CreateOptions(Wad wad, ManagedDoom.Compatibility.GameCompatibility compatibility, string mapName)
//     {
//         var options = new GameOptions
//         {
//             GameVersion = wad.GameVersion,
//             GameMode = wad.GameMode,
//             MissionPack = wad.MissionPack,
//             Compatibility = compatibility
//         };
//
//         if (TryParseCommercialMap(mapName, out var map))
//         {
//             options.GameMode = GameMode.Commercial;
//             options.Map = map;
//             return options;
//         }
//
//         if (TryParseEpisodeMap(mapName, out var episode, out map))
//         {
//             options.Episode = episode;
//             options.Map = map;
//             return options;
//         }
//
//         throw new InvalidOperationException(
//             $"Unsupported map marker '{mapName}'.");
//     }
//
//     private static IReadOnlyList<string> FindMapsFromTargetWad(Wad wad)
//     {
//         var result = new List<string>();
//         var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//
//         if (wad.LumpInfos.Count == 0)
//             return result;
//
//         // With IWAD + PWAD, the last stream is the target corpus WAD.
//         // The base IWAD has its own catalog entry, so we avoid re-testing all base maps.
//         var targetStream = wad.LumpInfos[^1].Stream;
//
//         for (var i = wad.LumpInfos.Count - 1; i >= 0; i--)
//         {
//             if (!ReferenceEquals(wad.LumpInfos[i].Stream, targetStream))
//                 continue;
//
//             if (!IsMapMarker(wad, i))
//                 continue;
//
//             if (seen.Add(wad.LumpInfos[i].Name))
//                 result.Add(wad.LumpInfos[i].Name);
//         }
//
//         result.Sort(CompareMapNames);
//         return result;
//     }
//
//     private static bool IsMapMarker(Wad wad, int lump)
//     {
//         var lumps = wad.LumpInfos;
//
//         if ((uint)lump >= (uint)lumps.Count)
//             return false;
//
//         var name = lumps[lump].Name;
//
//         if (!TryParseCommercialMap(name, out _) &&
//             !TryParseEpisodeMap(name, out _, out _))
//         {
//             return false;
//         }
//
//         // UDMF is included deliberately. Until UDMF support exists, adding such a
//         // WAD to the corpus produces one clear unsupported-format regression report.
//         if (lump + 1 < lumps.Count &&
//             string.Equals(
//                 lumps[lump + 1].Name,
//                 "TEXTMAP",
//                 StringComparison.OrdinalIgnoreCase))
//         {
//             return true;
//         }
//
//         return lump + 4 < lumps.Count &&
//                string.Equals(lumps[lump + 1].Name, "THINGS", StringComparison.OrdinalIgnoreCase) &&
//                string.Equals(lumps[lump + 2].Name, "LINEDEFS", StringComparison.OrdinalIgnoreCase) &&
//                string.Equals(lumps[lump + 3].Name, "SIDEDEFS", StringComparison.OrdinalIgnoreCase) &&
//                string.Equals(lumps[lump + 4].Name, "VERTEXES", StringComparison.OrdinalIgnoreCase);
//     }
//
//     private static bool TryParseCommercialMap(string name, out int map)
//     {
//         map = 0;
//
//         return name != null &&
//                name.Length > 3 &&
//                name.StartsWith("MAP", StringComparison.OrdinalIgnoreCase) &&
//                int.TryParse(name.AsSpan(3), out map) &&
//                map > 0;
//     }
//
//     private static bool TryParseEpisodeMap(string name, out int episode, out int map)
//     {
//         episode = 0;
//         map = 0;
//
//         if (string.IsNullOrEmpty(name) || char.ToUpperInvariant(name[0]) != 'E')
//         {
//             return false;
//         }
//
//         var separator = name.IndexOf('M', 1);
//
//         return separator >= 2 &&
//                separator < name.Length - 1 &&
//                int.TryParse(name.AsSpan(1, separator - 1), out episode) &&
//                int.TryParse(name.AsSpan(separator + 1), out map) &&
//                episode > 0 &&
//                map > 0;
//     }
//
//     private static int CompareMapNames(string x, string y)
//     {
//         if (TryParseCommercialMap(x, out var xMap) &&
//             TryParseCommercialMap(y, out var yMap))
//         {
//             return xMap.CompareTo(yMap);
//         }
//
//         if (TryParseEpisodeMap(x, out var xEpisode, out xMap) &&
//             TryParseEpisodeMap(y, out var yEpisode, out yMap))
//         {
//             var episode = xEpisode.CompareTo(yEpisode);
//
//             return episode != 0 ? episode : xMap.CompareTo(yMap);
//         }
//
//         return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
//     }
//
//     private sealed class RealResources : IDisposable
//     {
//         public RealResources(string[] paths)
//         {
//             Wad = new Wad(paths);
//             _ = new Palette(Wad);
//             _ = new ColorMap(Wad);
//             Textures = new TextureLookup(Wad);
//             Flats = new FlatLookup(Wad);
//             _ = new SpriteLookup(Wad);
//             Animation = new TextureAnimation(Textures, Flats);
//         }
//
//         public Wad Wad { get; }
//         public TextureLookup Textures { get; }
//         public FlatLookup Flats { get; }
//         public TextureAnimation Animation { get; }
//
//         public void Dispose()
//         {
//             Wad.Dispose();
//         }
//     }
// }
