namespace ManagedDoom.Compatibility.Mbf.Gameplay;

/// <summary>
/// Selects MBF comp_zombie line-exit behavior. Doom, Boom and the MBF
/// compatibility setting allow a player with zero health to trigger line exits.
/// MBF can instead reject those exits when comp_zombie = 0. Boss/sector-driven
/// exits are intentionally outside this selector, matching the original MBF fix.
/// </summary>
public static class MbfZombieExitCompatibility
{
    public static bool CanTriggerLineExit(
        GameCompatibility compatibility,
        bool compZombie,
        Mobj thing)
    {
        if (thing?.Player == null || thing.Player.Health > 0)
            return true;

        if (!GameCompatibilityFeatures.SupportsMbfZombieExitCompatibility(compatibility))
            return true;

        return compZombie;
    }
}
