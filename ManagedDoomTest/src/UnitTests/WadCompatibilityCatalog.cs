// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using ManagedDoom.Compatibility;
//
// namespace ManagedDoomTest.UnitTests;
//
// internal static class WadCompatibilityCatalog
// {
//     public static IReadOnlyList<WadCompatibilityCase> Create()
//     {
//         var result = new List<WadCompatibilityCase>
//         {
//             new("DOOM Shareware", GameCompatibility.Vanilla, WadPath.Doom1Shareware),
//             new("DOOM", GameCompatibility.Vanilla, WadPath.Doom1),
//             new("DOOM II", GameCompatibility.Vanilla, WadPath.Doom2),
//             new("TNT: Evilution", GameCompatibility.Vanilla, WadPath.Tnt),
//             new("The Plutonia Experiment", GameCompatibility.Vanilla, WadPath.Plutonia),
//
//             new("Requiem", GameCompatibility.Vanilla, WadPath.Doom2, WadPath.Requiem),
//             new("TNT: Blood", GameCompatibility.Vanilla, WadPath.Doom2, WadPath.TntBlood),
//             new("Memento Mori", GameCompatibility.Vanilla, WadPath.Doom2, WadPath.MementoMori),
//         };
//
//         // Optional fields are resolved by reflection, so this test file still compiles
//         // when a local checkout does not define every corpus WAD in WadPath.cs.
//         AddOptionalPwad(result, "SIGIL", GameCompatibility.Vanilla, WadPath.Doom1, "Sigil");
//         AddOptionalPwad(result, "Scythe", GameCompatibility.Vanilla, WadPath.Doom2, "Scythe");
//
//         AddOptionalPwad(result, "Boom actions", GameCompatibility.Boom, WadPath.Doom2, "BoomActions");
//         AddOptionalPwad(result, "Boom colormap", GameCompatibility.Boom, WadPath.Doom2, "BoomColormap");
//         AddOptionalPwad(result, "Boom friction", GameCompatibility.Boom, WadPath.Doom2, "BoomFriction");
//         AddOptionalPwad(result, "Boom water", GameCompatibility.Boom, WadPath.Doom2, "BoomWaterMap");
//
//         AddOptionalIwad(result, "Freedoom: Phase 1", GameCompatibility.Vanilla, "Freedoom1");
//         AddOptionalIwad(result, "Freedoom: Phase 2", GameCompatibility.Vanilla, "Freedoom2");
//
//         // Add your huge XNOD WAD here once its WadPath field has a stable name, e.g.:
//         // AddOptionalPwad(result, "Huge XNOD regression", GameCompatibility.Boom, WadPath.Doom2, "HugeXnod");
//         //
//         // Future MBF21:
//         // var mbf21 = ParseCompatibility("Mbf21", GameCompatibility.Boom);
//         // AddOptionalPwad(result, "MBF21 corpus", mbf21, WadPath.Doom2, "Mbf21Corpus");
//
//         return result;
//     }
//
//     private static void AddOptionalPwad(
//         ICollection<WadCompatibilityCase> result,
//         string displayName,
//         GameCompatibility compatibility,
//         string iwad,
//         string wadPathField)
//     {
//         var pwad = GetOptionalWadPath(wadPathField);
//
//         if (pwad != null)
//             result.Add(new WadCompatibilityCase(displayName, compatibility, iwad, pwad));
//     }
//
//     private static void AddOptionalIwad(
//         ICollection<WadCompatibilityCase> result,
//         string displayName,
//         GameCompatibility compatibility,
//         string wadPathField)
//     {
//         var iwad = GetOptionalWadPath(wadPathField);
//
//         if (iwad != null)
//             result.Add(new WadCompatibilityCase(displayName, compatibility, iwad));
//     }
//
//     private static string GetOptionalWadPath(string fieldName)
//     {
//         var field = typeof(WadPath).GetField(
//             fieldName,
//             BindingFlags.Public |
//             BindingFlags.Static |
//             BindingFlags.IgnoreCase);
//
//         return field?.GetValue(null) as string;
//     }
//
//     public static GameCompatibility ParseCompatibility(string name, GameCompatibility fallback)
//     {
//         return Enum.TryParse<GameCompatibility>(name, true, out var value) ? value : fallback;
//     }
// }
//
// internal sealed class WadCompatibilityCase
// {
//     public WadCompatibilityCase(string name, GameCompatibility compatibility, params string[] paths)
//     {
//         Name = name;
//         Compatibility = compatibility;
//         Paths = paths;
//     }
//
//     public string Name { get; }
//     public GameCompatibility Compatibility { get; }
//     public string[] Paths { get; }
// }
