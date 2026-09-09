namespace ManagedDoom.Compatibility.Mbf.Audio;

/// <summary>
/// Selects PrBoom's comp_sound compatibility behavior.
///
/// The corrected path adds the missing two-sided use-line "noway" sound,
/// suppresses the hard-landing "oof" for dead players, and lets pickup sounds
/// originate from non-display players as well. comp_sound=1 restores the old
/// Doom sound quirks for MBF compatibility. MBF21 deoptionalizes this flag and
/// always uses the corrected behavior.
/// </summary>
public static class MbfSoundCompatibility
{
    public static bool UsesDoomSoundQuirks(
        GameCompatibility compatibility,
        bool compSound)
    {
        if (compatibility == GameCompatibility.Vanilla)
            return true;

        // Boom owns the corrected lower-layer behavior and never reads MBF OPTIONS.
        if (!GameCompatibilityFeatures.SupportsMbfSoundCompatibility(compatibility))
            return false;

        // MBF21 deoptionalizes comp_sound to 0.
        if (GameCompatibilityFeatures.SupportsMbf21(compatibility))
            return false;

        return compSound;
    }

    public static bool UsesTwoSidedUseNoWayFix(
        GameCompatibility compatibility,
        bool compSound) =>
        !UsesDoomSoundQuirks(compatibility, compSound);

    public static bool ShouldPlayHardLandingSound(
        GameCompatibility compatibility,
        bool compSound,
        int health) =>
        UsesDoomSoundQuirks(compatibility, compSound) || health > 0;

    public static bool ShouldPlayPickupSound(
        GameCompatibility compatibility,
        bool compSound,
        bool isDisplayPlayer) =>
        !UsesDoomSoundQuirks(compatibility, compSound) || isDisplayPlayer;

    public static bool ShouldReplaceSameOriginChannel(
        GameCompatibility compatibility,
        bool compSound,
        bool existingIsPickup,
        bool newIsPickup) =>
        UsesDoomSoundQuirks(compatibility, compSound) ||
        existingIsPickup == newIsPickup;
}
