using System;
using ManagedDoom;
using ManagedDoom.Video;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class SegRenderGeometryResolverTest
{
    [TestMethod]
    public void ProjectsSplitVertexBackOntoSourceLinedef()
    {
        // Real 1monster.wad MAP01 geometry from linedef 665:
        // linedef (93,204) -> (80,208), BSP split vertex (84,207).
        // The split point is slightly off the source linedef.
        var a = new Vertex(Fixed.FromInt(93), Fixed.FromInt(204));
        var b = new Vertex(Fixed.FromInt(80), Fixed.FromInt(208));
        var split = new Vertex(Fixed.FromInt(84), Fixed.FromInt(207));

        var sector = new Sector(
            0,
            Fixed.Zero,
            Fixed.FromInt(128),
            0,
            0,
            160,
            (SectorSpecial)0,
            0);

        var side = new SideDef(
            Fixed.Zero,
            Fixed.Zero,
            0,
            0,
            0,
            sector);

        var line = new LineDef(
            a,
            b,
            LineFlags.TwoSided,
            (LineSpecial)0,
            0,
            side,
            side);

        var seg = new Seg(
            split,
            a,
            Fixed.Zero,
            Geometry.PointToAngle(split.X, split.Y, a.X, a.Y),
            side,
            line,
            sector,
            sector);

        var before = DistanceToLine(split.X, split.Y, line);
        var resolved = SegRenderGeometryResolver.Resolve(seg);
        var after = DistanceToLine(resolved.X1, resolved.Y1, line);

        Assert.IsTrue(resolved.Corrected);
        Assert.IsTrue(after < before);
        Assert.IsTrue(after < 0.001);

        // Original map/gameplay vertex remains untouched.
        Assert.AreEqual(Fixed.FromInt(84).Data, split.X.Data);
        Assert.AreEqual(Fixed.FromInt(207).Data, split.Y.Data);
    }

    [TestMethod]
    public void ProjectsSplitVertexForVanillaMapBecauseCorrectionIsRendererOnly()
    {
        // Real NUTS3.WAD MAP01 geometry from sector 78 / linedef 842:
        // linedef (-4095,-1097) -> (-4078,-1068), BSP split vertex (-4093,-1094).
        // NUTS3 has no Boom markers and resolves to Vanilla in Auto mode, but the
        // node-builder rounding error is still a renderer problem and must be fixed.
        var a = new Vertex(Fixed.FromInt(-4095), Fixed.FromInt(-1097));
        var b = new Vertex(Fixed.FromInt(-4078), Fixed.FromInt(-1068));
        var split = new Vertex(Fixed.FromInt(-4093), Fixed.FromInt(-1094));

        var sector = new Sector(
            0,
            Fixed.FromInt(-1020),
            Fixed.FromInt(2000),
            0,
            0,
            255,
            (SectorSpecial)0,
            0);

        var side = new SideDef(
            Fixed.Zero,
            Fixed.Zero,
            0,
            0,
            0,
            sector);

        var line = new LineDef(
            a,
            b,
            LineFlags.TwoSided,
            (LineSpecial)0,
            0,
            side,
            side);

        var seg = new Seg(
            split,
            b,
            Fixed.Zero,
            Geometry.PointToAngle(split.X, split.Y, b.X, b.Y),
            side,
            line,
            sector,
            sector);

        var before = DistanceToLine(split.X, split.Y, line);
        var resolved = SegRenderGeometryResolver.Resolve(seg);
        var after = DistanceToLine(resolved.X1, resolved.Y1, line);

        Assert.IsTrue(resolved.Corrected);
        Assert.IsTrue(after < before);
        Assert.IsTrue(after < 0.001);

        // Map/gameplay geometry is untouched.
        Assert.AreEqual(Fixed.FromInt(-4093).Data, split.X.Data);
        Assert.AreEqual(Fixed.FromInt(-1094).Data, split.Y.Data);
    }

    private static double DistanceToLine(Fixed x, Fixed y, LineDef line)
    {
        var ax = (double)line.Vertex1.X.Data;
        var ay = (double)line.Vertex1.Y.Data;
        var bx = (double)line.Vertex2.X.Data;
        var by = (double)line.Vertex2.Y.Data;
        var px = (double)x.Data;
        var py = (double)y.Data;

        var dx = bx - ax;
        var dy = by - ay;
        var length = Math.Sqrt(dx * dx + dy * dy);

        return Math.Abs(dy * (px - ax) - dx * (py - ay)) / length / Fixed.FromInt(1).Data;
    }
}
