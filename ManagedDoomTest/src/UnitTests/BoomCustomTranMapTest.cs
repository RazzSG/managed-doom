using System;
using System.IO;
using System.Text;
using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomCustomTranMapTest
{
    [TestMethod]
    public void Named64KLumpOverridesDefaultTranMap()
    {
        var defaultMap = CreateMap(3);
        var customMap = CreateMap(17);
        var path = WriteWad(("TRANMAP", defaultMap), ("MYTRANS", customMap));

        try
        {
            using var wad = new Wad(path);
            var lookup = new BoomTranslucencyMapLookup(wad);

            var resolved = lookup.ResolveMap("MYTRANS");

            Assert.AreSame(resolved, lookup.ResolveMap("MYTRANS"));
            CollectionAssert.AreEqual(customMap, resolved);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LaterWadCustomMapOverridesEarlierOne()
    {
        var first = WriteWad(("MYTRANS", CreateMap(5)));
        var expected = CreateMap(29);
        var second = WriteWad(("MYTRANS", expected));

        try
        {
            using var wad = new Wad(first, second);
            var lookup = new BoomTranslucencyMapLookup(wad);

            CollectionAssert.AreEqual(expected, lookup.ResolveMap("MYTRANS"));
        }
        finally
        {
            File.Delete(first);
            File.Delete(second);
        }
    }

    [TestMethod]
    public void InvalidWinningCustomMapFallsBackToDefaultInsteadOfEarlierCustom()
    {
        var defaultMap = CreateMap(7);
        var first = WriteWad(("TRANMAP", defaultMap), ("MYTRANS", CreateMap(11)));
        var second = WriteWad(("MYTRANS", new byte[BoomTranslucencyMapLookup.TableSize - 1]));

        try
        {
            using var wad = new Wad(first, second);
            var lookup = new BoomTranslucencyMapLookup(wad);

            Assert.AreSame(lookup.DefaultMap, lookup.ResolveMap("MYTRANS"));
            CollectionAssert.AreEqual(defaultMap, lookup.ResolveMap("MYTRANS"));
        }
        finally
        {
            File.Delete(first);
            File.Delete(second);
        }
    }

    [TestMethod]
    public void MissingCustomMapFallsBackToDefaultTranMap()
    {
        var defaultMap = CreateMap(13);
        var path = WriteWad(("TRANMAP", defaultMap));

        try
        {
            using var wad = new Wad(path);
            var lookup = new BoomTranslucencyMapLookup(wad);

            Assert.AreSame(lookup.DefaultMap, lookup.ResolveMap("MISSING"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void InvalidCustomAndMissingDefaultReturnNullForGeneratedFallback()
    {
        var path = WriteWad(("MYTRANS", new byte[123]));

        try
        {
            using var wad = new Wad(path);
            var lookup = new BoomTranslucencyMapLookup(wad);

            Assert.IsNull(lookup.ResolveMap("MYTRANS"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void CustomLookupIsCaseInsensitiveLikeDoomResourceNames()
    {
        var expected = CreateMap(31);
        var path = WriteWad(("MYTRANS", expected));

        try
        {
            using var wad = new Wad(path);
            var lookup = new BoomTranslucencyMapLookup(wad);

            CollectionAssert.AreEqual(expected, lookup.ResolveMap("mytrans"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void CustomMapPreservesBoomBackgroundForegroundLayout()
    {
        var custom = new byte[BoomTranslucencyMapLookup.TableSize];
        custom[(9 << 8) | 27] = 101;
        custom[(27 << 8) | 9] = 202;
        var path = WriteWad(("MYTRANS", custom));

        try
        {
            using var wad = new Wad(path);
            var lookup = new BoomTranslucencyMapLookup(wad);
            var resolved = lookup.ResolveMap("MYTRANS");

            Assert.AreEqual((byte)101, resolved[(9 << 8) | 27]);
            Assert.AreEqual((byte)202, resolved[(27 << 8) | 9]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static byte[] CreateMap(int seed)
    {
        var data = new byte[BoomTranslucencyMapLookup.TableSize];
        for (var background = 0; background < 256; background++)
        {
            for (var foreground = 0; foreground < 256; foreground++)
            {
                data[(background << 8) | foreground] =
                    (byte)((background * 7 + foreground * 11 + seed) & 255);
            }
        }

        return data;
    }

    private static string WriteWad(params (string Name, byte[] Data)[] lumps)
    {
        var path = Path.Combine(Path.GetTempPath(), $"manageddoom-boom-custom-tranmap-{Guid.NewGuid():N}.wad");
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
