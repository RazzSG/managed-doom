using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.AI;

/// <summary>
/// MBF monster_infighting option. The option controls retaliation between
/// ordinary hostile monsters after one damages another. Opposing FRIEND
/// sides still fight normally, and pre-MBF compatibility is untouched.
/// </summary>
public static class MbfMonsterInfighting
{
    public static bool CanRetaliate(
        GameCompatibility compatibility,
        bool optionEnabled,
        Mobj victim,
        Mobj attacker)
    {
        if (!GameCompatibilityFeatures.SupportsMbfMonsterInfighting(compatibility))
            return true;

        if (victim == null || attacker == null)
            return false;

        // Friendly-vs-hostile combat is not monster infighting; it is the
        // normal MBF team relationship and remains enabled even when the
        // monster_infighting option is disabled. Friendly-vs-friendly is
        // rejected earlier by MbfFriendTargeting.CanAcquireTarget.
        var victimFriendly = MbfFriendTargeting.IsFriendly(compatibility, victim);
        var attackerFriendly = MbfFriendTargeting.IsFriendly(compatibility, attacker);
        if (victimFriendly != attackerFriendly)
            return true;

        // Players are never governed by the monster-infighting toggle.
        if (victim.Player != null || attacker.Player != null)
            return true;

        // Restrict the option to living sentient actors. This avoids changing
        // retaliation semantics for barrels and other shootable map objects.
        if (!IsSentientMonster(victim) || !IsSentientMonster(attacker))
            return true;

        return optionEnabled;
    }

    private static bool IsSentientMonster(Mobj actor) =>
        actor.Health > 0 &&
        actor.Info != null &&
        actor.Info.SeeState != MobjState.Null;
}
