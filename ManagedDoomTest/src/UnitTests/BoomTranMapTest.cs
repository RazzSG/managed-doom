using System;
using System.IO;
using System.Text;
using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomTranMapTest
{
    [TestMethod]
    public void ValidTranMapIsLoadedByteForByte()
    {
        var expected = CreateTranMap(17);
        var path = WriteWad(("TRANMAP", expected));

        try
        {
            using var wad = new Wad(path);
            var lookup = new BoomTranslucencyMapLookup(wad);

            Assert.IsTrue(lookup.HasDefaultMap);
            Assert.IsNotNull(lookup.DefaultMap);
            Assert.AreEqual(BoomTranslucencyMapLookup.TableSize, lookup.DefaultMap.Length);
            CollectionAssert.AreEqual(expected, lookup.DefaultMap);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LaterWadTranMapOverridesEarlierOne()
    {
        var first = WriteWad(("TRANMAP", CreateTranMap(3)));
        var secondData = CreateTranMap(29);
        var second = WriteWad(("TRANMAP", secondData));

        try
        {
            using var wad = new Wad(first, second);
            var lookup = new BoomTranslucencyMapLookup(wad);

            Assert.IsTrue(lookup.HasDefaultMap);
            CollectionAssert.AreEqual(secondData, lookup.DefaultMap);
        }
        finally
        {
            File.Delete(first);
            File.Delete(second);
        }
    }

    [TestMethod]
    public void InvalidWinningTranMapIsRejectedInsteadOfUsingEarlierLump()
    {
        var first = WriteWad(("TRANMAP", CreateTranMap(5)));
        var second = WriteWad(("TRANMAP", new byte[BoomTranslucencyMapLookup.TableSize - 1]));

        try
        {
            using var wad = new Wad(first, second);
            var lookup = new BoomTranslucencyMapLookup(wad);

            Assert.IsFalse(lookup.HasDefaultMap);
            Assert.IsNull(lookup.DefaultMap);
        }
        finally
        {
            File.Delete(first);
            File.Delete(second);
        }
    }

    [TestMethod]
    public void MissingTranMapLeavesDefaultMapUnsetForGeneratedFallback()
    {
        var path = WriteWad(("DUMMY", Array.Empty<byte>()));

        try
        {
            using var wad = new Wad(path);
            var lookup = new BoomTranslucencyMapLookup(wad);

            Assert.IsFalse(lookup.HasDefaultMap);
            Assert.IsNull(lookup.DefaultMap);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LoadedTranMapPreservesBoomBackgroundForegroundLayout()
    {
        var data = new byte[BoomTranslucencyMapLookup.TableSize];
        data[(7 << 8) | 19] = 123;
        data[(19 << 8) | 7] = 231;
        var path = WriteWad(("TRANMAP", data));

        try
        {
            using var wad = new Wad(path);
            var lookup = new BoomTranslucencyMapLookup(wad);

            Assert.AreEqual((byte)123, lookup.DefaultMap[(7 << 8) | 19]);
            Assert.AreEqual((byte)231, lookup.DefaultMap[(19 << 8) | 7]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static byte[] CreateTranMap(int seed)
    {
        var data = new byte[BoomTranslucencyMapLookup.TableSize];
        for (var background = 0; background < 256; background++)
        {
            for (var foreground = 0; foreground < 256; foreground++)
            {
                data[(background << 8) | foreground] =
                    (byte)((background * 3 + foreground * 5 + seed) & 255);
            }
        }

        return data;
    }

    private static string WriteWad(params (string Name, byte[] Data)[] lumps)
    {
        var path = Path.Combine(Path.GetTempPath(), $"manageddoom-boom-tranmap-{Guid.NewGuid():N}.wad");
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
