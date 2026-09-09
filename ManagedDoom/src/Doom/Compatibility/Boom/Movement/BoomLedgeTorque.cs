using System;
using ManagedDoom.Compatibility.Mbf.Movement;

namespace ManagedDoom.Compatibility.Boom.Movement;

/// <summary>
/// Killough's pseudo-torque for gravity-affected, non-sentient objects that
/// are resting while their radius hangs over a lower adjacent floor.
///
/// The implementation predates the MBF compatibility selector in this port, so
/// the class keeps its historical name. Runtime activation is MBF-family only:
/// comp_falloff = 0 enables torque, while comp_falloff = 1 restores Doom/Boom
/// behavior where inert objects can remain balanced on ledges.
/// </summary>
public sealed class BoomLedgeTorque
{
    internal const int Overdrive = 6;
    internal const int MaxGear = Overdrive + 16;

    private readonly World world;
    private readonly Fixed[] thingBox;
    private readonly Func<LineDef, bool> applyLine;

    private Mobj thing;

    public BoomLedgeTorque(World world)
    {
        this.world = world;
        thingBox = new Fixed[4];
        applyLine = ApplyLine;
    }

    public static bool IsSentient(Mobj thing)
    {
        return thing.Health > 0 && thing.Info != null && thing.Info.SeeState != MobjState.Null;
    }

    /// <summary>
    /// Called only from the at-rest branch of Mobj.Run().
    /// </summary>
    public void UpdateAtRest(Mobj thing)
    {
        if (!MbfFalloffCompatibility.UsesLedgeTorque(
                world.Options.Compatibility,
                world.Options.MbfOptions.CompFalloff))
        {
            Reset(thing);
            return;
        }

        if (IsSentient(thing))
        {
            return;
        }

        if (thing.Z > thing.DropoffZ &&
            (thing.Flags & MobjFlags.NoGravity) == 0)
        {
            Apply(thing);
        }
        else
        {
            Reset(thing);
        }
    }

    internal void Apply(Mobj thing)
    {
        var wasFalling = thing.BoomLedgeFalling;

        this.thing = thing;
        thingBox[Box.Left] = thing.X - thing.Radius;
        thingBox[Box.Right] = thing.X + thing.Radius;
        thingBox[Box.Bottom] = thing.Y - thing.Radius;
        thingBox[Box.Top] = thing.Y + thing.Radius;

        var blockMap = world.Map.BlockMap;
        var minBlockX = Math.Clamp(blockMap.GetBlockX(thingBox[Box.Left]), 0, blockMap.Width - 1);
        var maxBlockX = Math.Clamp(blockMap.GetBlockX(thingBox[Box.Right]), 0, blockMap.Width - 1);
        var minBlockY = Math.Clamp(blockMap.GetBlockY(thingBox[Box.Bottom]), 0, blockMap.Height - 1);
        var maxBlockY = Math.Clamp(blockMap.GetBlockY(thingBox[Box.Top]), 0, blockMap.Height - 1);
        var validCount = world.GetNewValidCount();

        for (var x = minBlockX; x <= maxBlockX; x++)
        {
            for (var y = minBlockY; y <= maxBlockY; y++)
            {
                blockMap.IterateLines(x, y, applyLine, validCount);
            }
        }

        this.thing = null;

        thing.BoomLedgeFalling = thing.MomX != Fixed.Zero || thing.MomY != Fixed.Zero;

        if (!thing.BoomLedgeFalling && !wasFalling)
        {
            thing.BoomTorqueGear = 0;
        }
        else if (thing.BoomTorqueGear < MaxGear)
        {
            thing.BoomTorqueGear++;
        }
    }

    internal bool ApplyLine(LineDef line)
    {
        if (line.BackSector == null)
        {
            return true;
        }

        if (thingBox[Box.Right] <= line.BoundingBox[Box.Left] ||
            thingBox[Box.Left] >= line.BoundingBox[Box.Right] ||
            thingBox[Box.Top] <= line.BoundingBox[Box.Bottom] ||
            thingBox[Box.Bottom] >= line.BoundingBox[Box.Top] ||
            Geometry.BoxOnLineSide(thingBox, line) != -1)
        {
            return true;
        }

        // Original Boom deliberately performs the lever-arm calculation using
        // integer map coordinates, then treats the result as fixed-point.
        var dx = line.Dx.Data >> Fixed.FracBits;
        var dy = line.Dy.Data >> Fixed.FracBits;
        var thingX = thing.X.Data >> Fixed.FracBits;
        var thingY = thing.Y.Data >> Fixed.FracBits;
        var v1X = line.Vertex1.X.Data >> Fixed.FracBits;
        var v1Y = line.Vertex1.Y.Data >> Fixed.FracBits;

        var leverLong =
            (long)dx * thingY -
            (long)dy * thingX -
            (long)dx * v1Y +
            (long)dy * v1X;
        var lever = new Fixed(unchecked((int)leverLong));

        var frontFloor = line.FrontSector.FloorHeight;
        var backFloor = line.BackSector.FloorHeight;

        var contactsDropoff = lever < Fixed.Zero
            ? frontFloor < thing.Z && backFloor >= thing.Z
            : backFloor < thing.Z && frontFloor >= thing.Z;

        if (!contactsDropoff)
        {
            return true;
        }

        var major = Fixed.Abs(line.Dx);
        var minor = Fixed.Abs(line.Dy);
        if (minor > major)
        {
            (major, minor) = (minor, major);
        }

        if (major == Fixed.Zero)
        {
            return true;
        }

        // Equivalent to Boom's finesine[tantoangle(y/x)+ANG90].
        var normalScale = Trig.Cos(
            Geometry.PointToAngle(Fixed.Zero, Fixed.Zero, major, minor));

        var gear = thing.BoomTorqueGear;
        var scaledNormal = gear < Overdrive
            ? normalScale << (Overdrive - gear)
            : normalScale >> (gear - Overdrive);

        var distance = (lever * scaledNormal) / major;
        var torqueX = line.Dy * distance;
        var torqueY = line.Dx * distance;
        var magnitude = torqueX * torqueX + torqueY * torqueY;
        var maxMagnitude = Fixed.FromInt(4);

        while (magnitude > maxMagnitude && thing.BoomTorqueGear < MaxGear)
        {
            thing.BoomTorqueGear++;
            torqueX >>= 1;
            torqueY >>= 1;
            magnitude >>= 1;
        }

        thing.MomX -= torqueX;
        thing.MomY += torqueY;

        return true;
    }

    public static void Reset(Mobj thing)
    {
        thing.BoomLedgeFalling = false;
        thing.BoomTorqueGear = 0;
    }
}
