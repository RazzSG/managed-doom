using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.AI;

/// <summary>
/// Original MBF helper-dog ledge rule. Dogs may occasionally follow a nearby
/// same-side target down a tall ledge, but the special move is capped at 128
/// map units and is not a general-purpose DROPOFF permission.
/// </summary>
public static class MbfDogJumping
{
    public const int FollowDistanceUnits = 144;
    public const int MaxDropHeightUnits = 128;
    public const int RandomThreshold = 235;

    private static readonly Fixed followDistance = Fixed.FromInt(FollowDistanceUnits);
    private static readonly Fixed maxDropHeight = Fixed.FromInt(MaxDropHeightUnits);

    public static bool CanAttempt(
        GameCompatibility compatibility,
        bool optionEnabled,
        Mobj actor,
        Mobj target)
    {
        if (!GameCompatibilityFeatures.SupportsMbfDogJumping(compatibility) ||
            !optionEnabled ||
            actor == null ||
            target == null ||
            actor.Type != MobjType.Dog)
        {
            return false;
        }

        // The target must be on the same FRIEND side as the dog. This lets
        // helpers follow their owner/friends, but never use the jump while
        // pursuing a hostile target.
        if (((actor.Flags ^ target.Flags) & MobjFlags.Friend) != 0)
            return false;

        var distance = Geometry.AproxDistance(
            actor.X - target.X,
            actor.Y - target.Y);

        return distance < followDistance;
    }

    public static bool PassesRandom(int randomByte)
    {
        return randomByte < RandomThreshold;
    }

    public static bool ShouldAttempt(
        GameCompatibility compatibility,
        bool optionEnabled,
        Mobj actor,
        Mobj target,
        int randomByte)
    {
        return CanAttempt(compatibility, optionEnabled, actor, target) &&
               PassesRandom(randomByte);
    }

    public static bool AllowsTargetedDropoff(
        Mobj actor,
        Fixed destinationFloor,
        Fixed destinationDropoff)
    {
        if (actor?.Target == null)
            return false;

        var dropHeight = destinationFloor - destinationDropoff;
        if (dropHeight > maxDropHeight)
            return false;

        // MBF only allows the controlled dog drop when the followed target is
        // already at, or below, the lower floor on the far side of the ledge.
        return actor.Target.Z <= destinationDropoff;
    }
}
