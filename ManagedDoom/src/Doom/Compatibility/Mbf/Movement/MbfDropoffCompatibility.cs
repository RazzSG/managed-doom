using System;
using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.Movement;

/// <summary>
/// MBF comp_dropoff compatibility and dropoff-recovery behavior.
/// With the flag enabled MBF keeps Doom's old rule that ground actors cannot
/// be pushed over tall ledges. With it disabled, momentum/slide movement may
/// cross the ledge while voluntary monster movement remains constrained.
/// Monsters already hanging over a tall ledge also choose a short direction
/// away from the contacted dropoff when selecting a new chase direction.
/// </summary>
public sealed class MbfDropoffCompatibility
{
    public static readonly Fixed MaxStep = Fixed.FromInt(24);

    private readonly World world;
    private readonly Func<LineDef, bool> checkLine;

    private Fixed boxLeft;
    private Fixed boxRight;
    private Fixed boxTop;
    private Fixed boxBottom;
    private Fixed floorZ;
    private Fixed deltaX;
    private Fixed deltaY;

    public MbfDropoffCompatibility(World world)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        checkLine = CheckLine;
    }

    public static bool UsesClassicBlocking(
        GameCompatibility compatibility,
        bool compDropoff)
    {
        return GameCompatibilityFeatures.SupportsMbfDropoffCompatibility(compatibility) &&
               compDropoff;
    }

    public static bool AllowsExternalMomentumDropoff(
        GameCompatibility compatibility,
        bool compDropoff,
        bool requested)
    {
        return requested &&
               GameCompatibilityFeatures.SupportsMbfDropoffCompatibility(compatibility) &&
               !compDropoff;
    }

    public static bool ShouldRecoverFromDropoff(
        GameCompatibility compatibility,
        bool compDropoff,
        Mobj actor)
    {
        return actor != null &&
               GameCompatibilityFeatures.SupportsMbfDropoffCompatibility(compatibility) &&
               !compDropoff &&
               actor.FloorZ - actor.DropoffZ > MaxStep &&
               actor.Z <= actor.FloorZ &&
               (actor.Flags & (MobjFlags.DropOff | MobjFlags.Float)) == 0;
    }

    public bool TryGetAvoidanceDelta(
        bool compDropoff,
        Mobj actor,
        out Fixed avoidX,
        out Fixed avoidY)
    {
        avoidX = Fixed.Zero;
        avoidY = Fixed.Zero;

        if (!ShouldRecoverFromDropoff(
                world.Options.Compatibility,
                compDropoff,
                actor))
        {
            return false;
        }

        boxLeft = actor.X - actor.Radius;
        boxRight = actor.X + actor.Radius;
        boxBottom = actor.Y - actor.Radius;
        boxTop = actor.Y + actor.Radius;
        floorZ = actor.Z;
        deltaX = Fixed.Zero;
        deltaY = Fixed.Zero;

        var blockMap = world.Map.BlockMap;
        var blockX1 = blockMap.GetBlockX(boxLeft);
        var blockX2 = blockMap.GetBlockX(boxRight);
        var blockY1 = blockMap.GetBlockY(boxBottom);
        var blockY2 = blockMap.GetBlockY(boxTop);
        var validCount = world.GetNewValidCount();

        for (var bx = blockX1; bx <= blockX2; bx++)
        {
            for (var by = blockY1; by <= blockY2; by++)
            {
                blockMap.IterateLines(bx, by, checkLine, validCount);
            }
        }

        avoidX = deltaX;
        avoidY = deltaY;
        return avoidX != Fixed.Zero || avoidY != Fixed.Zero;
    }

    private bool CheckLine(LineDef line)
    {
        if (TryGetLineAvoidanceDelta(
                line,
                floorZ,
                boxLeft,
                boxRight,
                boxBottom,
                boxTop,
                out var lineDeltaX,
                out var lineDeltaY))
        {
            // Multiple contacted dropoff lines accumulate, including corners.
            deltaX += lineDeltaX;
            deltaY += lineDeltaY;
        }

        return true;
    }

    public static bool TryGetLineAvoidanceDelta(
        LineDef line,
        Fixed actorFloorZ,
        Fixed left,
        Fixed right,
        Fixed bottom,
        Fixed top,
        out Fixed avoidX,
        out Fixed avoidY)
    {
        avoidX = Fixed.Zero;
        avoidY = Fixed.Zero;

        if (line?.BackSector == null ||
            right <= line.BoundingBox[Box.Left] ||
            left >= line.BoundingBox[Box.Right] ||
            top <= line.BoundingBox[Box.Bottom] ||
            bottom >= line.BoundingBox[Box.Top] ||
            !BoxCrossesLine(left, right, bottom, top, line))
        {
            return false;
        }

        var front = line.FrontSector.FloorHeight;
        var back = line.BackSector.FloorHeight;
        Angle angle;

        if (back == actorFloorZ && front < actorFloorZ - MaxStep)
        {
            angle = Geometry.PointToAngle(
                Fixed.Zero,
                Fixed.Zero,
                line.Dx,
                line.Dy);
        }
        else if (front == actorFloorZ && back < actorFloorZ - MaxStep)
        {
            angle = Geometry.PointToAngle(
                line.Dx,
                line.Dy,
                Fixed.Zero,
                Fixed.Zero);
        }
        else
        {
            return false;
        }

        // Original MBF uses a fixed 32-unit escape vector away from the ledge.
        avoidX = -(Trig.Sin(angle) * 32);
        avoidY = Trig.Cos(angle) * 32;
        return true;
    }

    private static bool BoxCrossesLine(
        Fixed left,
        Fixed right,
        Fixed bottom,
        Fixed top,
        LineDef line)
    {
        int p1;
        int p2;

        switch (line.SlopeType)
        {
            case SlopeType.Horizontal:
                p1 = top > line.Vertex1.Y ? 1 : 0;
                p2 = bottom > line.Vertex1.Y ? 1 : 0;
                if (line.Dx < Fixed.Zero)
                {
                    p1 ^= 1;
                    p2 ^= 1;
                }
                break;

            case SlopeType.Vertical:
                p1 = right < line.Vertex1.X ? 1 : 0;
                p2 = left < line.Vertex1.X ? 1 : 0;
                if (line.Dy < Fixed.Zero)
                {
                    p1 ^= 1;
                    p2 ^= 1;
                }
                break;

            case SlopeType.Positive:
                p1 = Geometry.PointOnLineSide(left, top, line);
                p2 = Geometry.PointOnLineSide(right, bottom, line);
                break;

            case SlopeType.Negative:
                p1 = Geometry.PointOnLineSide(right, top, line);
                p2 = Geometry.PointOnLineSide(left, bottom, line);
                break;

            default:
                return false;
        }

        return p1 != p2;
    }
}
