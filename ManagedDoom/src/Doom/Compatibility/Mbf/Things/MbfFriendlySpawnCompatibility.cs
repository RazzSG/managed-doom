using ManagedDoom;

namespace ManagedDoom.Compatibility.Mbf.Things;

/// <summary>
/// MBF A_Spawn compatibility. The actor type stored in Misc1 is one-based,
/// Misc2 is a whole-map-unit Z offset, and comp_friendlyspawn controls whether
/// the spawned actor inherits the caller's FRIEND flag.
/// </summary>
public static class MbfFriendlySpawnCompatibility
{
    public static Mobj SpawnFromState(World world, Mobj source)
    {
        if (world == null ||
            source == null ||
            source.State == null ||
            !GameCompatibilityFeatures.SupportsMbfFriendlySpawnCompatibility(world.Options.Compatibility))
        {
            return null;
        }

        var dehackedType = source.State.Misc1;
        if (dehackedType == 0)
            return null;

        var typeIndex = dehackedType - 1;
        if ((uint)typeIndex >= (uint)DoomInfo.MobjInfos.Length)
            return null;

        var spawned = world.ThingAllocation.SpawnMobj(
            source.X,
            source.Y,
            source.Z + Fixed.FromInt(source.State.Misc2),
            (MobjType)typeIndex);

        if (world.Options.MbfOptions.CompFriendlySpawn)
        {
            spawned.Flags =
                (spawned.Flags & ~MobjFlags.Friend) |
                (source.Flags & MobjFlags.Friend);
        }

        return spawned;
    }
}
