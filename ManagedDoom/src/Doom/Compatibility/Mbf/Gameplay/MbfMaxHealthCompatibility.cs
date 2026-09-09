namespace ManagedDoom.Compatibility.Mbf.Gameplay;

/// <summary>
/// Resolves PrBoom comp_maxhealth semantics for the DeHackEd Misc
/// "Max Health" value. PrBoom keeps two distinct caps:
/// regular healing (maxhealth) and health bonuses (maxhealthbonus).
/// </summary>
public static class MbfMaxHealthCompatibility
{
    private const int DefaultRegularHealth = 100;
    private const int DefaultBonusHealth = 200;

    public static bool MaxHealthAppliesOnlyToBonuses(
        GameCompatibility compatibility,
        bool compMaxHealth)
    {
        if (!GameCompatibilityFeatures.SupportsMbfMaxHealthCompatibility(compatibility))
            return true;

        // MBF21 deoptionalizes comp_maxhealth and forces it to zero.
        if (GameCompatibilityFeatures.SupportsMbf21(compatibility))
            return false;

        return compMaxHealth;
    }

    public static int ResolveRegularHealthCap(
        GameCompatibility compatibility,
        bool compMaxHealth,
        int legacyHealthCap,
        int dehMaxHealth,
        bool hasDehMaxHealth)
    {
        if (!GameCompatibilityFeatures.SupportsMbfMaxHealthCompatibility(compatibility))
            return legacyHealthCap;

        if (MaxHealthAppliesOnlyToBonuses(compatibility, compMaxHealth))
            return DefaultRegularHealth;

        return hasDehMaxHealth ? dehMaxHealth : DefaultRegularHealth;
    }

    public static int ResolveBonusHealthCap(
        GameCompatibility compatibility,
        bool compMaxHealth,
        int legacyBonusHealthCap,
        int dehMaxHealth,
        bool hasDehMaxHealth)
    {
        if (!GameCompatibilityFeatures.SupportsMbfMaxHealthCompatibility(compatibility))
            return legacyBonusHealthCap;

        if (MaxHealthAppliesOnlyToBonuses(compatibility, compMaxHealth))
            return hasDehMaxHealth ? dehMaxHealth : DefaultBonusHealth;

        var regularCap = hasDehMaxHealth ? dehMaxHealth : DefaultRegularHealth;
        return regularCap * 2;
    }
}
