using System;
using System.IO;
using System.Text;
using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomColorMapTest
{
    [TestMethod]
    public void ColorMapLoadsBoomNamespaceAndWaterMap()
    {
        var path = WriteWad(
            ("COLORMAP", CreateIdentityColorMap()),
            ("C_START", Array.Empty<byte>()),
            ("TINTMAP", CreateShiftColorMap(17)),
            ("BADMAP", new byte[256]),
            ("C_END", Array.Empty<byte>()),
            ("OUTSIDE", CreateShiftColorMap(31)),
            ("WATERMAP", CreateShiftColorMap(7)));

        try
        {
            using var wad = new Wad(path);
            var colorMap = new ColorMap(wad);

            Assert.IsTrue(colorMap.TryGetSet("TINTMAP", out var tint));
            Assert.AreEqual((15 + 17) & 255, (int)tint[3][15]);

            Assert.IsTrue(colorMap.TryGetSet("WATERMAP", out var water));
            Assert.AreEqual((200 + 7) & 255, (int)water[12][200]);

            Assert.IsFalse(colorMap.TryGetSet("BADMAP", out _));
            Assert.IsFalse(colorMap.TryGetSet("OUTSIDE", out _));

            Assert.IsTrue(colorMap.TryGetSet("COLORMAP", out var defaultSet));
            Assert.AreSame(colorMap.DefaultSet, defaultSet);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LaterBoomColormapWithSameNameWins()
    {
        var path = WriteWad(
            ("COLORMAP", CreateIdentityColorMap()),
            ("C_START", Array.Empty<byte>()),
            ("DUPMAP", CreateShiftColorMap(3)),
            ("C_END", Array.Empty<byte>()),
            ("C_START", Array.Empty<byte>()),
            ("DUPMAP", CreateShiftColorMap(11)),
            ("C_END", Array.Empty<byte>()));

        try
        {
            using var wad = new Wad(path);
            var colorMap = new ColorMap(wad);

            Assert.IsTrue(colorMap.TryGetSet("DUPMAP", out var set));
            Assert.AreEqual((100 + 11) & 255, (int)set[5][100]);
        }
        finally
        {
            File.Delete(path);
        }
    }


    [TestMethod]
    public void ColormapNamespaceDoesNotLeakAcrossWadFiles()
    {
        var first = WriteWad(
            ("COLORMAP", CreateIdentityColorMap()),
            ("C_START", Array.Empty<byte>()));
        var second = WriteWad(
            ("OUTSIDE", CreateShiftColorMap(23)));

        try
        {
            using var wad = new Wad(first, second);
            var colorMap = new ColorMap(wad);

            Assert.IsFalse(colorMap.TryGetSet("OUTSIDE", out _));
        }
        finally
        {
            File.Delete(first);
            File.Delete(second);
        }
    }

    [TestMethod]
    public void TrueColorBoomColormapUsesExactPaletteRemap()
    {
        var path = WriteWad(
            ("PLAYPAL", CreatePalette()),
            ("COLORMAP", CreateIdentityColorMap()),
            ("C_START", Array.Empty<byte>()),
            ("TINTMAP", CreateShiftColorMap(1)),
            ("C_END", Array.Empty<byte>()));

        try
        {
            using var wad = new Wad(path);
            var palette = new Palette(wad);
            palette.ResetColors(1.0);

            var indexed = new ColorMap(wad);
            var trueColor = new TrueColorMap(palette, indexed);
            trueColor.Rebuild();

            Assert.IsTrue(trueColor.TryGetSet("TINTMAP", out var set));

            const int sourceIndex = 37;
            Assert.AreEqual(palette[0][sourceIndex + 1], set[8][sourceIndex]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void TransferHeightChoosesMiddleBottomAndTopColormapNames()
    {
        var viewSector = CreateSector(0);
        var control = CreateSector(1);
        var side = new SideDef(
            Fixed.Zero,
            Fixed.Zero,
            -1,
            -1,
            -1,
            control,
            "TOPMAP",
            "BOTMAP",
            "MIDMAP");

        var line = new LineDef(
            new Vertex(Fixed.Zero, Fixed.Zero),
            new Vertex(Fixed.FromInt(64), Fixed.Zero),
            (LineFlags)0,
            (LineSpecial)BoomTransferHeightResolver.TransferHeightsSpecial,
            1,
            side,
            null);

        viewSector.HeightSector = control;
        viewSector.HeightSectorLine = line;

        Assert.AreEqual(
            "MIDMAP",
            BoomTransferHeightResolver.ResolveColorMapName(viewSector, BoomTransferHeightZone.Normal));
        Assert.AreEqual(
            "BOTMAP",
            BoomTransferHeightResolver.ResolveColorMapName(viewSector, BoomTransferHeightZone.BelowFakeFloor));
        Assert.AreEqual(
            "TOPMAP",
            BoomTransferHeightResolver.ResolveColorMapName(viewSector, BoomTransferHeightZone.AboveFakeCeiling));
    }

    [TestMethod]
    public void ValidWallTextureNameFallsBackToDefaultColormap()
    {
        var viewSector = CreateSector(0);
        var control = CreateSector(1);
        var side = new SideDef(
            Fixed.Zero,
            Fixed.Zero,
            -1,
            -1,
            12,
            control,
            "TOPMAP",
            "BOTMAP",
            "MIDTEX");

        var line = new LineDef(
            new Vertex(Fixed.Zero, Fixed.Zero),
            new Vertex(Fixed.FromInt(64), Fixed.Zero),
            (LineFlags)0,
            (LineSpecial)BoomTransferHeightResolver.TransferHeightsSpecial,
            1,
            side,
            null);

        viewSector.HeightSector = control;
        viewSector.HeightSectorLine = line;

        Assert.IsNull(BoomTransferHeightResolver.ResolveColorMapName(
            viewSector,
            BoomTransferHeightZone.Normal));
    }

    private static Sector CreateSector(int number)
    {
        return new Sector(
            number,
            Fixed.Zero,
            Fixed.FromInt(128),
            0,
            0,
            128,
            0,
            0);
    }

    private static byte[] CreateIdentityColorMap()
    {
        var data = new byte[34 * 256];

        for (var map = 0; map < 34; map++)
        {
            for (var i = 0; i < 256; i++)
            {
                data[map * 256 + i] = (byte)i;
            }
        }

        return data;
    }

    private static byte[] CreateShiftColorMap(int shift)
    {
        var data = new byte[34 * 256];

        for (var map = 0; map < 34; map++)
        {
            for (var i = 0; i < 256; i++)
            {
                data[map * 256 + i] = (byte)((i + shift) & 255);
            }
        }

        return data;
    }

    private static byte[] CreatePalette()
    {
        var data = new byte[256 * 3];

        for (var i = 0; i < 256; i++)
        {
            data[3 * i] = (byte)i;
            data[3 * i + 1] = (byte)(255 - i);
            data[3 * i + 2] = (byte)((i * 3) & 255);
        }

        return data;
    }

    private static string WriteWad(params (string Name, byte[] Data)[] lumps)
    {
        var path = Path.Combine(Path.GetTempPath(), $"manageddoom-boom-colormap-{Guid.NewGuid():N}.wad");
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
}
