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
public sealed class BoomAnimatedTest
{
    [TestMethod]
    public void MissingAnimatedLumpUsesVanillaFallbackInstance()
    {
        var textures = new TestTextureLookup();
        var flats = new TestFlatLookup();
        var vanilla = new TextureAnimation(textures, flats);
        var path = WriteWad("addon.wad", ("DUMMY", Array.Empty<byte>()));

        try
        {
            using var wad = new Wad(path);
            var animated = new BoomAnimated(wad, textures, flats, vanilla);

            Assert.IsFalse(animated.HasCustomTable);
            Assert.AreSame(vanilla, animated.Animation);
        }
        finally
        {
            DeleteWad(path);
        }
    }

    [TestMethod]
    public void CustomTableBuildsTextureAndFlatAnimationsAndStopsAtTerminator()
    {
        var textures = new TestTextureLookup(("TEX1", 10), ("TEX2", 11), ("TEX3", 12));
        var flats = new TestFlatLookup(("FLAT1", 20), ("FLAT2", 21), ("FLAT3", 22), ("FLAT4", 23));
        var vanilla = new TextureAnimation(textures, flats);
        var data = CreateAnimated(
            (1, "TEX3", "TEX1", 4),
            (0, "FLAT4", "FLAT1", 7),
            (-1, "", "", 0),
            (1, "IGNORED", "IGNORED", 1));
        var path = WriteWad("addon.wad", ("ANIMATED", data));

        try
        {
            using var wad = new Wad(path);
            var animated = new BoomAnimated(wad, textures, flats, vanilla);
            var animations = animated.Animation.Animations;

            Assert.IsTrue(animated.HasCustomTable);
            Assert.AreEqual(2, animations.Length);

            Assert.IsTrue(animations[0].IsTexture);
            Assert.AreEqual(10, animations[0].BasePic);
            Assert.AreEqual(12, animations[0].PicNum);
            Assert.AreEqual(3, animations[0].NumPics);
            Assert.AreEqual(4, animations[0].Speed);

            Assert.IsFalse(animations[1].IsTexture);
            Assert.AreEqual(20, animations[1].BasePic);
            Assert.AreEqual(23, animations[1].PicNum);
            Assert.AreEqual(4, animations[1].NumPics);
            Assert.AreEqual(7, animations[1].Speed);
        }
        finally
        {
            DeleteWad(path);
        }
    }

    [TestMethod]
    public void MissingStartResourceSkipsDefinitionLikeBoom()
    {
        var textures = new TestTextureLookup(("GOOD1", 1), ("GOOD2", 2));
        var flats = new TestFlatLookup();
        var vanilla = new TextureAnimation(textures, flats);
        var path = WriteWad("addon.wad", ("ANIMATED", CreateAnimated(
            (1, "MISSING2", "MISSING1", 3),
            (1, "GOOD2", "GOOD1", 5),
            (-1, "", "", 0))));

        try
        {
            using var wad = new Wad(path);
            var animated = new BoomAnimated(wad, textures, flats, vanilla);

            Assert.AreEqual(1, animated.Animation.Animations.Length);
            Assert.AreEqual(1, animated.Animation.Animations[0].BasePic);
            Assert.AreEqual(5, animated.Animation.Animations[0].Speed);
        }
        finally
        {
            DeleteWad(path);
        }
    }

