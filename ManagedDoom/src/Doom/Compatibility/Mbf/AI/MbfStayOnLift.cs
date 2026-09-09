using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.AI;

/// <summary>
/// Original MBF comp_staylift behavior from P_SmartMove. With the
/// compatibility flag disabled, a monster that starts a smart move on a lift
/// shared by a living target with the same sector tag usually abandons its
/// chase direction if that move carries it off the lift.
/// </summary>
public static class MbfStayOnLift
{
    public const int StayRandomThreshold = 230;

    public static bool ShouldTrackBeforeMove(
        World world,
        bool compStayLift,
        Mobj actor)
    {
        if (world == null ||
            actor == null ||
            compStayLift ||
            !GameCompatibilityFeatures.SupportsMbfStayOnLift(world.Options.Compatibility))
        {
            return false;
        }

        var target = actor.Target;
        var actorSector = actor.Subsector?.Sector;
        var targetSector = target?.Subsector?.Sector;

        // Keep the original short-circuit order so the lift lookup is only
        // reached for a live target that shares the actor's sector tag.
        if (target == null ||
            target.Health <= 0 ||
            actorSector == null ||
            targetSector == null ||
            targetSector.Tag != actorSector.Tag)
        {
            return false;
        }

        return MbfFriendDistance.IsOnLift(world, actor);
    }

    public static bool ShouldTrackBeforeMove(
        GameCompatibility compatibility,
        bool compStayLift,
        bool targetAlive,
        bool sameSectorTag,
        bool actorOnLift)
    {
        return GameCompatibilityFeatures.SupportsMbfStayOnLift(compatibility) &&
               !compStayLift &&
               targetAlive &&
               sameSectorTag &&
               actorOnLift;
    }

    /// <summary>
    /// Pure post-move decision. The caller intentionally obtains the random
    /// byte before querying the actor's new lift state, matching MBF's
    /// left-to-right short-circuit order.
    /// </summary>
    public static bool ShouldAbandonDirectionAfterMove(
        GameCompatibility compatibility,
        bool compStayLift,
        bool trackedBeforeMove,
        int randomByte,
        bool isOnLiftAfterMove)
    {
        return GameCompatibilityFeatures.SupportsMbfStayOnLift(compatibility) &&
               !compStayLift &&
               trackedBeforeMove &&
               randomByte < StayRandomThreshold &&
               !isOnLiftAfterMove;
    }
}
