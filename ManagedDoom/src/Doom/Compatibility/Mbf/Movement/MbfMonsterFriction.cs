using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Friction;

namespace ManagedDoom.Compatibility.Mbf.Movement;

/// <summary>
/// MBF makes living monsters participate in Boom floor-friction effects.
/// Boom itself keeps custom friction player-only. In MBF this affects both
/// coasting momentum and the monster's own attempted movement step.
/// </summary>
public static class MbfMonsterFriction
{
    private static readonly int iceMomentumScale =
        Fixed.FracUnit / BoomFrictionTranslator.OriginalMoveFactorData / 4;

    public static bool Applies(GameCompatibility compatibility, Mobj thing)
    {
        return Applies(compatibility, true, thing);
    }

    public static bool Applies(GameCompatibility compatibility, bool enabled, Mobj thing)
    {
        if (!enabled ||
            !GameCompatibilityFeatures.SupportsMbfMonsterFriction(compatibility) ||
            thing == null ||
            thing.Player != null ||
            thing.Health <= 0)
        {
            return false;
        }

        if ((thing.Flags & (MobjFlags.Missile |
                            MobjFlags.SkullFly |
                            MobjFlags.Corpse |
                            MobjFlags.NoClip |
                            MobjFlags.NoGravity)) != 0)
        {
            return false;
        }

        // Match the MBF notion of a sentient actor: alive and possessing a
        // chase/see state. This also covers DeHackEd monsters whose COUNTKILL
        // flag was intentionally changed.
        return thing.Info != null && thing.Info.SeeState != MobjState.Null;
    }

    /// <summary>
    /// MBF keeps applying the sector's floor friction to a monster's residual
    /// horizontal momentum after it has become a corpse. This is separate from
    /// <see cref="Applies"/>, which deliberately covers living/sentient monster
    /// movement only.
    /// </summary>
    public static bool AppliesToCorpseCoasting(
        GameCompatibility compatibility,
        bool enabled,
        Mobj thing)
    {
        if (!enabled ||
            !GameCompatibilityFeatures.SupportsMbfMonsterFriction(compatibility) ||
            thing == null ||
            thing.Player != null ||
            (thing.Flags & MobjFlags.Corpse) == 0)
        {
            return false;
        }

        return (thing.Flags & (MobjFlags.Missile |
                               MobjFlags.SkullFly |
                               MobjFlags.NoClip |
                               MobjFlags.NoGravity)) == 0;
    }

    public static Fixed GetCorpseCoastingFriction(
        GameCompatibility compatibility,
        bool enabled,
        Mobj thing)
    {
        if (!AppliesToCorpseCoasting(compatibility, enabled, thing))
            return BoomFrictionTranslator.OriginalFriction;

        ResolveFloorProperties(thing, out var friction, out _);
        return friction;
    }

    public static Fixed GetCoastingFriction(
        GameCompatibility compatibility,
        Mobj thing)
    {
        return GetCoastingFriction(compatibility, true, thing);
    }

    public static Fixed GetCoastingFriction(
        GameCompatibility compatibility,
        bool enabled,
        Mobj thing)
    {
        if (!Applies(compatibility, enabled, thing))
            return BoomFrictionTranslator.OriginalFriction;

        ResolveFloorProperties(thing, out var friction, out _);
        return friction;
    }

    /// <summary>
    /// Resolves the friction and movement factor used by MBF's P_Move path.
    /// When an actor straddles multiple friction sectors, muddy/low friction
    /// takes precedence over icy/high friction just like MBF's P_GetFriction.
    /// </summary>
    public static void ResolveMovementProperties(
        GameCompatibility compatibility,
        bool enabled,
        Mobj thing,
        out Fixed friction,
        out Fixed moveFactor)
    {
        if (!Applies(compatibility, enabled, thing))
        {
            friction = BoomFrictionTranslator.OriginalFriction;
            moveFactor = BoomFrictionTranslator.OriginalMoveFactor;
            return;
        }

        ResolveFloorProperties(thing, out friction, out moveFactor);

        if (friction < BoomFrictionTranslator.OriginalFriction)
        {
            // MBF's P_GetMoveFactor gives monsters progressively better
            // footing in sludge as their current horizontal momentum grows.
            var momentum = Geometry.AproxDistance(thing.MomX, thing.MomY).Data;
            if (momentum > (BoomFrictionTranslator.MoreFrictionMomentumData << 2))
            {
                moveFactor <<= 3;
            }
            else if (momentum > (BoomFrictionTranslator.MoreFrictionMomentumData << 1))
            {
                moveFactor <<= 2;
            }
            else if (momentum > BoomFrictionTranslator.MoreFrictionMomentumData)
            {
                moveFactor <<= 1;
            }
        }
    }

    /// <summary>
    /// MBF reduces a monster's attempted step speed in sludge. The integer
    /// formula is intentionally kept identical to the original fixed-point
    /// calculation, including the minimum speed of one map unit/tic.
    /// </summary>
    public static int GetAdjustedStepSpeed(int baseSpeed, Fixed friction, Fixed moveFactor)
    {
        if (friction >= BoomFrictionTranslator.OriginalFriction)
            return baseSpeed;

        var original = BoomFrictionTranslator.OriginalMoveFactorData;
        var footing = original - (original - moveFactor.Data) / 2;
        var speed = (int)(((long)footing * baseSpeed) / original);
        return speed == 0 ? 1 : speed;
    }

    public static bool UsesMomentumStep(Fixed friction) =>
        friction > BoomFrictionTranslator.OriginalFriction;

    /// <summary>
    /// Converts the resolved Boom move factor to the fixed-point multiplier
    /// MBF uses when an otherwise-valid monster step happens on ice.
    /// </summary>
    public static Fixed GetMomentumStepScale(Fixed moveFactor) =>
        new Fixed(moveFactor.Data * iceMomentumScale);

    private static void ResolveFloorProperties(
        Mobj thing,
        out Fixed friction,
        out Fixed moveFactor)
    {
        friction = BoomFrictionTranslator.OriginalFriction;
        moveFactor = BoomFrictionTranslator.OriginalMoveFactor;

        if (thing == null)
            return;

        var found = false;
        for (var node = thing.TouchingSectorList; node != null; node = node.ThingNext)
        {
            var sector = node.Sector;
            if (sector == null ||
                ((int)sector.Special & BoomFrictionTranslator.FrictionMask) == 0)
            {
                continue;
            }

            var touchesFloor = thing.Z <= sector.FloorHeight ||
                (sector.HeightSector != null && thing.Z <= sector.HeightSector.FloorHeight);
            if (!touchesFloor)
                continue;

            if (!found ||
                sector.Friction < friction ||
                friction == BoomFrictionTranslator.OriginalFriction)
            {
                friction = sector.Friction;
                moveFactor = sector.MoveFactor;
                found = true;
            }
        }

        // The touching-sector list should normally be present, but keeping a
        // subsector fallback makes the helper safe for synthetic/unit actors.
        if (!found && thing.Subsector != null)
        {
            var sector = thing.Subsector.Sector;
            if (sector != null &&
                ((int)sector.Special & BoomFrictionTranslator.FrictionMask) != 0 &&
                (thing.Z <= sector.FloorHeight ||
                 (sector.HeightSector != null && thing.Z <= sector.HeightSector.FloorHeight)))
            {
                friction = sector.Friction;
                moveFactor = sector.MoveFactor;
            }
        }
    }
}
