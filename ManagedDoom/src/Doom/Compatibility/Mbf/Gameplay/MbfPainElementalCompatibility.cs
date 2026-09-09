using ManagedDoom.Compatibility.Boom.Gameplay;

namespace ManagedDoom.Compatibility.Mbf.Gameplay;

/// <summary>
/// Selects the Pain Elemental Lost Soul population limit for MBF.
/// comp_pain keeps Doom's compatibility limit when enabled; disabling it uses
/// the corrected Boom/MBF behavior. Spawn-safety fixes belong to comp_skull.
/// </summary>
public static class MbfPainElementalCompatibility
{
    public static bool EnforcesLostSoulLimit(
        GameCompatibility compatibility,
        bool compPain)
    {
        if (!GameCompatibilityFeatures.SupportsMbfPainElementalCompatibility(compatibility))
            return BoomGameplayBugFixes.EnforcesPainElementalLostSoulLimit(compatibility);

        return compPain;
    }
}
