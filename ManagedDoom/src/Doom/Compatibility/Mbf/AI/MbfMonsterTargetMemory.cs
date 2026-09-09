using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.AI;

/// <summary>
/// MBF's default "monsters remember" behavior. This helper only owns the
/// previous-target bookkeeping; target discovery remains in MonsterBehavior.
/// </summary>
public static class MbfMonsterTargetMemory
{
    public static void RememberCurrentEnemy(
        GameCompatibility compatibility,
        Mobj actor,
        Mobj newTarget)
    {
        RememberCurrentEnemy(compatibility, true, actor, newTarget);
    }

    public static void RememberCurrentEnemy(
        GameCompatibility compatibility,
        bool enabled,
        Mobj actor,
        Mobj newTarget)
    {
        if (!enabled ||
            !GameCompatibilityFeatures.SupportsMbfMonsterMemory(compatibility) ||
            actor == null ||
            newTarget == null)
        {
            return;
        }

        var current = actor.Target;
        if (current == null ||
            object.ReferenceEquals(current, newTarget) ||
            !MbfFriendTargeting.CanAcquireTarget(compatibility, actor, current))
        {
            return;
        }

        actor.LastEnemy = current;
    }

    public static bool TryRestorePreviousEnemy(
        GameCompatibility compatibility,
        Mobj actor)
    {
        return TryRestorePreviousEnemy(compatibility, true, actor);
    }

    public static bool TryRestorePreviousEnemy(
        GameCompatibility compatibility,
        bool enabled,
        Mobj actor)
    {
        if (!enabled || !GameCompatibilityFeatures.SupportsMbfMonsterMemory(compatibility) || actor == null)
            return false;

        var previous = actor.LastEnemy;
        actor.LastEnemy = null;

        if (!MbfFriendTargeting.CanAcquireTarget(compatibility, actor, previous))
            return false;

        actor.Target = previous;
        return true;
    }
}
