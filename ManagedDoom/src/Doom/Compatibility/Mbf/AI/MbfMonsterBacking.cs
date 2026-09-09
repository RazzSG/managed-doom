using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.AI;

/// <summary>
/// MBF ranged-monster backing rule from P_NewChaseDir. Backing is an
/// offensive spacing behavior: it is used only when the actor has a ranged
/// attack and the live opposing target is close enough to favor melee. The
/// caller owns MBF's separate StrafeCount lifetime/facing state.
/// </summary>
public static class MbfMonsterBacking
{
    private static readonly Fixed meleeTargetDistance = WeaponBehavior.MeleeRange * 2;
    private static readonly Fixed playerMeleeDistance = WeaponBehavior.MeleeRange * 3;

    public static bool ShouldBackAway(
        GameCompatibility compatibility,
        bool optionEnabled,
        Mobj actor,
        Mobj target)
    {
        if (!GameCompatibilityFeatures.SupportsMbfMonsterBacking(compatibility) ||
            !optionEnabled ||
            actor == null ||
            target == null ||
            actor.Info == null ||
            target.Info == null ||
            target.Health <= 0 ||
            actor.Info.MissileState == 0 ||
            actor.Type == MobjType.Skull)
        {
            return false;
        }

        // Original MBF only backs away across the FRIEND boundary. Two
        // ordinary hostile monsters can still infight, but do not use this
        // smarter backing behavior against each other.
        var actorFriendly = MbfFriendTargeting.IsFriendly(compatibility, actor);
        var targetFriendly = MbfFriendTargeting.IsFriendly(compatibility, target);
        if (actorFriendly == targetFriendly)
            return false;

        var distance = Geometry.AproxDistance(
            target.X - actor.X,
            target.Y - actor.Y);

        if (target.Info.MissileState == 0 && distance < meleeTargetDistance)
            return true;

        if (target.Player == null || distance >= playerMeleeDistance)
            return false;

        return target.Player.ReadyWeapon == WeaponType.Fist ||
               target.Player.ReadyWeapon == WeaponType.Chainsaw;
    }
}
