using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.AI;

/// <summary>
/// MBF's monster_avoid_hazards movement rule. The original MBF AI treats
/// any active ceiling mover touching the actor as a hazard: an upward mover
/// is moderate, while a downward mover is serious. Monsters that were
/// already in danger are allowed to keep moving so they can escape.
/// </summary>
public static class MbfMonsterHazardAvoidance
{
    public const int ModerateAvoidanceThreshold = 200;

    public static bool Applies(GameCompatibility compatibility) =>
        Applies(compatibility, true);

    public static bool Applies(GameCompatibility compatibility, bool enabled) =>
        enabled && GameCompatibilityFeatures.SupportsMbfMonsterHazardAvoidance(compatibility);

    /// <summary>
    /// Mirrors MBF P_IsUnderDamage: 0 means no active ceiling movement,
    /// positive means moderate danger, and negative means serious danger.
    /// The direction values are ORed across every sector touched by the
    /// actor's radius, matching MBF's msecnode traversal.
    /// </summary>
    public static int GetCurrentHazardDirection(Mobj actor)
    {
        if (actor == null)
            return 0;

        var direction = 0;

        for (var node = actor.TouchingSectorList; node != null; node = node.ThingNext)
        {
            if (node.Sector?.CeilingData is CeilingMove ceiling)
                direction |= ceiling.Direction;
        }

        return direction;
    }

    public static bool IsUnderDamage(Mobj actor) => GetCurrentHazardDirection(actor) != 0;

    /// <summary>
    /// Decides whether a successful step into a newly hazardous area should
    /// force a new chase direction. Serious (downward) ceiling movement is
    /// always avoided. Moderate movement is avoided on random values 0..199,
    /// preserving MBF's 200/256 probability.
    /// </summary>
    public static bool ShouldAbandonDirection(
        GameCompatibility compatibility,
        bool wasUnderDamage,
        int currentHazardDirection,
        int randomByte)
    {
        if (!Applies(compatibility) || wasUnderDamage || currentHazardDirection == 0)
            return false;

        if (currentHazardDirection < 0)
            return true;

        return (randomByte & 0xff) < ModerateAvoidanceThreshold;
    }
}
