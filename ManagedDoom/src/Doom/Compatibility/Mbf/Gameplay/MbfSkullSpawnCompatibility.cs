using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Gameplay;

namespace ManagedDoom.Compatibility.Mbf.Gameplay;

/// <summary>
/// Selects the Pain Elemental Lost Soul spawn-safety fixes for MBF.
/// comp_skull restores Doom-compatible unsafe spawning when enabled.
/// </summary>
public static class MbfSkullSpawnCompatibility
{
    public static bool UsesSafeLostSoulSpawn(
        GameCompatibility compatibility,
        bool compSkull)
    {
        if (!GameCompatibilityFeatures.SupportsMbfSkullSpawnCompatibility(compatibility))
            return BoomGameplayBugFixes.UsesSafePainElementalLostSoulSpawn(compatibility);

        return !compSkull;
    }

    public static bool IsLostSoulSpawnBlocked(
        World world,
        Mobj actor,
        Fixed x,
        Fixed y)
    {
        if (!UsesSafeLostSoulSpawn(
                world.Options.Compatibility,
                world.Options.MbfOptions.CompSkull))
        {
            return false;
        }

        return BoomGameplayBugFixes.IsPainElementalLostSoulSpawnBlocked(
            world,
            actor,
            x,
            y);
    }

    public static bool IsLostSoulOutsideVerticalBounds(
        World world,
        Fixed z,
        Fixed height,
        Fixed floorHeight,
        Fixed ceilingHeight)
    {
        if (!UsesSafeLostSoulSpawn(
                world.Options.Compatibility,
                world.Options.MbfOptions.CompSkull))
        {
            return false;
        }

        return BoomGameplayBugFixes.IsPainElementalLostSoulOutsideVerticalBounds(
            world.Options.Compatibility,
            z,
            height,
            floorHeight,
            ceilingHeight);
    }
}
