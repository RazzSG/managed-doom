using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.AI;

/// <summary>
/// MBF comp_pursuit target-retention rules. The option is a Doom-compatibility
/// switch: enabled preserves a living single-player target even without sight;
/// disabled allows MBF to periodically reconsider the target.
/// </summary>
public static class MbfPursuit
{
    public const int BaseThreshold = 100;
    public const int PlayerAcquireThreshold = 60;

    public static void ApplyPlayerAcquireThreshold(
        GameCompatibility compatibility,
        bool compPursuit,
        Mobj actor)
    {
        if (actor == null ||
            !GameCompatibilityFeatures.SupportsMbfPursuit(compatibility) ||
            compPursuit)
        {
            return;
        }

        actor.Threshold = PlayerAcquireThreshold;
    }

    public static bool ShouldKeepCurrentTarget(
        GameCompatibility compatibility,
        bool compPursuit,
        bool netGame,
        bool monsterInfighting,
        Mobj actor,
        Mobj target,
        bool targetVisible)
    {
        if (!GameCompatibilityFeatures.SupportsMbfPursuit(compatibility) ||
            actor == null ||
            target == null ||
            target.Health <= 0)
        {
            return false;
        }

        if (compPursuit && !netGame)
            return true;

        var actorFriendly = MbfFriendTargeting.IsFriendly(compatibility, actor);
        var targetFriendly = MbfFriendTargeting.IsFriendly(compatibility, target);
        var opposingSides = actorFriendly != targetFriendly;
        var validTarget = opposingSides || (!actorFriendly && monsterInfighting);

        return validTarget && targetVisible;
    }
}
