using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.Movement;

/// <summary>
/// Runtime semantics for MBF's BOUNCES actor flag. Bouncers use the MBF
/// line-clipping rule, reflect XY momentum from blocking geometry, and use
/// the original mass-scaled vertical gravity and floor damping constants.
/// </summary>
public static class MbfBounceCompatibility
{
    private static readonly Fixed normalFloorDecay = Fixed.FromDouble(0.45);
    private static readonly Fixed floatFloorDecay = Fixed.FromDouble(0.70);
    private static readonly Fixed floatDropoffFloorDecay = Fixed.FromDouble(0.85);

    public static bool IsBouncer(GameCompatibility compatibility, Mobj actor)
    {
        return GameCompatibilityFeatures.SupportsMbfActorPhysicsFlags(compatibility) &&
               actor != null &&
               (actor.Flags & MobjFlags.Bounces) != 0;
    }

    public static bool UsesMissileLineBlockingRules(
        GameCompatibility compatibility,
        Mobj actor)
    {
        return actor != null &&
               ((actor.Flags & MobjFlags.Missile) != 0 || IsBouncer(compatibility, actor));
    }

    public static bool IsNonSolidNonMissileBouncer(
        GameCompatibility compatibility,
        Mobj actor)
    {
        return IsBouncer(compatibility, actor) &&
               (actor.Flags & (MobjFlags.Solid | MobjFlags.Missile)) == 0;
    }

    public static void BounceFromSolidThing(Mobj actor)
    {
        if (actor == null)
            return;

        actor.MomX = -actor.MomX;
        actor.MomY = -actor.MomY;

        if ((actor.Flags & MobjFlags.NoGravity) == 0)
        {
            actor.MomX >>= 2;
            actor.MomY >>= 2;
        }
    }

    public static void BounceFromLine(Mobj actor, LineDef line)
    {
        if (actor == null)
            return;

        if (line == null)
        {
            actor.MomX = Fixed.Zero;
            actor.MomY = Fixed.Zero;
            return;
        }

        // MBF projects the current momentum onto the contacted line using
        // integer map-unit dx/dy, reflects the perpendicular component, and
        // halves that perpendicular component when gravity applies.
        var dx = line.Dx.Data >> Fixed.FracBits;
        var dy = line.Dy.Data >> Fixed.FracBits;
        var denominator = (long)dx * dx + (long)dy * dy;

        if (denominator == 0)
        {
            actor.MomX = Fixed.Zero;
            actor.MomY = Fixed.Zero;
            return;
        }

        var numerator =
            (long)dx * actor.MomX.Data +
            (long)dy * actor.MomY.Data;
        var r = new Fixed((int)(numerator / denominator));
        var projectionX = r * line.Dx;
        var projectionY = r * line.Dy;

        var reflectedX = projectionX * 2 - actor.MomX;
        var reflectedY = projectionY * 2 - actor.MomY;

        if ((actor.Flags & MobjFlags.NoGravity) != 0)
        {
            actor.MomX = reflectedX;
            actor.MomY = reflectedY;
        }
        else
        {
            actor.MomX = (reflectedX + projectionX) / 2;
            actor.MomY = (reflectedY + projectionY) / 2;
        }
    }

    public static Fixed GetVerticalGravityStep(Mobj actor, Fixed gravity)
    {
        if (actor?.Info == null)
            return Fixed.Zero;

        return new Fixed((int)((long)actor.Info.Mass * gravity.Data / 256));
    }

    public static Fixed ApplyFloorDecay(Mobj actor, Fixed upwardMomentum, Fixed gravity)
    {
        if (actor == null || (actor.Flags & MobjFlags.NoGravity) != 0)
            return upwardMomentum;

        Fixed decay;
        if ((actor.Flags & MobjFlags.Float) != 0)
        {
            decay = (actor.Flags & MobjFlags.DropOff) != 0
                ? floatDropoffFloorDecay
                : floatFloorDecay;
        }
        else
        {
            decay = normalFloorDecay;
        }

        var result = upwardMomentum * decay;
        var stopThreshold = GetVerticalGravityStep(actor, gravity) * 4;
        return Fixed.Abs(result) <= stopThreshold ? Fixed.Zero : result;
    }

    public static bool PreservesLedgeMomentum(
        GameCompatibility compatibility,
        Mobj actor)
    {
        return IsBouncer(compatibility, actor) && actor.Z > actor.DropoffZ;
    }
}
