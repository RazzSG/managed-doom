using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Movement;

/// <summary>
/// Selects the Lost Soul vertical-bounce behavior used by the original
/// executables and by Boom-compatible play.
/// </summary>
public static class BoomLostSoulCompatibility
{
    public static bool UsesCorrectBounce(
        GameCompatibility compatibility,
        GameVersion gameVersion)
    {
        // Boom fixes the Doom II 1.9 Lost Soul floor/ceiling bounce bug.
        // Ultimate Doom, Final Doom and Doom95 already contain the corrected
        // ordering, so keep that behavior even in Vanilla compatibility.
        return GameCompatibilityFeatures.SupportsBoom(compatibility) ||
            gameVersion >= GameVersion.Ultimate;
    }
}
