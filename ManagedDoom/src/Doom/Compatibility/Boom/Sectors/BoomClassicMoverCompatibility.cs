using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Sectors;

/// <summary>
/// Boom fixes for classic Doom floor/platform/stair movers that are not part of
/// the generalized linedef implementations.
/// </summary>
public static class BoomClassicMoverCompatibility
{
    /// <summary>
    /// Doom's EV_BuildStairs reuses the current stair-chain sector number as the
    /// outer tagged-sector search cursor. If the first stair chain jumps past a
    /// second tagged starter, that later staircase is skipped. Boom fixes this by
    /// keeping the tagged-sector cursor independent from the chain cursor.
    /// </summary>
    public static bool UsesFixedMultiTaggedStairScan(GameCompatibility compatibility)
    {
        return GameCompatibilityFeatures.SupportsBoom(compatibility);
    }

    /// <summary>
    /// A raise-and-change platform can reverse after hitting an obstruction and
    /// reach its original low position. Doom leaves that completed thinker active;
    /// Boom removes it so the sector can be triggered again.
    /// </summary>
    public static bool RemovesBouncedPureRaisePlatform(GameCompatibility compatibility)
    {
        return GameCompatibilityFeatures.SupportsBoom(compatibility);
    }
}
