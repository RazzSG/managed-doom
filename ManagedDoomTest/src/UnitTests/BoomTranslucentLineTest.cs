using System;
using System.IO;
using System.Text;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomTranslucentLineTest
{
    [TestMethod]
    public void FeatureGateStartsAtBoom()
    {
        Assert.IsFalse(GameCompatibilityFeatures.SupportsTranslucentLines(GameCompatibility.Vanilla));
        Assert.IsTrue(GameCompatibilityFeatures.SupportsTranslucentLines(GameCompatibility.Boom));
        Assert.IsTrue(GameCompatibilityFeatures.SupportsTranslucentLines(GameCompatibility.Mbf));
        Assert.IsTrue(GameCompatibilityFeatures.SupportsTranslucentLines(GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void TagZeroAffectsOnlyControllerAndKeepsRealMiddleTexture()
    {
        var controller = CreateLine(260, 0, 12, "MIDTEX");
        var other = CreateLine(0, 0, 13, "OTHER");

        BoomTranslucentLineResolver.Apply(
            new[] { controller.Line, other.Line },
            GameCompatibility.Boom);

        Assert.AreEqual(BoomTranslucentLineResolver.DefaultMapName, controller.Line.TranslucencyMapName);
        Assert.IsNull(other.Line.TranslucencyMapName);
        Assert.AreEqual(12, controller.Side.MiddleTexture);
    }


    [TestMethod]
    public void TagZeroRetainsCustomSelectorLikeBoomTranlumpSetup()
    {
        var controller = CreateLine(260, 0, -1, "MYTRANS");

        BoomTranslucentLineResolver.Apply(
            new[] { controller.Line },
            GameCompatibility.Boom);

        Assert.AreEqual("MYTRANS", controller.Line.TranslucencyMapName);
        Assert.AreEqual(0, controller.Side.MiddleTexture);
    }

    [TestMethod]
    public void NonZeroTagAffectsAllMatchingLinesAndConsumesMapSelector()
    {
        var controller = CreateLine(260, 77, -1, "TRANMAP");
        var target = CreateLine(0, 77, 14, "GRATE");
        var unrelated = CreateLine(0, 78, 15, "OTHER");

        BoomTranslucentLineResolver.Apply(
            new[] { controller.Line, target.Line, unrelated.Line },
            GameCompatibility.Boom);

        Assert.AreEqual("TRANMAP", controller.Line.TranslucencyMapName);
        Assert.AreEqual("TRANMAP", target.Line.TranslucencyMapName);
        Assert.IsNull(unrelated.Line.TranslucencyMapName);
        Assert.AreEqual(0, controller.Side.MiddleTexture);
    }

    [TestMethod]
    public void NonZeroTagRetainsCustomMapName()
    {
        var controller = CreateLine(260, 11, -1, "MYTRANS");
        var target = CreateLine(0, 11, 14, "GRATE");

        BoomTranslucentLineResolver.Apply(
            new[] { controller.Line, target.Line },
            GameCompatibility.Boom);

        Assert.AreEqual("MYTRANS", target.Line.TranslucencyMapName);
        Assert.AreEqual(0, controller.Side.MiddleTexture);
    }


    [TestMethod]
    public void TagZeroUsesValidCustomLumpInProductionLookup()
    {
        var path = WriteWad(("MYTRANS", new byte[BoomTranslucencyMapLookup.TableSize]));

        try
        {
            using var wad = new Wad(path);
            var maps = new BoomTranslucencyMapLookup(wad);
            var controller = CreateLine(260, 0, 12, "MYTRANS");
            var other = CreateLine(0, 0, 13, "OTHER");

            BoomTranslucentLineResolver.Apply(
                new[] { controller.Line, other.Line },
                GameCompatibility.Boom,
                maps);

            Assert.AreEqual("MYTRANS", controller.Line.TranslucencyMapName);
            Assert.IsNull(other.Line.TranslucencyMapName);
            Assert.AreEqual(0, controller.Side.MiddleTexture);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void Valid64KCustomLumpWinsEvenWhenSelectorIsAlsoAWallTexture()
    {
        var path = WriteWad(("BOTHNAME", new byte[BoomTranslucencyMapLookup.TableSize]));

        try
        {
            using var wad = new Wad(path);
            var maps = new BoomTranslucencyMapLookup(wad);
            var controller = CreateLine(260, 17, 12, "BOTHNAME");
            var target = CreateLine(0, 17, 14, "GRATE");

            BoomTranslucentLineResolver.Apply(
                new[] { controller.Line, target.Line },
                GameCompatibility.Boom,
                maps);

            Assert.AreEqual("BOTHNAME", target.Line.TranslucencyMapName);
            Assert.AreEqual(0, controller.Side.MiddleTexture);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void InvalidCustomSelectorFallsBackToDefaultAndKeepsRealWallTexture()
    {
        var path = WriteWad(("BOTHNAME", new byte[123]));

        try
        {
            using var wad = new Wad(path);
            var maps = new BoomTranslucencyMapLookup(wad);
            var controller = CreateLine(260, 17, 12, "BOTHNAME");
            var target = CreateLine(0, 17, 14, "GRATE");

            BoomTranslucentLineResolver.Apply(
                new[] { controller.Line, target.Line },
                GameCompatibility.Boom,
                maps);

            Assert.AreEqual(BoomTranslucentLineResolver.DefaultMapName, target.Line.TranslucencyMapName);
            Assert.AreEqual(12, controller.Side.MiddleTexture);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LaterControllerWinsWhenSeveralControlsShareATag()
    {
        var first = CreateLine(260, 42, 12, "MIDTEX");
        var target = CreateLine(0, 42, 14, "GRATE");
        var second = CreateLine(260, 42, -1, "SECOND");

        BoomTranslucentLineResolver.Apply(
            new[] { first.Line, target.Line, second.Line },
            GameCompatibility.Boom);

        Assert.AreEqual("SECOND", first.Line.TranslucencyMapName);
        Assert.AreEqual("SECOND", target.Line.TranslucencyMapName);
        Assert.AreEqual("SECOND", second.Line.TranslucencyMapName);
    }

    [TestMethod]
    public void VanillaClearsResolvedStateWithoutConsumingSelectorTexture()
    {
        var controller = CreateLine(260, 0, -1, "TRANMAP");
        controller.Line.TranslucencyMapName = "OLD";

        BoomTranslucentLineResolver.Apply(
            new[] { controller.Line },
            GameCompatibility.Vanilla);

        Assert.IsNull(controller.Line.TranslucencyMapName);
        Assert.AreEqual(-1, controller.Side.MiddleTexture);
    }

    [TestMethod]
    public void DefaultIndexedMapUsesBoom66PercentForegroundAndBoomLayout()
    {
        var palette = CreateGrayPalette();
        var map = BoomTranslucencyMap.BuildDefaultIndexed(palette);

        // Boom table layout is [background << 8 | foreground].
        Assert.AreEqual((byte)168, map[(0 << 8) | 255]);
        Assert.AreEqual((byte)87, map[(255 << 8) | 0]);
        Assert.AreEqual((byte)64, map[(64 << 8) | 64]);
    }

    [TestMethod]
    public void TrueColorFallbackUsesSameBoom66PercentWeight()
    {
        var foreground = Pack(200, 100, 40);
        var background = Pack(20, 60, 100);

        var blended = BoomTranslucencyMap.BlendDefault(foreground, background);

        Assert.AreEqual(Pack(139, 86, 60), blended);
    }

    private static (LineDef Line, SideDef Side) CreateLine(
        int special,
        short tag,
        int middleTexture,
        string middleTextureName)
    {
        var sector = new Sector(
            0,
            Fixed.Zero,
            Fixed.FromInt(128),
            0,
            0,
            128,
            0,
            0);

        var side = new SideDef(
            Fixed.Zero,
            Fixed.Zero,
            0,
            0,
            middleTexture,
            sector,
            "-",
            "-",
            middleTextureName);

        var line = new LineDef(
            new Vertex(Fixed.Zero, Fixed.Zero),
            new Vertex(Fixed.FromInt(64), Fixed.Zero),
            (LineFlags)0,
            (LineSpecial)special,
            tag,
            side,
            null);

        return (line, side);
    }

    private static string WriteWad(params (string Name, byte[] Data)[] lumps)
    {
        var path = Path.Combine(Path.GetTempPath(), $"manageddoom-boom-translucent-line-{Guid.NewGuid():N}.wad");
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

    private static uint[] CreateGrayPalette()
    {
        var result = new uint[256];
        for (var i = 0; i < 256; i++)
            result[i] = Pack(i, i, i);

        return result;
    }

    private static uint Pack(int r, int g, int b)
    {
        return (uint)(r | (g << 8) | (b << 16) | (255 << 24));
    }
}
