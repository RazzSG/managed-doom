using System;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Rendering;

/// <summary>
/// Resolves renderer-only seg geometry for Boom-family maps.
///
/// Classic BSP node builders can introduce split vertices that are a fraction of
/// a map unit away from the linedef they belong to. Modern Boom-derived ports
/// project those renderer vertices back onto the source linedef to suppress
/// slime-trail / plane-seam artifacts without changing gameplay geometry.
/// </summary>
public static class BoomSegRenderGeometryResolver
{
    private const int MaxCorrectionUnits = 8;

    public static BoomSegRenderGeometry Resolve(
        Seg seg,
        GameCompatibility compatibility)
    {
        if (seg == null)
            throw new ArgumentNullException(nameof(seg));

        if (!GameCompatibilityFeatures.SupportsBoom(compatibility) ||
            seg.LineDef == null)
        {
            return Original(seg);
        }

        var line = seg.LineDef;

        ResolveVertex(seg.Vertex1, line, out var x1, out var y1, out var corrected1);
        ResolveVertex(seg.Vertex2, line, out var x2, out var y2, out var corrected2);

        var corrected = corrected1 || corrected2;
        if (!corrected)
            return Original(seg);

        var angle = x1 == x2 && y1 == y2
            ? seg.Angle
            : Geometry.PointToAngle(x1, y1, x2, y2);

        return new BoomSegRenderGeometry(
            x1,
            y1,
            x2,
            y2,
            angle,
            corrected: true);
    }

    private static BoomSegRenderGeometry Original(Seg seg) =>
        new(
            seg.Vertex1.X,
            seg.Vertex1.Y,
            seg.Vertex2.X,
            seg.Vertex2.Y,
            seg.Angle,
            corrected: false);

    private static void ResolveVertex(
        Vertex vertex,
        LineDef line,
        out Fixed x,
        out Fixed y,
        out bool corrected)
    {
        x = vertex.X;
        y = vertex.Y;
        corrected = false;

        // Real linedef endpoints are already exact and must not move.
        if (ReferenceEquals(vertex, line.Vertex1) ||
            ReferenceEquals(vertex, line.Vertex2))
        {
            return;
        }

        var ax = (double)line.Vertex1.X.Data;
        var ay = (double)line.Vertex1.Y.Data;
        var bx = (double)line.Vertex2.X.Data;
        var by = (double)line.Vertex2.Y.Data;
        var px = (double)vertex.X.Data;
        var py = (double)vertex.Y.Data;

        var dx = bx - ax;
        var dy = by - ay;
        var lengthSquared = dx * dx + dy * dy;
        if (lengthSquared <= 0)
            return;

        var t = ((px - ax) * dx + (py - ay) * dy) / lengthSquared;
        var projectedX = ax + t * dx;
        var projectedY = ay + t * dy;

        var correctionX = projectedX - px;
        var correctionY = projectedY - py;
        var correctionSquared =
            correctionX * correctionX +
            correctionY * correctionY;

        var maxCorrection = (double)Fixed.FromInt(MaxCorrectionUnits).Data;
        if (correctionSquared > maxCorrection * maxCorrection)
            return;

        x = new Fixed((int)Math.Clamp(
            Math.Round(projectedX),
            int.MinValue,
            int.MaxValue));

        y = new Fixed((int)Math.Clamp(
            Math.Round(projectedY),
            int.MinValue,
            int.MaxValue));

        corrected = x != vertex.X || y != vertex.Y;
    }
}

public readonly struct BoomSegRenderGeometry
{
    public BoomSegRenderGeometry(
        Fixed x1,
        Fixed y1,
        Fixed x2,
        Fixed y2,
        Angle angle,
        bool corrected)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        Angle = angle;
        Corrected = corrected;
    }

    public Fixed X1 { get; }
    public Fixed Y1 { get; }
    public Fixed X2 { get; }
    public Fixed Y2 { get; }
    public Angle Angle { get; }
    public bool Corrected { get; }
}
