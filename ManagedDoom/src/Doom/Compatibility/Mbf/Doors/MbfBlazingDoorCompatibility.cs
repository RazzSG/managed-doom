using ManagedDoom.Compatibility.Boom.Doors;

namespace ManagedDoom.Compatibility.Mbf.Doors;

/// <summary>
/// Selects the classic blazing-door sound bug for MBF. With comp_blazing
/// enabled, blazing doors keep Doom-compatible double closing sounds and the
/// normal reopen sound when a closing blaze-raise door is obstructed.
/// Disabling the option keeps the Boom/MBF corrected sound behavior.
/// </summary>
public static class MbfBlazingDoorCompatibility
{
    public static bool UsesFixedBlazingDoorSounds(
        GameCompatibility compatibility,
        bool compBlazing)
    {
        if (!GameCompatibilityFeatures.SupportsMbfBlazingDoorCompatibility(compatibility))
            return BoomDoorCompatibility.FixesBlazingDoorSounds(compatibility);

        return !compBlazing;
    }
}
