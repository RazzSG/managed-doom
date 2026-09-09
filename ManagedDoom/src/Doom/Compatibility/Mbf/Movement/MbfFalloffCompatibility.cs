using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.Movement;

/// <summary>
/// Selects MBF's ledge pseudo-torque compatibility behavior.
///
/// comp_falloff = 0 is the MBF default and allows non-sentient,
/// gravity-affected objects to fall from ledges under simulated torque.
/// comp_falloff = 1 restores Doom-compatible behavior and disables it.
/// Boom predates this MBF feature and therefore never enables the torque path.
/// </summary>
public static class MbfFalloffCompatibility
{
    public static bool UsesLedgeTorque(
        GameCompatibility compatibility,
        bool compFalloff)
    {
        return GameCompatibilityFeatures.SupportsMbfFalloffCompatibility(compatibility) &&
               !compFalloff;
    }
}
