using ManagedDoom.Compatibility.Boom.Movement;

namespace ManagedDoom.Compatibility.Mbf.Movement;

/// <summary>
/// Selects the Lost Soul vertical-bounce ordering for MBF. comp_soul keeps
/// Doom's buggy ordering when enabled; disabling it uses the corrected
/// Boom/MBF ordering. Pre-MBF compatibility continues to use the existing
/// Boom/version resolver unchanged.
/// </summary>
public static class MbfLostSoulCompatibility
{
    public static bool UsesCorrectBounce(
        GameCompatibility compatibility,
        GameVersion gameVersion,
        bool compSoul)
    {
        if (!GameCompatibilityFeatures.SupportsMbfLostSoulCompatibility(compatibility))
            return BoomLostSoulCompatibility.UsesCorrectBounce(compatibility, gameVersion);

        return !compSoul;
    }
}
