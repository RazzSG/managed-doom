using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.AI;

/// <summary>
/// MBF comp_doorstuck compatibility. Doom treated any successfully activated
/// special line as proof that a blocked monster movement succeeded, even when
/// the monster was actually pushing into a door track. MBF fixes that by
/// default, but this flag restores the classic result.
/// </summary>
public static class MbfDoorStuck
{
    public static bool UsesClassicBlockedDoorResult(
        GameCompatibility compatibility,
        bool compDoorStuck,
        int activatedFlags) =>
        GameCompatibilityFeatures.SupportsMbfDoorStuckCompatibility(compatibility) &&
        compDoorStuck &&
        activatedFlags != 0;
}
