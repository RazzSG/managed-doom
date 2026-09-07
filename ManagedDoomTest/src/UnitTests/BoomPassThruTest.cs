using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomPassThruTest
{
    [TestMethod]
    public void PassThruUsesBoomBitNine()
    {
        Assert.AreEqual(0x0200, (int)LineFlags.PassThru);
    }

    [TestMethod]
    public void LineDefParserPreservesPassThruFlag()
    {
        var data = new byte[14];
        data[2] = 1; // vertex2 index
        data[4] = 0x00;
        data[5] = 0x02; // 0x0200 little-endian
        data[10] = 0;
        data[11] = 0;
        data[12] = 0xFF;
        data[13] = 0xFF;

        var vertices = new[]
        {
            new Vertex(Fixed.Zero, Fixed.Zero),
            new Vertex(Fixed.FromInt(64), Fixed.Zero)
        };
        var sector = new Sector(0, Fixed.Zero, Fixed.FromInt(128), 0, 0, 160, (SectorSpecial)0, 0);
        var sides = new[] { new SideDef(Fixed.Zero, Fixed.Zero, 0, 0, 0, sector) };

        var line = LineDef.FromData(data, 0, vertices, sides);

        Assert.IsTrue((line.Flags & LineFlags.PassThru) != 0);
    }

    [TestMethod]
    public void BoomPassThruContinuesUseTraversal()
    {
        var line = CreateLine(LineFlags.PassThru);

        Assert.IsTrue(BoomUseTraversal.ShouldContinueAfterActivation(line, GameCompatibility.Boom));
        Assert.IsTrue(BoomUseTraversal.ShouldContinueAfterActivation(line, GameCompatibility.Mbf));
        Assert.IsTrue(BoomUseTraversal.ShouldContinueAfterActivation(line, GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void BoomLineWithoutPassThruStopsUseTraversal()
    {
        var line = CreateLine(0);

        Assert.IsFalse(BoomUseTraversal.ShouldContinueAfterActivation(line, GameCompatibility.Boom));
    }

    [TestMethod]
    public void VanillaIgnoresPassThruFlag()
    {
        var line = CreateLine(LineFlags.PassThru);

        Assert.IsFalse(BoomUseTraversal.ShouldContinueAfterActivation(line, GameCompatibility.Vanilla));
    }

    [TestMethod]
    public void OtherLinedefFlagsDoNotEnablePassThru()
    {
        var line = CreateLine(LineFlags.Mapped | LineFlags.TwoSided);

        Assert.IsFalse(BoomUseTraversal.ShouldContinueAfterActivation(line, GameCompatibility.Boom));
    }

    [TestMethod]
    public void NullLineNeverContinuesTraversal()
    {
        Assert.IsFalse(BoomUseTraversal.ShouldContinueAfterActivation(null, GameCompatibility.Boom));
    }

    private static LineDef CreateLine(LineFlags flags)
    {
        var sector = new Sector(0, Fixed.Zero, Fixed.FromInt(128), 0, 0, 160, (SectorSpecial)0, 0);
        var side = new SideDef(Fixed.Zero, Fixed.Zero, 0, 0, 0, sector);

        return new LineDef(
            new Vertex(Fixed.Zero, Fixed.Zero),
            new Vertex(Fixed.FromInt(64), Fixed.Zero),
            flags,
            (LineSpecial)1,
            0,
            side,
            null);
    }
}
