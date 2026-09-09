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
            ((short)271, GameCompatibility.Boom),
            ((short)272, GameCompatibility.Boom),
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
    public void SkyTransferLinedefsDoNotPromoteBoomGameplayCompatibilityToMbf()
    {
        foreach (var special in new short[] { 271, 272 })
        {
            var path = CreateMapWad(special);

            try
            {
                using var wad = new Wad(path);
                var result = CompatibilityDetector.Detect(wad);

                Assert.AreEqual(GameCompatibility.Boom, result.Compatibility);
                Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }


    [TestMethod]
    public void EmbeddedMbfCodePointerSelectsMbf()
    {
        var path = CreateDeHackEdWad("[CODEPTR]\nFRAME 1 = Mushroom\n");

        try
        {
            using var wad = new Wad(path);
            var result = CompatibilityDetector.Detect(wad);

            Assert.AreEqual(GameCompatibility.Mbf, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [DataTestMethod]
    [DataRow(968)]
    [DataRow(970)]
    [DataRow(1062)]
    [DataRow(1074)]
    [DataRow(1075)]
    public void EmbeddedClassicMbfCodePointerSourceSelectsMbf(int sourceFrame)
    {
        var path = CreateDeHackEdWad($"Pointer 1 (Frame 194)\nCodep Frame = {sourceFrame}\n");

        try
        {
            using var wad = new Wad(path);
            var result = CompatibilityDetector.Detect(wad);

            Assert.AreEqual(GameCompatibility.Mbf, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ClassicMbfScreamSourceDoesNotPromoteGameplayCompatibility()
    {
        var path = CreateDeHackEdWad("Pointer 1 (Frame 194)\nCodep Frame = 969\n");

        try
        {
            using var wad = new Wad(path);
            var result = CompatibilityDetector.Detect(wad);

            Assert.AreEqual(GameCompatibility.Vanilla, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.Default, result.Source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void EmbeddedMbfActorBitsSelectMbf()
    {
        foreach (var bits in new[] { 0x10000000, 0x20000000, 0x40000000 })
        {
            var path = CreateDeHackEdWad($"Thing 1\nBits = {bits}\n");

            try
            {
                using var wad = new Wad(path);
                var result = CompatibilityDetector.Detect(wad);

                Assert.AreEqual(GameCompatibility.Mbf, result.Compatibility);
                Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    public void BoomTranslucentBitDoesNotPromoteToMbf()
    {
        var path = CreateDeHackEdWad("Thing 1\nBits = 2147483648\n");

        try
        {
            using var wad = new Wad(path);
            var result = CompatibilityDetector.Detect(wad);

            Assert.AreEqual(GameCompatibility.Boom, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void EmbeddedMbfActorBitMnemonicsSelectMbf()
    {
        var path = CreateDeHackEdWad("Thing 1\nBits = SOLID+SHOOTABLE+FRIEND\n");

        try
        {
            using var wad = new Wad(path);
            var result = CompatibilityDetector.Detect(wad);

            Assert.AreEqual(GameCompatibility.Mbf, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void EmbeddedBoomTranslucentMnemonicSelectsBoomOnly()
    {
        var path = CreateDeHackEdWad("Thing 1\nBits = SOLID+TRANSLUCENT\n");

        try
        {
            using var wad = new Wad(path);
            var result = CompatibilityDetector.Detect(wad);

            Assert.AreEqual(GameCompatibility.Boom, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void VanillaCodePointerDoesNotPromoteGameplayCompatibility()
    {
        var path = CreateDeHackEdWad("[CODEPTR]\nFRAME 1 = Chase\n");

        try
        {
            using var wad = new Wad(path);
            var result = CompatibilityDetector.Detect(wad);

            Assert.AreEqual(GameCompatibility.Vanilla, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.Default, result.Source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void TextPayloadThatLooksLikeCodePtrDoesNotAffectDetection()
    {
        const string payload = "[CODEPTR]\nFRAME 1 = Mushroom";
        Assert.AreEqual(28, payload.Length);

        var path = CreateDeHackEdWad("Text 0 28\n" + payload + "\n");

        try
        {
            using var wad = new Wad(path);
            var result = CompatibilityDetector.Detect(wad);

            Assert.AreEqual(GameCompatibility.Vanilla, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.Default, result.Source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ExternalMbfDeHackEdSelectsMbf()
    {
        var patchPath = CreateExternalDeHackEd("Thing 1\nBits = SOLID+FRIEND\n");

        try
        {
            using var wad = new Wad(WadPath.Doom2);
            var args = new CommandLineArgs(new[] { "-deh", patchPath });
            var result = CompatibilityDetector.Detect(wad, args);

            Assert.AreEqual(GameCompatibility.Mbf, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
        }
        finally
        {
            File.Delete(patchPath);
        }
    }

    [TestMethod]
    public void ExternalClassicMbfMushroomPointerSelectsMbf()
    {
        var patchPath = CreateExternalDeHackEd("Pointer 1 (Frame 194)\nCodep Frame = 1075\n");

        try
        {
            using var wad = new Wad(WadPath.Doom2);
            var args = new CommandLineArgs(new[] { "-deh", patchPath });
            var result = CompatibilityDetector.Detect(wad, args);

            Assert.AreEqual(GameCompatibility.Mbf, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
        }
        finally
        {
            File.Delete(patchPath);
        }
    }

    [TestMethod]
    public void ExternalBoomOnlyDeHackEdSelectsBoom()
    {
        var patchPath = CreateExternalDeHackEd("Thing 1\nBits = SOLID+TRANSLUCENT\n");

        try
        {
            using var wad = new Wad(WadPath.Doom2);
            var args = new CommandLineArgs(new[] { "-deh", patchPath });
            var result = CompatibilityDetector.Detect(wad, args);

            Assert.AreEqual(GameCompatibility.Boom, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
        }
        finally
        {
            File.Delete(patchPath);
        }
    }

    [TestMethod]
    public void NodehSuppressesEmbeddedDeHackEdCompatibilityPromotion()
    {
        var path = CreateDeHackEdWad("Thing 1\nBits = SOLID+FRIEND\n");

        try
        {
            using var wad = new Wad(path);
            var args = new CommandLineArgs(new[] { "-nodeh" });
            var result = CompatibilityDetector.Detect(wad, args);

            Assert.AreEqual(GameCompatibility.Vanilla, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.Default, result.Source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void NodehDoesNotSuppressExplicitExternalDeHackEdDetection()
    {
        var patchPath = CreateExternalDeHackEd("Thing 1\nBits = SOLID+FRIEND\n");

        try
        {
            using var wad = new Wad(WadPath.Doom2);
            var args = new CommandLineArgs(new[] { "-nodeh", "-deh", patchPath });
            var result = CompatibilityDetector.Detect(wad, args);

            Assert.AreEqual(GameCompatibility.Mbf, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
        }
        finally
        {
            File.Delete(patchPath);
        }
    }

    [TestMethod]
    public void ExternalIncludeContributesToCompatibilityDetection()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"manageddoom_deh_include_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        var parentPath = Path.Combine(directory, "parent.bex");
        var childPath = Path.Combine(directory, "child patch.bex");
        File.WriteAllText(parentPath, "INCLUDE NOTEXT \"child patch.bex\"\n", Encoding.ASCII);
        File.WriteAllText(childPath, "Thing 1\nBits = SOLID+FRIEND\n", Encoding.ASCII);

        try
        {
            using var wad = new Wad(WadPath.Doom2);
            var args = new CommandLineArgs(new[] { "-deh", parentPath });
            var result = CompatibilityDetector.Detect(wad, args);

            Assert.AreEqual(GameCompatibility.Mbf, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void NestedExternalIncludeDoesNotContributeToDetection()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"manageddoom_deh_nested_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        var parentPath = Path.Combine(directory, "parent.bex");
        var childPath = Path.Combine(directory, "child.bex");
        var nestedPath = Path.Combine(directory, "nested.bex");
        File.WriteAllText(parentPath, "INCLUDE child.bex\n", Encoding.ASCII);
        File.WriteAllText(childPath, "INCLUDE nested.bex\n", Encoding.ASCII);
        File.WriteAllText(nestedPath, "Thing 1\nBits = SOLID+FRIEND\n", Encoding.ASCII);

        try
        {
            using var wad = new Wad(WadPath.Doom2);
            var args = new CommandLineArgs(new[] { "-deh", parentPath });
            var result = CompatibilityDetector.Detect(wad, args);

            Assert.AreEqual(GameCompatibility.Vanilla, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.Default, result.Source);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void GameOptionsAutoDetectionIncludesExternalDeHackEd()
    {
        var patchPath = CreateExternalDeHackEd("[CODEPTR]\nFRAME 1 = Mushroom\n");

        try
        {
            using var content = GameContent.CreateDummy(WadPath.Doom2);
            var args = new CommandLineArgs(new[] { "-deh", patchPath });
            var options = new GameOptions(args, content);

            Assert.AreEqual(GameCompatibilityMode.Auto, options.CompatibilityMode);
            Assert.AreEqual(GameCompatibility.Mbf, options.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, options.CompatibilitySource);
        }
        finally
        {
            File.Delete(patchPath);
        }
    }

    [TestMethod]
    public void UserOverrideTakesPriorityOverExternalDeHackEdDetection()
    {
        var patchPath = CreateExternalDeHackEd("Thing 1\nBits = SOLID+FRIEND\n");

        try
        {
            using var wad = new Wad(WadPath.Doom2);
            var args = new CommandLineArgs(new[] { "-deh", patchPath });

            var automatic = CompatibilityResolver.Resolve(wad, GameCompatibilityMode.Auto, args);
            Assert.AreEqual(GameCompatibility.Mbf, automatic.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, automatic.Source);

            var overridden = CompatibilityResolver.Resolve(wad, GameCompatibilityMode.Vanilla, args);
            Assert.AreEqual(GameCompatibility.Vanilla, overridden.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.UserOverride, overridden.Source);
        }
        finally
        {
            File.Delete(patchPath);
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


    private static string CreateExternalDeHackEd(string patch)
    {
        var path = Path.Combine(Path.GetTempPath(), $"manageddoom_deh_external_{Guid.NewGuid():N}.bex");
        File.WriteAllText(path, patch, Encoding.ASCII);
        return path;
    }

    private static string CreateDeHackEdWad(string patch)
    {
        var data = Encoding.ASCII.GetBytes(patch);
        var path = Path.Combine(Path.GetTempPath(), $"manageddoom_deh_features_{Guid.NewGuid():N}.wad");

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        writer.Write(Encoding.ASCII.GetBytes("PWAD"));
        writer.Write(1);
        writer.Write(12 + data.Length);
        writer.Write(data);
        writer.Write(12);
        writer.Write(data.Length);

        var name = new byte[8];
        Encoding.ASCII.GetBytes("DEHACKED").CopyTo(name, 0);
        writer.Write(name);

        return path;
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
