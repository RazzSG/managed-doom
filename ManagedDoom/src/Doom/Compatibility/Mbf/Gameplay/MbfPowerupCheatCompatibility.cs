using ManagedDoom;

namespace ManagedDoom.Compatibility.Mbf.Gameplay;

/// <summary>
/// MBF comp_infcheat selector. MBF's corrected default keeps idbehold-style
/// powerup cheats active indefinitely; the compatibility flag restores the
/// original Doom timed behavior. Infinite powers use the classic negative
/// sentinel so the normal player ticker can leave them untouched.
/// </summary>
public static class MbfPowerupCheatCompatibility
{
    public const int InfiniteDuration = -1;

    public static bool UsesInfiniteDuration(
        GameCompatibility compatibility,
        bool compInfCheat)
    {
        return GameCompatibilityFeatures.SupportsMbf(compatibility) && !compInfCheat;
    }

    public static int ResolveActivatedPowerValue(
        GameCompatibility compatibility,
        bool compInfCheat,
        PowerType powerType,
        int normalValue)
    {
        if (!UsesInfiniteDuration(compatibility, compInfCheat))
            return normalValue;

        // Strength is already effectively permanent in Doom: its counter counts
        // upward instead of expiring, so it does not need the negative sentinel.
        return powerType == PowerType.Strength ? normalValue : InfiniteDuration;
    }

    public static bool IsActive(int value) => value != 0;

    public static bool ShouldTickDown(int value) => value > 0;
}
