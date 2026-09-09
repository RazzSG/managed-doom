using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.Movement;

/// <summary>
/// MBF "monkeys" stair/ledge clipping. With the option disabled Doom uses
/// the full floor-to-dropoff span at the destination. With it enabled MBF
/// compares destination floor and dropoff heights independently against the
/// actor's remembered previous values. The ordinary step-up check still limits
/// upward movement; this rule prevents either remembered edge from dropping by
/// more than 24 units at once.
/// </summary>
public static class MbfMonkeyClimbing
{
    public static readonly Fixed MaxStep = Fixed.FromInt(24);

    public static bool Applies(GameCompatibility compatibility, bool enabled)
    {
        return enabled &&
               GameCompatibilityFeatures.SupportsMbfMonkeyClimbing(compatibility);
    }

    public static bool BlocksMove(
        GameCompatibility compatibility,
        bool enabled,
        Mobj thing,
        Fixed destinationFloor,
        Fixed destinationDropoff)
    {
        if (thing == null)
            return destinationFloor - destinationDropoff > MaxStep;

        if (!Applies(compatibility, enabled))
            return destinationFloor - destinationDropoff > MaxStep;

        // Original MBF p_map.c, P_TryMove():
        //   thing->floorz   - tmfloorz   > 24 ||
        //   thing->dropoffz - tmdropoffz > 24
        // Equality at exactly 24 units is allowed.
        return thing.FloorZ - destinationFloor > MaxStep ||
               thing.DropoffZ - destinationDropoff > MaxStep;
    }
}
