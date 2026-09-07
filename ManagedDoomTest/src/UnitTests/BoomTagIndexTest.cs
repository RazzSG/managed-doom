using ManagedDoom;
using ManagedDoom.Compatibility.Boom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomTagIndexTest
{
    [TestMethod]
    public void IndexPreservesMapOrderAndFindsTaggedSectorsAndLines()
    {
        var sectors = new[]
        {
            CreateSector(0, 7),
            CreateSector(1, 3),
            CreateSector(2, 7),
            CreateSector(3, -1)
        };

        var lines = new[]
        {
            CreateLine(7),
            CreateLine(3),
            CreateLine(7)
        };

        var index = new BoomTagIndex(sectors, lines);
        var taggedSectors = index.GetSectors(7);
        var taggedLines = index.GetLines(7);

        Assert.AreEqual(2, taggedSectors.Length);
        Assert.AreSame(sectors[0], taggedSectors[0]);
        Assert.AreSame(sectors[2], taggedSectors[1]);
        Assert.AreEqual(2, taggedLines.Length);
        Assert.AreSame(lines[0], taggedLines[0]);
        Assert.AreSame(lines[2], taggedLines[1]);
        Assert.AreEqual(0, index.GetSectors(999).Length);
        Assert.AreEqual(0, index.GetLines(999).Length);

        Assert.AreEqual(0, index.FindNextSectorNumber(7, -1));
        Assert.AreEqual(2, index.FindNextSectorNumber(7, 0));
        Assert.AreEqual(-1, index.FindNextSectorNumber(7, 2));
    }

    private static Sector CreateSector(int number, int tag)
    {
        return new Sector(number, Fixed.Zero, Fixed.FromInt(128), 0, 0, 160, (SectorSpecial)0, tag);
    }

    private static LineDef CreateLine(int tag)
    {
        var vertex1 = new Vertex(Fixed.Zero, Fixed.Zero);
        var vertex2 = new Vertex(Fixed.One, Fixed.Zero);
        return new LineDef(vertex1, vertex2, (LineFlags)0, (LineSpecial)0, (short)tag, null, null);
    }
}
