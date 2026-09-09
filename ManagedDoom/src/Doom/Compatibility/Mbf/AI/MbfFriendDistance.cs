using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.AI;

/// <summary>
/// MBF friend-distance steering from P_NewChaseDir. When two friendly actors
/// are closer than the configured comfort distance, the chaser reverses its
/// target delta and tries to move away instead of freezing in place.
/// </summary>
public static class MbfFriendDistance
{
    public const int DefaultDistanceUnits = 128;
    public const int MaxDistanceUnits = 999;

    private static readonly Fixed defaultDistance = Fixed.FromInt(DefaultDistanceUnits);

    public static bool ShouldMoveAway(
        World world,
        Mobj actor,
        Mobj target)
    {
        return ShouldMoveAway(world, actor, target, defaultDistance);
    }

    public static bool ShouldMoveAway(
        World world,
        Mobj actor,
        Mobj target,
        Fixed comfortDistance)
    {
        if (world == null)
            return false;

        var compatibility = world.Options.Compatibility;
        if (!GameCompatibilityFeatures.SupportsMbfFriendDistance(compatibility) ||
            actor == null ||
            target == null ||
            comfortDistance <= Fixed.Zero ||
            !MbfFriendTargeting.AreOnSameSide(compatibility, actor, target))
        {
            return false;
        }

        var distance = Geometry.AproxDistance(
            target.X - actor.X,
            target.Y - actor.Y);

        if (distance >= comfortDistance)
            return false;

        // Match MBF's short-circuit order: the more expensive lift/tag and
        // touching-sector checks are only needed for an actually-close friend.
        return !IsOnLift(world, target) &&
               !MbfMonsterHazardAvoidance.IsUnderDamage(actor);
    }

    /// <summary>
    /// Pure decision overload used by tests and by callers that already know
    /// the environmental state. Original MBF suppresses friend spacing on a
    /// lift or while the actor is under crusher/ceiling danger so crowded
    /// situations can resolve naturally.
    /// </summary>
    public static bool ShouldMoveAway(
        GameCompatibility compatibility,
        Mobj actor,
        Mobj target,
        Fixed comfortDistance,
        bool targetOnLift,
        bool actorUnderDamage)
    {
        if (!GameCompatibilityFeatures.SupportsMbfFriendDistance(compatibility) ||
            actor == null ||
            target == null ||
            comfortDistance <= Fixed.Zero ||
            targetOnLift ||
            actorUnderDamage ||
            !MbfFriendTargeting.AreOnSameSide(compatibility, actor, target))
        {
            return false;
        }

        var distance = Geometry.AproxDistance(
            target.X - actor.X,
            target.Y - actor.Y);

        return distance < comfortDistance;
    }

    /// <summary>
    /// Mirrors MBF P_IsOnLift closely enough for the friend-distance AI path:
    /// an active platform counts immediately, otherwise a non-zero tagged
    /// sector counts when one of the original lift specials targets its tag.
    /// The map's BoomTagIndex keeps this lookup allocation-free and avoids a
    /// full linedef scan each time NewChaseDir runs.
    /// </summary>
    public static bool IsOnLift(World world, Mobj actor)
    {
        if (world?.Map == null || actor?.Subsector?.Sector == null)
            return false;

        var sector = actor.Subsector.Sector;

        if (sector.FloorData is Platform)
            return true;

        if (sector.Tag == 0)
            return false;

        var lines = world.Map.BoomTags.GetLines(sector.Tag);
        for (var i = 0; i < lines.Length; i++)
        {
            if (IsLiftSpecial(lines[i].Special))
                return true;
        }

        return false;
    }

    public static bool IsLiftSpecial(LineSpecial special)
    {
        return (int)special switch
        {
            10 or 14 or 15 or 20 or 21 or 22 or
            47 or 53 or 62 or 66 or 67 or 68 or
            87 or 88 or 95 or 120 or 121 or 122 or 123 or
            143 or 144 or 148 or 149 or
            162 or 163 or 181 or 182 or 211 or
            227 or 228 or 231 or 232 or 235 or 236 => true,
            _ => false
        };
    }
}
