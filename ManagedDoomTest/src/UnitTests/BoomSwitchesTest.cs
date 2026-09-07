using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManagedDoom.Compatibility.Detection;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomSwitchesTest
{
    [TestMethod]
    public void MissingSwitchesLumpUsesVanillaFallbackList()
    {
        var fallback = new[] { 10, 11, 20, 21 };
        var textures = new TestTextureLookup(fallback);
        var path = WriteWad("addon.wad", ("DUMMY", Array.Empty<byte>()));

        try
        {
            using var wad = new Wad(path);
            var switches = new BoomSwitches(wad, textures);

            Assert.IsFalse(switches.HasCustomTable);
            Assert.AreSame(fallback, switches.SwitchList);
        }
        finally
        {
            DeleteWad(path);
        }
    }

    [TestMethod]
    public void CommercialModeIncludesEpisodesOneTwoAndThreeAndStopsAtTerminator()
    {
        var textures = CreateEpisodeTextures();
        var data = CreateSwitches(
            ("OFF1", "ON1", 1),
            ("OFF2", "ON2", 2),
            ("OFF3", "ON3", 3),
            ("", "", 0),
            ("AFTER", "AFTERON", 1));
        var path = WriteWad("doom2.wad", ("SWITCHES", data));

        try
        {
            using var wad = new Wad(path);
            var switches = new BoomSwitches(wad, textures);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, switches.SwitchList);
        }
        finally
        {
            DeleteWad(path);
        }
    }

    [TestMethod]
    public void RetailModeFiltersCommercialOnlyEntries()
    {
        var textures = CreateEpisodeTextures();
        var path = WriteWad("doom.wad", ("SWITCHES", CreateSwitches(
            ("OFF1", "ON1", 1),
            ("OFF2", "ON2", 2),
            ("OFF3", "ON3", 3),
            ("", "", 0))));

        try
        {
            using var wad = new Wad(path);
            var switches = new BoomSwitches(wad, textures);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, switches.SwitchList);
        }
        finally
        {
            DeleteWad(path);
        }
    }

    [TestMethod]
    public void SharewareModeKeepsOnlyEpisodeOneEntries()
    {
        var textures = CreateEpisodeTextures();
        var path = WriteWad("doom1.wad", ("SWITCHES", CreateSwitches(
            ("OFF1", "ON1", 1),
            ("OFF2", "ON2", 2),
            ("OFF3", "ON3", 3),
            ("", "", 0))));

        try
        {
            using var wad = new Wad(path);
            var switches = new BoomSwitches(wad, textures);

            CollectionAssert.AreEqual(new[] { 1, 2 }, switches.SwitchList);
        }
        finally
        {
            DeleteWad(path);
        }
    }

    [TestMethod]
    public void DefinitionsReferencingMissingTexturesAreIgnored()
    {
        var textures = new TestTextureLookup(Array.Empty<int>(),
            ("GOODOFF", 7), ("GOODON", 8), ("BADONE", 9));
        var path = WriteWad("doom2.wad", ("SWITCHES", CreateSwitches(
            ("BADONE", "MISSING", 3),
            ("GOODOFF", "GOODON", 3),
            ("", "", 0))));

        try
        {
            using var wad = new Wad(path);
            var switches = new BoomSwitches(wad, textures);

            CollectionAssert.AreEqual(new[] { 7, 8 }, switches.SwitchList);
        }
        finally
        {
            DeleteWad(path);
        }
    }

    [TestMethod]
    public void LaterWadSwitchesLumpReplacesEarlierTable()
    {
        var textures = new TestTextureLookup(Array.Empty<int>(),
            ("EARLYOFF", 1), ("EARLYON", 2), ("LATEOFF", 3), ("LATEON", 4));
        var first = WriteWad("doom2.wad", ("SWITCHES", CreateSwitches(
            ("EARLYOFF", "EARLYON", 3),
            ("", "", 0))));
        var second = WriteWad("addon.wad", ("SWITCHES", CreateSwitches(
            ("LATEOFF", "LATEON", 3),
            ("", "", 0))));

        try
        {
            using var wad = new Wad(first, second);
            var switches = new BoomSwitches(wad, textures);

            CollectionAssert.AreEqual(new[] { 3, 4 }, switches.SwitchList);
        }
        finally
        {
            DeleteWad(first);
            DeleteWad(second);
        }
    }

    [TestMethod]
    public void IncompleteTrailingRecordIsIgnoredSafely()
    {
        var textures = new TestTextureLookup(Array.Empty<int>(), ("OFF", 1), ("ON", 2));
        var complete = CreateSwitches(("OFF", "ON", 3));
        var data = new byte[complete.Length + 7];
        Buffer.BlockCopy(complete, 0, data, 0, complete.Length);
        var path = WriteWad("doom2.wad", ("SWITCHES", data));

        try
        {
            using var wad = new Wad(path);
            var switches = new BoomSwitches(wad, textures);

            CollectionAssert.AreEqual(new[] { 1, 2 }, switches.SwitchList);
        }
        finally
        {
            DeleteWad(path);
        }
    }

    [TestMethod]
    public void SwitchesResourceIsDetectedAsBoomInAutoCompatibility()
    {
        var path = WriteWad("addon.wad", ("SWITCHES", CreateSwitches(
            ("OFF", "ON", 1),
            ("", "", 0))));

        try
        {
            using var wad = new Wad(path);
            var result = CompatibilityDetector.Detect(wad);

            Assert.AreEqual(GameCompatibility.Boom, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
        }
        finally
        {
            DeleteWad(path);
        }
    }

    [TestMethod]
    public void RuntimeUsesCustomSwitchesOnlyAtBoomCompatibility()
    {
        var path = WriteWad("switches.wad", ("SWITCHES", CreateSwitches(
            ("SW1BRCOM", "SW2BRN1", 3),
            ("", "", 0))));

        try
        {
            using var content = GameContent.CreateDummy(WadPath.Doom2, path);
            var off = content.Textures.GetNumber("SW1BRCOM");
            var customOn = content.Textures.GetNumber("SW2BRN1");
            var vanillaOn = content.Textures.GetNumber("SW2BRCOM");

            Assert.AreNotEqual(-1, off);
            Assert.AreNotEqual(-1, customOn);
            Assert.AreNotEqual(-1, vanillaOn);
            Assert.AreNotEqual(customOn, vanillaOn);

            var boomWorld = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
            var boomLine = PrepareSwitchLine(boomWorld, off);
            boomWorld.Specials.ChangeSwitchTexture(boomLine, false);
            Assert.AreEqual(customOn, boomLine.FrontSide.TopTexture);

            var vanillaWorld = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
            var vanillaLine = PrepareSwitchLine(vanillaWorld, off);
            vanillaWorld.Specials.ChangeSwitchTexture(vanillaLine, false);
            Assert.AreEqual(vanillaOn, vanillaLine.FrontSide.TopTexture);
        }
        finally
        {
            DeleteWad(path);
        }
    }

    private static LineDef PrepareSwitchLine(World world, int topTexture)
    {
        var line = world.Map.Lines.First(l => l.SoundOrigin != null);
        line.FrontSide.TopTexture = topTexture;
        line.FrontSide.MiddleTexture = 0;
        line.FrontSide.BottomTexture = 0;
        return line;
    }

    private static TestTextureLookup CreateEpisodeTextures()
    {
        return new TestTextureLookup(Array.Empty<int>(),
            ("OFF1", 1), ("ON1", 2),
            ("OFF2", 3), ("ON2", 4),
            ("OFF3", 5), ("ON3", 6),
            ("AFTER", 7), ("AFTERON", 8));
    }

    private static byte[] CreateSwitches(params (string Off, string On, short Episode)[] definitions)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        foreach (var definition in definitions)
        {
            WriteName(writer, definition.Off);
            WriteName(writer, definition.On);
            writer.Write(definition.Episode);
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static void WriteName(BinaryWriter writer, string value)
    {
        var bytes = new byte[9];
        var encoded = Encoding.ASCII.GetBytes(value);
        Buffer.BlockCopy(encoded, 0, bytes, 0, Math.Min(encoded.Length, 8));
        writer.Write(bytes);
    }

    private static string WriteWad(string fileName, params (string Name, byte[] Data)[] lumps)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"manageddoom-boom-switches-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        var positions = new int[lumps.Length];

        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        writer.Write(Encoding.ASCII.GetBytes("PWAD"));
        writer.Write(lumps.Length);
        writer.Write(0);

        for (var i = 0; i < lumps.Length; i++)
        {
            positions[i] = checked((int)stream.Position);
            writer.Write(lumps[i].Data);
        }

        var directoryOffset = checked((int)stream.Position);
        for (var i = 0; i < lumps.Length; i++)
        {
            writer.Write(positions[i]);
            writer.Write(lumps[i].Data.Length);

            var name = new byte[8];
            var encoded = Encoding.ASCII.GetBytes(lumps[i].Name);
            Buffer.BlockCopy(encoded, 0, name, 0, Math.Min(encoded.Length, name.Length));
            writer.Write(name);
        }

        stream.Position = 8;
        writer.Write(directoryOffset);
        writer.Flush();

        return path;
    }

    private static void DeleteWad(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (File.Exists(path))
            File.Delete(path);
        if (directory != null && Directory.Exists(directory))
            Directory.Delete(directory, true);
    }

    private sealed class TestTextureLookup : ITextureLookup
    {
        private readonly Dictionary<string, int> numbers;

        public TestTextureLookup(int[] switchList, params (string Name, int Number)[] textures)
        {
            SwitchList = switchList;
            numbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var texture in textures)
                numbers[texture.Name] = texture.Number;
        }

        public int GetNumber(string name) => numbers.TryGetValue(name, out var number) ? number : -1;
        public int[] SwitchList { get; }
        public int Count => 0;
        public Texture this[int index] => null;
        public Texture this[string name] => null;
        public IEnumerator<Texture> GetEnumerator() => Enumerable.Empty<Texture>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
