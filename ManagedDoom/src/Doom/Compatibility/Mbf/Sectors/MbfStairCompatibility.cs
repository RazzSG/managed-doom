using ManagedDoom.Compatibility.Boom.Sectors;

namespace ManagedDoom.Compatibility.Mbf.Sectors;

/// <summary>
/// Selects the classic stair-building semantics controlled by MBF comp_stairs.
///
/// Doom has two relevant quirks in EV_BuildStairs:
///  - the outer tagged-sector scan resumes after the last sector reached by the
///    previous staircase, which can skip another tagged stair starter;
///  - the next step height is advanced before checking whether a same-texture
///    candidate sector is already moving, so a rejected candidate can consume
///    one step of height.
///
/// Boom fixes both behaviors. MBF defaults to the Boom behavior and
/// comp_stairs = 1 intentionally restores the Doom semantics. MBF21 inherits
/// the selector, but uses the corrected Doom emulation rather than MBF's old
/// hash-chain compatibility bug.
/// </summary>
public static class MbfStairCompatibility
{
    public static bool UsesDoomStairSemantics(
        GameCompatibility compatibility,
        bool compStairs)
    {
        if (GameCompatibilityFeatures.SupportsMbfStairCompatibility(compatibility))
            return compStairs;

        return compatibility == GameCompatibility.Vanilla;
    }

    public static bool UsesFixedMultiTaggedStairScan(
        GameCompatibility compatibility,
        bool compStairs) =>
        BoomClassicMoverCompatibility.UsesFixedMultiTaggedStairScan(
            ResolveStairCompatibility(compatibility, compStairs));

    public static bool AdvancesHeightBeforeBusyCandidateCheck(
        GameCompatibility compatibility,
        bool compStairs) =>
        UsesDoomStairSemantics(compatibility, compStairs);

    private static GameCompatibility ResolveStairCompatibility(
        GameCompatibility compatibility,
        bool compStairs) =>
        UsesDoomStairSemantics(compatibility, compStairs)
            ? GameCompatibility.Vanilla
            : GameCompatibility.Boom;
}
