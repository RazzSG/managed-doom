using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Sectors;

/// <summary>
/// Sector plane movement behavior that differs between vanilla Doom and Boom 2.02.
/// </summary>
public static class BoomSectorMovementQuirks
{
    /// <summary>
    /// Vanilla Doom rolls an intermediate lowering-floor step back when a thing
    /// does not fit. Boom 2.02 intentionally keeps the lowered position instead.
    /// Destination handling is separate and still performs the normal rollback.
    /// </summary>
    public static bool ShouldRestoreIntermediateLoweringFloorAfterNoFit(
        GameCompatibility compatibility)
    {
        return !GameCompatibilityFeatures.SupportsBoom(compatibility);
    }

    /// <summary>
    /// Vanilla Doom lets a crushing floor keep an intermediate upward step when
    /// something no longer fits. Boom 2.02 fixes floor crushers by restoring the
    /// previous floor height even when crushing is enabled, so the mover keeps
    /// retrying the blocked step while P_CheckSector applies crusher damage.
    /// Non-crushing floors restore the blocked step in every compatibility mode.
    /// </summary>
    public static bool ShouldRestoreIntermediateRaisingFloorAfterNoFit(
        GameCompatibility compatibility,
        bool crush)
    {
        return GameCompatibilityFeatures.SupportsBoom(compatibility) || !crush;
    }
}
