using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using ManagedDoom;
using ManagedDoom.Compatibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManagedDoom.Compatibility.Detection;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class CompatibilityDetectorTest
{
    [TestMethod]
    public void Doom2FallsBackToVanillaDefault()
    {
        using var wad = new Wad(WadPath.Doom2);

        var result = CompatibilityDetector.Detect(wad);

        Assert.AreEqual(GameCompatibility.Vanilla, result.Compatibility);
        Assert.AreEqual(CompatibilityDetectionSource.Default, result.Source);
    }

    [TestMethod]
    public void ComplvlSelectsCompatibility()
    {
        var cases = new[]
        {
            ("vanilla", GameCompatibility.Vanilla),
            ("boom", GameCompatibility.Boom),
            ("mbf", GameCompatibility.Mbf),
            ("\uFEFF  MbF21\r\n\0", GameCompatibility.Mbf21)
        };

        foreach (var testCase in cases)
        {
            var path = CreateComplvlWad(testCase.Item1);

            try
            {
                using var wad = new Wad(path);
                var result = CompatibilityDetector.Detect(wad);

                Assert.AreEqual(testCase.Item2, result.Compatibility);
                Assert.AreEqual(CompatibilityDetectionSource.Complvl, result.Source);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }


    [TestMethod]
    public void FeatureScanSelectsMinimumCompatibility()
    {
        var cases = new[]
        {
            ((short)78, GameCompatibility.Boom),
            ((short)85, GameCompatibility.Boom),
            ((short)242, GameCompatibility.Boom),
            ((short)271, GameCompatibility.Mbf),
            ((short)1024, GameCompatibility.Mbf21)
        };

        foreach (var testCase in cases)
        {
            var path = CreateMapWad(testCase.Item1);

            try
            {
                using var wad = new Wad(path);
                var result = CompatibilityDetector.Detect(wad);

                Assert.AreEqual(testCase.Item2, result.Compatibility);
                Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    public void UserOverrideTakesPriorityOverAutomaticDetection()
    {
        var path = CreateComplvlWad("mbf21");

        try
        {
            using var wad = new Wad(path);

            var automatic = CompatibilityResolver.Resolve(wad, GameCompatibilityMode.Auto);
            Assert.AreEqual(GameCompatibility.Mbf21, automatic.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.Complvl, automatic.Source);

            var overridden = CompatibilityResolver.Resolve(wad, GameCompatibilityMode.Vanilla);
            Assert.AreEqual(GameCompatibility.Vanilla, overridden.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.UserOverride, overridden.Source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void GameOptionsResolvesCompatibilityMode()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var args = new CommandLineArgs(new[] { "-compatibility", "mbf21" });

        var options = new GameOptions(args, content);

        Assert.AreEqual(GameCompatibilityMode.Mbf21, options.CompatibilityMode);
        Assert.AreEqual(GameCompatibility.Mbf21, options.Compatibility);
        Assert.AreEqual(CompatibilityDetectionSource.UserOverride, options.CompatibilitySource);
    }

    private static string CreateComplvlWad(string value)
    {
        var path = Path.Combine(Path.GetTempPath(), $"manageddoom_complvl_{Guid.NewGuid():N}.wad");
        var data = Encoding.UTF8.GetBytes(value);

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        writer.Write(Encoding.ASCII.GetBytes("PWAD"));
        writer.Write(1);
        writer.Write(12 + data.Length);
        writer.Write(data);
        writer.Write(12);
        writer.Write(data.Length);

        var name = new byte[8];
        Encoding.ASCII.GetBytes("COMPLVL").CopyTo(name, 0);
        writer.Write(name);

        return path;
    }

    private static string CreateMapWad(short lineSpecial)
    {
        var line = new byte[14];
        BinaryPrimitives.WriteInt16LittleEndian(line.AsSpan(6, 2), lineSpecial);

        var lumps = new (string Name, byte[] Data)[]
        {
            ("MAP01", Array.Empty<byte>()),
            ("THINGS", new byte[10]),
            ("LINEDEFS", line),
            ("SIDEDEFS", Array.Empty<byte>()),
            ("VERTEXES", Array.Empty<byte>()),
            ("SEGS", Array.Empty<byte>()),
            ("SSECTORS", Array.Empty<byte>()),
            ("NODES", Array.Empty<byte>()),
            ("SECTORS", new byte[26]),
            ("REJECT", Array.Empty<byte>()),
            ("BLOCKMAP", Array.Empty<byte>())
        };

        var path = Path.Combine(Path.GetTempPath(), $"manageddoom_features_{Guid.NewGuid():N}.wad");
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        writer.Write(Encoding.ASCII.GetBytes("PWAD"));
        writer.Write(lumps.Length);
        writer.Write(0);

        var positions = new int[lumps.Length];
        for (var i = 0; i < lumps.Length; i++)
        {
            positions[i] = (int)stream.Position;
            writer.Write(lumps[i].Data);
        }

        var directoryOffset = (int)stream.Position;
        for (var i = 0; i < lumps.Length; i++)
        {
            writer.Write(positions[i]);
            writer.Write(lumps[i].Data.Length);

            var name = new byte[8];
            Encoding.ASCII.GetBytes(lumps[i].Name).CopyTo(name, 0);
            writer.Write(name);
        }

        stream.Position = 8;
        writer.Write(directoryOffset);
        return path;
    }

}
