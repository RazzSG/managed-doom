using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Movement;

namespace ManagedDoom.Compatibility.Mbf.Movement;

/// <summary>
/// MBF compatibility selection for teleport stomping. PrBoom uses the
/// classic MAP30 rule when comp_telefrag is enabled, and its corrected
/// boss-teleport rule when the option is disabled.
/// </summary>
public static class MbfTelefragCompatibility
{
    public static bool CanTelefragAtDestination(
        Mobj thing,
        bool bossTeleport,
        int mapNumber,
        GameCompatibility compatibility,
        bool compTelefrag)
    {
        if (!GameCompatibilityFeatures.SupportsMbfTelefragCompatibility(compatibility))
        {
            return BoomTeleportQuirks.CanTelefragAtDestination(
                thing,
                bossTeleport,
                mapNumber,
                compatibility);
        }

        if (thing.Player != null)
            return true;

        return compTelefrag ? mapNumber == 30 : bossTeleport;
    }
}
