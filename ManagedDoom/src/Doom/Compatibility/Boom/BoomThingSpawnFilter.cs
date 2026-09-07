namespace ManagedDoom.Compatibility.Boom;

public static class BoomThingSpawnFilter
{
    public static bool IsAllowed(GameOptions options, ThingFlags flags)
    {
        // Vanilla Doom's multiplayer-only flag is always meaningful.
        if (!options.NetGame)
            return (flags & ThingFlags.MultiplayerOnly) == 0;

        // Boom's additional multiplayer-mode filters are ignored when the
        // effective compatibility is explicitly Vanilla.
        if (!GameCompatibilityFeatures.SupportsBoom(options.Compatibility))
            return true;

        if (options.Deathmatch != 0)
            return (flags & ThingFlags.NotDeathmatch) == 0;

        return (flags & ThingFlags.NotCooperative) == 0;
    }
}
