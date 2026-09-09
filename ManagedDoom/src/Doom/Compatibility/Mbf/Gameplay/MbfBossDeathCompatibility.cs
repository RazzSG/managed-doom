namespace ManagedDoom.Compatibility.Mbf.Gameplay;

/// <summary>
/// Selects PrBoom's comp_666 boss-death compatibility behavior.
///
/// The compatibility quirk emulates the pre-Ultimate Doom ExM8 checks: on
/// episodes 1-3, map 8 accepts boss-death callers broadly, except that the
/// E1M8 Baron case is rejected outside episode 1. The corrected path uses the
/// episode-specific Ultimate Doom boss mapping. MBF21 deoptionalizes comp_666
/// and always keeps the corrected behavior.
/// </summary>
public static class MbfBossDeathCompatibility
{
    public static bool UsesPreUltimateBossChecks(
        GameCompatibility compatibility,
        bool comp666)
    {
        if (!GameCompatibilityFeatures.SupportsMbfBossDeathCompatibility(compatibility))
            return false;

        // MBF21 deoptionalizes comp_666 to 0.
        if (GameCompatibilityFeatures.SupportsMbf21(compatibility))
            return false;

        return comp666;
    }

    public static bool IsNonCommercialBossDeathTrigger(
        GameCompatibility compatibility,
        bool comp666,
        int episode,
        int map,
        MobjType actorType)
    {
        if (UsesPreUltimateBossChecks(compatibility, comp666) && episode < 4)
        {
            if (map != 8)
                return false;

            // PrBoom's old check only special-cases the E1M8 boss flag outside
            // episode 1. In stock ManagedDoom that flag corresponds to Bruiser.
            return actorType != MobjType.Bruiser || episode == 1;
        }

        return episode switch
        {
            1 => map == 8 && actorType == MobjType.Bruiser,
            2 => map == 8 && actorType == MobjType.Cyborg,
            3 => map == 8 && actorType == MobjType.Spider,
            4 => (map == 6 && actorType == MobjType.Cyborg) ||
                 (map == 8 && actorType == MobjType.Spider),
            _ => false
        };
    }
}
