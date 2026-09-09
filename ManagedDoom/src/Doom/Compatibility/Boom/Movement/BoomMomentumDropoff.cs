using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Movement;

/// <summary>
/// Boom-era physical momentum movement may push an actor across a tall
/// dropoff. Voluntary monster movement still uses the normal ledge rule.
/// MBF owns its later comp_dropoff override, so MBF-family modes are handled
/// by MbfDropoffCompatibility instead of this Boom baseline policy.
/// </summary>
public static class BoomMomentumDropoff
{
    public static bool Allows(
        GameCompatibility compatibility,
        bool requested)
    {
        return requested &&
               GameCompatibilityFeatures.SupportsBoom(compatibility) &&
               !GameCompatibilityFeatures.SupportsMbf(compatibility);
    }
}
