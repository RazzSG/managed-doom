using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Collision;

/// <summary>
/// Thing-to-thing collision behavior changed by Boom 2.02.
/// These rules apply only after missile, skull and pickup special cases
/// have already been handled by the normal movement code.
/// </summary>
public static class BoomCollisionQuirks
{
    public static bool BlocksGenericThing(
        MobjFlags movingFlags,
        MobjFlags obstacleFlags,
        GameCompatibility compatibility)
    {
        var obstacleIsSolid = (obstacleFlags & MobjFlags.Solid) != 0;

        if (!GameCompatibilityFeatures.SupportsBoom(compatibility))
            return obstacleIsSolid;

        // Boom lets non-solid moving objects pass through ordinary solid things,
        // and treats a thing carrying MF_NOCLIP as non-blocking.
        return obstacleIsSolid &&
               (obstacleFlags & MobjFlags.NoClip) == 0 &&
               (movingFlags & MobjFlags.Solid) != 0;
    }
}
