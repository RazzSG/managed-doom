using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.Movement;

/// <summary>
/// PrBoom/MBF21 comp_voodooscroller compatibility. The legacy LXDOOM-era
/// voodoo-doll stop rule clears very small scrolling momentum and makes dolls
/// travel too slowly on conveyors. MBF keeps that bug by default; MBF21 fixes
/// it by default, while both profiles allow an explicit OPTIONS override.
/// </summary>
public static class MbfVoodooScrollerCompatibility
{
    public static bool IsLegacyBugEnabled(
        GameCompatibility compatibility,
        bool compVoodooScroller,
        bool hasExplicitOverride)
    {
        if (!GameCompatibilityFeatures.SupportsMbfVoodooScrollerCompatibility(compatibility))
            return false;

        if (GameCompatibilityFeatures.SupportsMbf21(compatibility) && !hasExplicitOverride)
            return false;

        return compVoodooScroller;
    }

    /// <summary>
    /// Extra low-momentum stop predicate used only for voodoo dolls. A real
    /// player is still governed by the ordinary no-input stop rule. For a doll
    /// with active player input, the fixed behavior keeps scrolling momentum
    /// alive exactly when the transient MIF_SCROLLING-equivalent is present.
    /// </summary>
    public static bool ShouldForceStop(
        Mobj thing,
        GameCompatibility compatibility,
        bool compVoodooScroller,
        bool hasExplicitOverride)
    {
        var player = thing?.Player;
        if (player == null || object.ReferenceEquals(player.Mobj, thing))
            return false;

        if (!GameCompatibilityFeatures.SupportsMbfVoodooScrollerCompatibility(compatibility))
            return false;

        return IsLegacyBugEnabled(
                   compatibility,
                   compVoodooScroller,
                   hasExplicitOverride) ||
               !thing.MbfScrollingMovement;
    }
}
