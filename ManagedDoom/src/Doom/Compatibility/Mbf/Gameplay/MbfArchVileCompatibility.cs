using ManagedDoom.Compatibility.Boom.Gameplay;

namespace ManagedDoom.Compatibility.Mbf.Gameplay;

/// <summary>
/// Selects the Arch-Vile resurrection ghost bug for MBF.
/// comp_vile preserves Doom's buggy resurrection dimensions when enabled;
/// disabling it keeps the corrected Boom/MBF behavior.
/// </summary>
public static class MbfArchVileCompatibility
{
    public static bool UsesFixedResurrection(
        GameCompatibility compatibility,
        bool compVile)
    {
        if (!GameCompatibilityFeatures.SupportsMbfArchVileCompatibility(compatibility))
            return BoomGameplayBugFixes.UsesFixedArchVileResurrection(compatibility);

        return !compVile;
    }

    public static BoomGameplayBugFixes.ArchVileCorpseFitState PrepareCorpseForFitCheck(
        Mobj corpse,
        GameCompatibility compatibility,
        bool compVile)
    {
        return BoomGameplayBugFixes.PrepareArchVileCorpseForFitCheck(
            corpse,
            UsesFixedResurrection(compatibility, compVile));
    }

    public static void RestoreCorpseAfterFitCheck(
        Mobj corpse,
        BoomGameplayBugFixes.ArchVileCorpseFitState state,
        GameCompatibility compatibility,
        bool compVile)
    {
        BoomGameplayBugFixes.RestoreArchVileCorpseAfterFitCheck(
            corpse,
            state,
            UsesFixedResurrection(compatibility, compVile));
    }

    public static void ApplyResurrectionDimensions(
        Mobj corpse,
        GameCompatibility compatibility,
        bool compVile)
    {
        BoomGameplayBugFixes.ApplyArchVileResurrectionDimensions(
            corpse,
            UsesFixedResurrection(compatibility, compVile));
    }
}
