using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Rendering;

/// <summary>
/// Resolves the status-bar key patch used by Boom-compatible HUDs.
/// Boom adds STKEYS6..8 for the combined card+skull states.
/// </summary>
public static class BoomStatusBarKeys
{
    public const int ColorCount = 3;
    public const int ClassicPatchCount = 6;
    public const int CombinedPatchBase = 6;
    public const int PatchCount = 9;

    /// <summary>
    /// Returns the patch index for one color slot, or -1 when neither key is owned.
    /// Vanilla keeps the traditional skull-priority behavior. Boom and later
    /// compatibility levels use STKEYS6..8 when both key types are owned.
    /// </summary>
    public static int ResolvePatchIndex(
        int colorIndex,
        bool hasCard,
        bool hasSkull,
        GameCompatibility compatibility)
    {
        ValidateColorIndex(colorIndex);

        if (!hasCard && !hasSkull)
            return -1;

        if (hasCard && hasSkull && GameCompatibilityFeatures.SupportsBoom(compatibility))
            return CombinedPatchBase + colorIndex;

        if (hasSkull)
            return colorIndex + ColorCount;

        return colorIndex;
    }

    private static void ValidateColorIndex(int colorIndex)
    {
        if ((uint)colorIndex >= ColorCount)
            throw new System.ArgumentOutOfRangeException(nameof(colorIndex));
    }
}
