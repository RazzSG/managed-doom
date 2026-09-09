using ManagedDoom.Compatibility.Boom.Movement;

namespace ManagedDoom.Compatibility.Mbf.Movement;

/// <summary>
/// Selects the large-negative-displacement splitting rule used by P_XYMovement.
///
/// Doom only split large positive displacements, so sufficiently large negative
/// movement could cross a blocking line in one step. Boom fixed that by splitting
/// both signs. PrBoom exposes the old Doom behavior as comp_moveblock for MBF
/// compatibility, while MBF21 deoptionalizes the fix and therefore always uses
/// the corrected split.
/// </summary>
public static class MbfMoveBlockCompatibility
{
    public static bool ShouldSplitNegativeDisplacement(
        Fixed moveX,
        Fixed moveY,
        Fixed halfMaxMove,
        GameCompatibility compatibility,
        bool compMoveBlock)
    {
        var boomWouldSplit = BoomMovementQuirks.ShouldSplitNegativeDisplacement(
            moveX,
            moveY,
            halfMaxMove,
            compatibility);

        if (!boomWouldSplit)
            return false;

        // Boom owns the corrected lower-layer behavior and never reads MBF OPTIONS.
        if (!GameCompatibilityFeatures.SupportsMbfMoveBlockCompatibility(compatibility))
            return true;

        // MBF21 removes comp_moveblock from the optional compatibility set:
        // the large-negative-displacement fix is always enabled.
        if (GameCompatibilityFeatures.SupportsMbf21(compatibility))
            return true;

        // MBF / PrBoom compatibility: comp_moveblock=1 restores the Doom bug.
        return !compMoveBlock;
    }
}
