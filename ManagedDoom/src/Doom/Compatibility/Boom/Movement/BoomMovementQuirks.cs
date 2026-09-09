using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Friction;

namespace ManagedDoom.Compatibility.Boom.Movement;

/// <summary>
/// Movement behavior that differs between vanilla Doom and Boom 2.02.
/// Keep these compatibility decisions out of the core movement code where possible.
/// </summary>
public static class BoomMovementQuirks
{
    public static bool ShouldSplitNegativeDisplacement(
        Fixed moveX,
        Fixed moveY,
        Fixed halfMaxMove,
        GameCompatibility compatibility)
    {
        if (!GameCompatibilityFeatures.SupportsBoom(compatibility))
            return false;

        return moveX < -halfMaxMove || moveY < -halfMaxMove;
    }

    public static Fixed GetCoastingFriction(
        Mobj thing,
        Fixed oldX,
        Fixed oldY,
        GameCompatibility compatibility)
    {
        if (!GameCompatibilityFeatures.SupportsBoom(compatibility))
            return BoomFrictionTranslator.OriginalFriction;

        var sectorFriction = BoomSectorFriction.GetFriction(thing);

        // Boom 2.02 falls back to ordinary friction when a player with
        // custom floor friction could not move at all during this tic.
        if (thing.X == oldX && thing.Y == oldY)
            return BoomFrictionTranslator.OriginalFriction;

        return sectorFriction;
    }
    public static Fixed GetPlayerBobLimit(
        Mobj thing,
        Fixed normalLimit,
        GameCompatibility compatibility)
    {
        if (compatibility != GameCompatibility.Boom)
            return normalLimit;

        // Boom 2.02 derives bobbing from physical player momentum and caps it
        // to one quarter of MAXBOB on ice. MBF switches to independent
        // player-applied bob momentum and uses the normal limit.
        return BoomSectorFriction.GetFriction(thing) > BoomFrictionTranslator.OriginalFriction
            ? normalLimit >> 2
            : normalLimit;
    }

}
