using ManagedDoom.Compatibility.Boom.Doors;
using ManagedDoom.Compatibility.Boom.Lines;

namespace ManagedDoom.Compatibility.Mbf.Doors;

/// <summary>
/// Selects MBF tagged manual-door lighting semantics. Boom changes tagged
/// manual-door lighting only at the fully-open/fully-closed endpoints. MBF
/// replaces that with gradual lighting while the door moves. Enabling
/// comp_doorlight restores Doom-compatible behavior by disabling the tagged
/// manual-door lighting effect entirely.
/// </summary>
public static class MbfDoorLightingCompatibility
{
    public static bool UsesTaggedManualDoorLighting(
        GameCompatibility compatibility,
        bool compDoorLight)
    {
        if (!GameCompatibilityFeatures.SupportsMbfDoorLightingCompatibility(compatibility))
            return BoomDoorCompatibility.UsesTaggedManualDoorLighting(compatibility);

        return !compDoorLight;
    }

    public static bool UsesGradualDoorLighting(
        GameCompatibility compatibility,
        bool compDoorLight)
    {
        if (!GameCompatibilityFeatures.SupportsMbfDoorLightingCompatibility(compatibility))
            return BoomDoorCompatibility.UsesGradualDoorLighting(compatibility);

        return !compDoorLight;
    }

    public static int GetGeneralizedDoorLightTag(
        GameCompatibility compatibility,
        bool compDoorLight,
        BoomTriggerType trigger,
        int tag)
    {
        if (!UsesTaggedManualDoorLighting(compatibility, compDoorLight))
            return 0;

        return BoomDoorCompatibility.GetGeneralizedDoorLightTag(
            compatibility,
            trigger,
            tag);
    }
}
