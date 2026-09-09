using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.Movement;

/// <summary>
/// PrBoom/MBF21 comp_ledgeblock compatibility. MBF defaults the option to
/// false, while MBF21 defaults it to true. MBF21 also temporarily ignores
/// ledge blocking for momentum supplied by scrollers and pushers.
/// </summary>
public static class MbfLedgeBlockCompatibility
{
    public static readonly Fixed MaxStep = Fixed.FromInt(24);

    public static bool IsEnabled(
        GameCompatibility compatibility,
        bool compLedgeBlock,
        bool hasExplicitOverride)
    {
        if (!GameCompatibilityFeatures.SupportsMbfLedgeBlockCompatibility(compatibility))
            return false;

        if (GameCompatibilityFeatures.SupportsMbf21(compatibility) && !hasExplicitOverride)
            return true;

        return compLedgeBlock;
    }

    public static bool BlocksTallDropoff(
        GameCompatibility compatibility,
        bool compLedgeBlock,
        bool hasExplicitOverride,
        bool scrollingMovement,
        Fixed destinationFloor,
        Fixed destinationDropoff)
    {
        if (!IsEnabled(compatibility, compLedgeBlock, hasExplicitOverride))
            return false;

        // MBF21 explicitly exempts the next XY move after a scroller/pusher
        // supplies momentum. The transient marker is cleared after XYMovement.
        if (GameCompatibilityFeatures.SupportsMbf21(compatibility) && scrollingMovement)
            return false;

        return destinationFloor - destinationDropoff > MaxStep;
    }

    public static void MarkScrollingMovement(Mobj thing, Fixed deltaX, Fixed deltaY)
    {
        // Do not leave a stale one-shot marker behind when a zero-strength
        // pusher/current happens to run. Upstream marks actual scroll momentum.
        if (thing != null && (deltaX != Fixed.Zero || deltaY != Fixed.Zero))
            thing.MbfScrollingMovement = true;
    }

    public static void ClearScrollingMovement(Mobj thing)
    {
        if (thing != null)
            thing.MbfScrollingMovement = false;
    }
}
