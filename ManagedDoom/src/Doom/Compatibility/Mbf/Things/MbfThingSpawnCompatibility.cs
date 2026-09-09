namespace ManagedDoom.Compatibility.Mbf.Things;

/// <summary>
/// MBF map-thing flag rules applied before the common Doom/Boom spawn filters.
/// This class only resolves map-format semantics; friendly AI is handled by the
/// later MBF actor-behavior layer.
/// </summary>
public static class MbfThingSpawnCompatibility
{
    private const ThingFlags DoomThingFlagsMask =
        ThingFlags.Easy |
        ThingFlags.Normal |
        ThingFlags.Hard |
        ThingFlags.Ambush |
        ThingFlags.MultiplayerOnly;

    public static ThingFlags ResolveMapFlags(
        GameCompatibility compatibility,
        ThingFlags flags)
    {
        if (!GameCompatibilityFeatures.SupportsMbfThingFlags(compatibility))
            return flags;

        // MBF reserves bit 8 as an old-editor compatibility marker. If it is
        // present, all post-Doom map-thing flag bits must be ignored.
        if ((flags & ThingFlags.Reserved) != 0)
            return flags & DoomThingFlagsMask;

        return flags;
    }

    public static bool HasFriendlyFlag(
        GameCompatibility compatibility,
        ThingFlags resolvedFlags) =>
        GameCompatibilityFeatures.SupportsMbfThingFlags(compatibility) &&
        (resolvedFlags & ThingFlags.Friendly) != 0;
}