    [TestMethod]
    public void LaterWadAnimatedLumpReplacesEarlierTable()
    {
        var textures = new TestTextureLookup(
            ("EARLY1", 1), ("EARLY2", 2),
            ("LATE1", 10), ("LATE2", 11), ("LATE3", 12));
        var flats = new TestFlatLookup();
        var vanilla = new TextureAnimation(textures, flats);
        var first = WriteWad("first.wad", ("ANIMATED", CreateAnimated(
            (1, "EARLY2", "EARLY1", 2),
            (-1, "", "", 0))));
        var second = WriteWad("second.wad", ("ANIMATED", CreateAnimated(
            (1, "LATE3", "LATE1", 6),
            (-1, "", "", 0))));

        try
        {
            using var wad = new Wad(first, second);
            var animated = new BoomAnimated(wad, textures, flats, vanilla);
            var animations = animated.Animation.Animations;

            Assert.AreEqual(1, animations.Length);
            Assert.AreEqual(10, animations[0].BasePic);
            Assert.AreEqual(12, animations[0].PicNum);
            Assert.AreEqual(6, animations[0].Speed);
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
        var textures = new TestTextureLookup(("TEX1", 1), ("TEX2", 2));
        var flats = new TestFlatLookup();
        var vanilla = new TextureAnimation(textures, flats);
        var complete = CreateAnimated((1, "TEX2", "TEX1", 8));
        var data = new byte[complete.Length + 11];
        Buffer.BlockCopy(complete, 0, data, 0, complete.Length);
        var path = WriteWad("addon.wad", ("ANIMATED", data));

        try
        {
            using var wad = new Wad(path);
            var animated = new BoomAnimated(wad, textures, flats, vanilla);

            Assert.AreEqual(1, animated.Animation.Animations.Length);
            Assert.AreEqual(8, animated.Animation.Animations[0].Speed);
        }
        finally
        {
            DeleteWad(path);
        }
    }

    [TestMethod]
    public void AnimatedResourceIsDetectedAsBoomInAutoCompatibility()
    {
        var path = WriteWad("addon.wad", ("ANIMATED", CreateAnimated((-1, "", "", 0))));

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
    public void RuntimeUsesCustomAnimatedOnlyAtBoomCompatibility()
    {
        var path = WriteWad("animated.wad", ("ANIMATED", CreateAnimated(
            (0, "RROCK08", "RROCK05", 2),
            (-1, "", "", 0))));

        try
        {
            using var content = GameContent.CreateDummy(WadPath.Doom2, path);

            var boomWorld = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
            var boomAnimations = boomWorld.Map.Animation.Animations;
            Assert.AreEqual(1, boomAnimations.Length);
            Assert.IsFalse(boomAnimations[0].IsTexture);
            Assert.AreEqual(content.Flats.GetNumber("RROCK05"), boomAnimations[0].BasePic);
            Assert.AreEqual(content.Flats.GetNumber("RROCK08"), boomAnimations[0].PicNum);
            Assert.AreEqual(2, boomAnimations[0].Speed);

            var vanillaWorld = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
            var vanillaAnimation = vanillaWorld.Map.Animation.Animations.First(a =>
                !a.IsTexture && a.BasePic == content.Flats.GetNumber("RROCK05"));
            Assert.AreEqual(8, vanillaAnimation.Speed);
            Assert.IsTrue(vanillaWorld.Map.Animation.Animations.Length > 1);
        }
        finally
        {
            DeleteWad(path);
        }
    }

    [TestMethod]
    public void CustomAnimatedEntryAdvancesUsingConfiguredSpeed()
    {
        var path = WriteWad("animated.wad", ("ANIMATED", CreateAnimated(
            (0, "RROCK08", "RROCK05", 2),
            (-1, "", "", 0))));

        try
        {
            using var content = GameContent.CreateDummy(WadPath.Doom2, path);
            var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
            var animation = world.Map.Animation.Animations[0];

            world.LevelTime = 0;
            world.Specials.Update();
            var atZero = world.Specials.FlatTranslation[animation.BasePic];

            world.LevelTime = animation.Speed;
            world.Specials.Update();
            var afterOneStep = world.Specials.FlatTranslation[animation.BasePic];

            var expected = animation.BasePic + ((1 + animation.BasePic) % animation.NumPics);
            Assert.AreEqual(expected, afterOneStep);
            Assert.AreNotEqual(atZero, afterOneStep);
        }
        finally
        {
            DeleteWad(path);
        }
    }

    private static byte[] CreateAnimated(params (sbyte Type, string End, string Start, int Speed)[] definitions)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        foreach (var definition in definitions)
        {
            writer.Write(unchecked((byte)definition.Type));
            WriteName(writer, definition.End);
            WriteName(writer, definition.Start);
            writer.Write(definition.Speed);
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
        var directory = Path.Combine(Path.GetTempPath(), $"manageddoom-boom-animated-{Guid.NewGuid():N}");
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

        public TestTextureLookup(params (string Name, int Number)[] textures)
        {
            numbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var texture in textures)
                numbers[texture.Name] = texture.Number;
        }

        public int GetNumber(string name) => numbers.TryGetValue(name, out var number) ? number : -1;
        public int[] SwitchList => Array.Empty<int>();
        public int Count => 0;
        public Texture this[int index] => null;
        public Texture this[string name] => null;
        public IEnumerator<Texture> GetEnumerator() => Enumerable.Empty<Texture>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class TestFlatLookup : IFlatLookup
    {
        private readonly Dictionary<string, int> numbers;

        public TestFlatLookup(params (string Name, int Number)[] flats)
        {
            numbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var flat in flats)
                numbers[flat.Name] = flat.Number;
        }

        public int GetNumber(string name) => numbers.TryGetValue(name, out var number) ? number : -1;
        public int Count => 0;
        public Flat this[int index] => null;
        public Flat this[string name] => null;
        public int SkyFlatNumber => -1;
        public Flat SkyFlat => null;
        public IEnumerator<Flat> GetEnumerator() => Enumerable.Empty<Flat>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
