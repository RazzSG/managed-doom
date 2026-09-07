using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ManagedDoom;
using ManagedDoom.Compatibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.CompatibilityTests;

/// <summary>
/// Phase 17.6 external corpus verification for real DeHackEd/BEX WAD sets.
/// The corpus assets are intentionally not redistributed with the repository.
/// Configure data/reference/dehacked-corpus.json or set
/// MANAGEDDOOM_DEHACKED_CORPUS_MANIFEST to its full path.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class DeHackEdRealWadCorpusTest
{
    private const string EnvironmentVariable = "MANAGEDDOOM_DEHACKED_CORPUS_MANIFEST";
    private const string ManifestFileName = "dehacked-corpus.json";
    private const int MinimumCorpusEntries = 3;
    private const int DefaultTicksPerMap = 128;
    private const int MaximumTicksPerMap = 4096;

    [TestMethod]
    [TestCategory("ExternalReference")]
    [TestCategory("DeHackEdCorpus")]
    public void RealDeHackEdCorpusAppliesDefinitionsAndRunsRepresentativeMaps()
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
                Console.WriteLine($"[DeHackEd corpus] {entry.Name}: PASS");
            }
            catch (Exception e)
            {
                failures.Add($"{entry.Name}: {e}");
                Console.WriteLine($"[DeHackEd corpus] {entry.Name}: FAIL - {e.GetType().Name}: {e.Message}");
            }
        }

        if (failures.Count != 0)
        {
            Assert.Fail(
                $"DeHackEd/BEX corpus verification completed all {manifest.Entries.Count} entries with {failures.Count} failure(s):" +
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
        var dehPaths = entry.Deh
            .Select(path => ResolveRequiredPath(path, manifestDirectory, entry.Name, "DEH/BEX"))
            .ToArray();

        // Capture the pristine executable definition state immediately before
        // loading this corpus entry. DeHackEd.Initialize always restores the
        // same process-global baseline before applying a new patch set.
        ResetDefinitionsToBaseline(iwadPath);
        var baselineFingerprint = CaptureDefinitionFingerprint();

        var args = CreateRuntimeArgs(iwadPath, pwadPaths, dehPaths);

        try
        {
            using var content = new GameContent(args);

            var embeddedPatchCount = content.Wad.LumpInfos.Count(
                lump => string.Equals(lump.Name, "DEHACKED", StringComparison.OrdinalIgnoreCase));

            Assert.IsTrue(
                embeddedPatchCount > 0 || dehPaths.Length > 0,
                $"{entry.Name}: corpus entry must contain at least one embedded DEHACKED lump or external .deh/.bex patch.");

            var patchedFingerprint = CaptureDefinitionFingerprint();

            if (entry.RequireDefinitionChange)
            {
                Assert.AreNotEqual(
                    baselineFingerprint,
                    patchedFingerprint,
                    $"{entry.Name}: the configured patch sources loaded without changing the DeHackEd/BEX definition state. " +
                    "This usually means the wrong patch/WAD was selected, the patch was empty, or its contents were ignored.");
            }

            Console.WriteLine(
                $"[DeHackEd corpus] {entry.Name}: {embeddedPatchCount} embedded patch(es), " +
                $"{dehPaths.Length} external patch(es), definition fingerprint {patchedFingerprint[..12]}.");

            var ticksPerMap = entry.TicksPerMap ?? manifestDefaultTicks;

            foreach (var mapName in entry.Maps)
                RunRepresentativeMap(entry.Name, args, content, mapName, ticksPerMap);
        }
        finally
        {
            // External corpus patches mutate process-global DoomInfo/DoomString
            // tables. Restore immediately so the corpus harness cannot make
            // later unrelated tests order-dependent.
            ResetDefinitionsToBaseline(iwadPath);
        }
    }

    private static void ResetDefinitionsToBaseline(string iwadPath)
    {
        using var wad = new Wad(new[] { iwadPath });
        DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
    }

    private static void RunRepresentativeMap(
        string entryName,
        CommandLineArgs args,
        GameContent content,
        string mapName,
        int ticksPerMap)
    {
        var selection = ParseMapSelection(mapName);

        Assert.AreNotEqual(
            -1,
            content.Wad.GetLumpNumber(selection.CanonicalName),
            $"{entryName}: configured representative map {selection.CanonicalName} was not found in the merged WAD set.");

        var options = new GameOptions(args, content)
        {
            Episode = selection.Episode,
            Map = selection.Map,
            NoMonsters = true,
            Skill = GameSkill.Medium
        };

        var game = new DoomGame(content, options);
        var commands = CreateCommands();

        game.DeferedInitNew();
        game.Update(commands);

        Assert.IsNotNull(game.World, $"{entryName} {selection.CanonicalName}: world creation failed.");
        Assert.IsNotNull(game.World.Map, $"{entryName} {selection.CanonicalName}: map creation failed.");

        var startGameTic = game.GameTic;

        for (var i = 0; i < ticksPerMap; i++)
            game.Update(commands);

        Assert.IsNotNull(game.World, $"{entryName} {selection.CanonicalName}: world disappeared while ticking the corpus map.");
        Assert.IsNotNull(game.World.Map, $"{entryName} {selection.CanonicalName}: map disappeared while ticking the corpus map.");
        Assert.AreEqual(
            startGameTic + ticksPerMap,
            game.GameTic,
            $"{entryName} {selection.CanonicalName}: the real game loop did not advance for all requested corpus ticks.");
    }

    private static CommandLineArgs CreateRuntimeArgs(
        string iwadPath,
        IReadOnlyList<string> pwadPaths,
        IReadOnlyList<string> dehPaths)
    {
        var values = new List<string> { "-iwad", iwadPath };

        if (pwadPaths.Count != 0)
        {
            values.Add("-file");
            values.AddRange(pwadPaths);
        }

        if (dehPaths.Count != 0)
        {
            values.Add("-deh");
            values.AddRange(dehPaths);
        }

        // Intentionally do not add -nodeh: this corpus exists specifically to
        // exercise the real embedded/external DeHackEd/BEX loading pipeline.
        return new CommandLineArgs(values.ToArray());
    }

    private static TicCmd[] CreateCommands()
    {
        var commands = new TicCmd[Player.MaxPlayerCount];
        for (var i = 0; i < commands.Length; i++)
            commands[i] = new TicCmd();

        return commands;
    }

    private static string CaptureDefinitionFingerprint()
    {
        var builder = new StringBuilder(256 * 1024);

        AppendObjects(builder, "mobj", DoomInfo.MobjInfos);
        AppendObjects(builder, "state", DoomInfo.States);
        AppendObjects(builder, "definition", DoomInfo.WeaponInfos);
        AppendSequence(builder, "ammo-max", DoomInfo.AmmoInfos.Max);
        AppendSequence(builder, "ammo-clip", DoomInfo.AmmoInfos.Clip);
        AppendObjects(builder, "sound", DoomInfo.DeHackEdSoundInfos);

        AppendStaticProperties(builder, "const", typeof(DoomInfo.DeHackEdConst));

        for (var episode = 0; episode < DoomInfo.ParTimes.Doom1.Count; episode++)
            AppendSequence(builder, $"par-e{episode + 1}", DoomInfo.ParTimes.Doom1[episode]);
        AppendSequence(builder, "par-doom2", DoomInfo.ParTimes.Doom2);

        AppendDoomStrings(builder, "sprite", DoomInfo.SpriteNames);
        AppendDoomStrings(builder, "sfx", DoomInfo.SfxNames);
        AppendDoomStrings(builder, "bgm", DoomInfo.BgmNames);
        AppendNamedDoomStrings(builder);
        AppendRawStringReplacements(builder);

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static void AppendObjects<T>(StringBuilder builder, string prefix, IEnumerable<T> values)
    {
        var properties = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();

        var index = 0;
        foreach (var value in values)
        {
            builder.Append(prefix).Append('[').Append(index++).Append("]{");
            foreach (var property in properties)
            {
                builder.Append(property.Name).Append('=');
                AppendValue(builder, property.GetValue(value));
                builder.Append(';');
            }
            builder.AppendLine("}");
        }
    }

    private static void AppendStaticProperties(StringBuilder builder, string prefix, Type type)
    {
        foreach (var property in type
                     .GetProperties(BindingFlags.Static | BindingFlags.Public)
                     .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                     .OrderBy(property => property.Name, StringComparer.Ordinal))
        {
            builder.Append(prefix).Append('.').Append(property.Name).Append('=');
            AppendValue(builder, property.GetValue(null));
            builder.AppendLine();
        }
    }

    private static void AppendSequence<T>(StringBuilder builder, string prefix, IEnumerable<T> values)
    {
        builder.Append(prefix).Append('=');
        foreach (var value in values)
        {
            AppendValue(builder, value);
            builder.Append(',');
        }
        builder.AppendLine();
    }

    private static void AppendDoomStrings(StringBuilder builder, string prefix, IEnumerable<DoomString> values)
    {
        var index = 0;
        foreach (var value in values)
            builder.Append(prefix).Append('[').Append(index++).Append("]=").Append(value).AppendLine();
    }

    private static void AppendNamedDoomStrings(StringBuilder builder)
    {
        foreach (var field in typeof(DoomInfo.Strings)
                     .GetFields(BindingFlags.Static | BindingFlags.Public)
                     .Where(field => field.FieldType == typeof(DoomString))
                     .OrderBy(field => field.Name, StringComparer.Ordinal))
        {
            builder.Append("string.").Append(field.Name).Append('=')
                .Append((DoomString)field.GetValue(null)).AppendLine();
        }
    }

    private static void AppendRawStringReplacements(StringBuilder builder)
    {
        var field = typeof(DoomString).GetField(
            "replacementTable",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(field, "DoomString replacement table reflection contract changed.");

        var replacements = field.GetValue(null) as IDictionary;
        Assert.IsNotNull(replacements, "DoomString replacement table is unavailable.");

        var pairs = new List<(string Key, string Value)>();
        foreach (DictionaryEntry entry in replacements)
            pairs.Add(((string)entry.Key, (string)entry.Value));

        foreach (var pair in pairs.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            builder.Append("raw-string[").Append(pair.Key).Append("]=").Append(pair.Value).AppendLine();
    }

    private static void AppendValue(StringBuilder builder, object value)
    {
        switch (value)
        {
            case null:
                builder.Append("<null>");
                break;
            case Delegate action:
                builder.Append(action.Method.DeclaringType?.FullName)
                    .Append('.')
                    .Append(action.Method.Name);
                break;
            case Fixed fixedValue:
                builder.Append(fixedValue.Data.ToString(CultureInfo.InvariantCulture));
                break;
            case Enum enumValue:
                builder.Append(Convert.ToInt64(enumValue, CultureInfo.InvariantCulture));
                break;
            case IFormattable formattable:
                builder.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                break;
            default:
                builder.Append(value);
                break;
        }
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

            Assert.IsNotNull(manifest, "DeHackEd corpus manifest was empty.");
            return manifest;
        }
        catch (JsonException e)
        {
            Assert.Fail($"Failed to parse DeHackEd corpus manifest '{manifestPath}': {e.Message}");
            throw;
        }
    }

    private static void ValidateManifest(CorpusManifest manifest)
    {
        Assert.IsNotNull(manifest.Entries, "DeHackEd corpus manifest must contain an 'entries' array.");
        Assert.IsTrue(
            manifest.Entries.Count >= MinimumCorpusEntries,
            $"Phase 17.6 requires at least {MinimumCorpusEntries} independent real DeHackEd/BEX corpus entries; " +
            $"the manifest currently contains {manifest.Entries.Count}.");

        Assert.IsTrue(
            manifest.DefaultTicksPerMap is >= 1 and <= MaximumTicksPerMap,
            $"defaultTicksPerMap must be between 1 and {MaximumTicksPerMap}.");

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in manifest.Entries)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Name), "Every DeHackEd corpus entry must have a non-empty name.");
            Assert.IsTrue(names.Add(entry.Name), $"DeHackEd corpus entry name '{entry.Name}' is duplicated.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Iwad), $"{entry.Name}: 'iwad' is required.");
            Assert.IsNotNull(entry.Pwads, $"{entry.Name}: 'pwads' is required (use an empty array for external-patch-only entries).");
            Assert.IsNotNull(entry.Deh, $"{entry.Name}: 'deh' is required (use an empty array for embedded-only entries).");
            Assert.IsFalse(entry.Pwads.Any(string.IsNullOrWhiteSpace), $"{entry.Name}: PWAD paths must not be empty.");
            Assert.IsFalse(entry.Deh.Any(string.IsNullOrWhiteSpace), $"{entry.Name}: DEH/BEX paths must not be empty.");
            Assert.IsNotNull(entry.Maps, $"{entry.Name}: 'maps' is required.");
            Assert.IsTrue(entry.Maps.Count > 0, $"{entry.Name}: choose at least one representative map.");
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
            "directory, project directory, reference directory, or corpus manifest.");
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
            "Phase 17.6 external DeHackEd/BEX corpus test skipped: dehacked-corpus.json was not found. " +
            $"Set {EnvironmentVariable} to your local manifest, or copy " +
            "ManagedDoomTest/data/reference/dehacked-corpus.example.json to dehacked-corpus.json and point it at your local WAD/DEH/BEX files. " +
            "External corpus assets are intentionally not redistributed by this project.");
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
        public int DefaultTicksPerMap { get; set; } = DeHackEdRealWadCorpusTest.DefaultTicksPerMap;
        public List<CorpusEntry> Entries { get; set; } = new();
    }

    public sealed class CorpusEntry
    {
        public string Name { get; set; }
        public string Iwad { get; set; }
        public List<string> Pwads { get; set; } = new();
        public List<string> Deh { get; set; } = new();
        public List<string> Maps { get; set; } = new();
        public int? TicksPerMap { get; set; }
        public bool RequireDefinitionChange { get; set; } = true;
    }

    private readonly record struct MapSelection(int Episode, int Map, string CanonicalName);
}
