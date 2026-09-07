using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;

namespace ManagedDoom.Compatibility.Boom.Doors;

/// <summary>
/// Compatibility rules for Boom's fixes to the original vertical-door code.
/// Keep the old behavior intact for Vanilla compatibility and only opt into
/// the corrected behavior at the compatibility level that introduced it.
/// </summary>
public static class BoomDoorCompatibility
{
    public const int BlockingLineActivated = 1;
    public const int OtherLineActivated = 2;

    public static bool FixesBlazingDoorSounds(GameCompatibility compatibility) =>
        GameCompatibilityFeatures.SupportsBoom(compatibility);

    public static bool UsesTaggedManualDoorLighting(GameCompatibility compatibility) =>
        GameCompatibilityFeatures.SupportsBoom(compatibility);

    public static bool UsesGradualDoorLighting(GameCompatibility compatibility) =>
        GameCompatibilityFeatures.SupportsMbf(compatibility);

    public static int GetGeneralizedDoorLightTag(
        GameCompatibility compatibility,
        BoomTriggerType trigger,
        int tag)
    {
        if (!GameCompatibilityFeatures.SupportsBoom(compatibility) || tag == 0)
            return 0;

        return trigger is BoomTriggerType.PushOnce or BoomTriggerType.PushRepeat
            ? tag
            : 0;
    }

    /// <summary>
    /// Boom 2.02 and MBF fixed the classic "doortrack" monster behavior.
    /// The random byte must only be consumed after at least one special line
    /// actually activated, matching P_Move in the reference implementation.
    /// </summary>
    public static bool ShouldContinueAfterBlockedDoorAttempt(
        GameCompatibility compatibility,
        int activatedFlags,
        int randomByte)
    {
        if (activatedFlags == 0)
            return false;

        if (!GameCompatibilityFeatures.SupportsBoom(compatibility))
            return true;

        if (!GameCompatibilityFeatures.SupportsMbf(compatibility))
            return (randomByte & 3) != 0;

        var blockingLineActivated = (activatedFlags & BlockingLineActivated) != 0;
        return (randomByte >= 230) ^ blockingLineActivated;
    }
}
