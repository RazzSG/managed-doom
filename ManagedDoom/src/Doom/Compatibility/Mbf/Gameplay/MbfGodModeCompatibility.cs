using ManagedDoom.Compatibility.Boom.Gameplay;

namespace ManagedDoom.Compatibility.Mbf.Gameplay;

/// <summary>
/// Selects MBF comp_god semantics without duplicating Boom's damage rules.
/// MBF defaults to absolute god mode; comp_god = 1 intentionally restores
/// Doom's two quirks: 1000+ damage bypasses god mode and sector 11 clears it.
/// </summary>
public static class MbfGodModeCompatibility
{
    public static bool UsesAbsoluteGodMode(
        GameCompatibility compatibility,
        bool compGod)
    {
        if (!GameCompatibilityFeatures.SupportsMbfGodModeCompatibility(compatibility))
            return GameCompatibilityFeatures.SupportsBoom(compatibility);

        return !compGod;
    }

    public static bool ClearsGodModeInExitDamageSector(
        GameCompatibility compatibility,
        bool compGod) =>
        BoomGameplayBugFixes.ClearsGodModeInExitDamageSector(
            ResolveCompatibility(compatibility, compGod));

    public static bool ShouldIgnorePlayerDamage(
        GameCompatibility compatibility,
        bool compGod,
        int damage,
        bool godMode,
        bool invulnerable) =>
        BoomGameplayBugFixes.ShouldIgnorePlayerDamage(
            ResolveCompatibility(compatibility, compGod),
            damage,
            godMode,
            invulnerable);

    private static GameCompatibility ResolveCompatibility(
        GameCompatibility compatibility,
        bool compGod) =>
        UsesAbsoluteGodMode(compatibility, compGod)
            ? GameCompatibility.Boom
            : GameCompatibility.Vanilla;
}
