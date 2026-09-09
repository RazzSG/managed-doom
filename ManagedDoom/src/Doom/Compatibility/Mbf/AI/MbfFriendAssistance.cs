using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.AI;

/// <summary>
/// MBF help_friends behavior. When enabled, a healthy-enough monster may
/// temporarily switch to the attacker of a badly hurt same-faction monster
/// that it can see. The option defaults to off, matching MBF.
/// </summary>
public static class MbfFriendAssistance
{
    public const int HealthyFriendStopThreshold = 180;
    public const int RescueThreshold = 100;

    public static bool TryAcquireThreat(World world, bool optionEnabled, Mobj actor)
    {
        if (world == null ||
            actor == null ||
            !optionEnabled ||
            !GameCompatibilityFeatures.SupportsMbfHelpFriends(world.Options.Compatibility) ||
            !IsSentientMonster(actor) ||
            (long)actor.Health * 3 < actor.Info.SpawnHealth)
        {
            return false;
        }

        var compatibility = world.Options.Compatibility;

        foreach (var thinker in world.Thinkers)
        {
            if (thinker is not Mobj friend ||
                object.ReferenceEquals(friend, actor) ||
                friend.Player != null ||
                !IsSentientMonster(friend) ||
                !AreFactionAllies(compatibility, actor, friend))
            {
                continue;
            }

            // Original MBF probabilistically stops searching when it reaches a
            // healthy ally. Consume this random value only while help_friends
            // is enabled, so the default path remains untouched.
            if ((long)friend.Health * 2 >= friend.Info.SpawnHealth)
            {
                if (ShouldStopAtHealthyFriend(world.Random.Next()))
                    break;

                continue;
            }

            if ((friend.Flags & MobjFlags.JustHit) == 0 ||
                friend.Target == null ||
                object.ReferenceEquals(friend.Target, actor.Target))
            {
                continue;
            }

            var threat = friend.Target;
            if (!CanAssistAgainst(compatibility, actor, threat))
                continue;

            // PIT_FindTarget has one extra MBF target-selection rule: if the
            // candidate is already in a reciprocal fight with a healthy
            // opponent, it is skipped roughly 60% of the time. The random
            // byte is consumed only for a reciprocal engagement, and before
            // the sight test, matching the original evaluation order.
            if (IsReciprocallyEngaged(threat) &&
                ShouldSkipEngagedThreat(
                    compatibility,
                    threat,
                    world.Random.Next()))
            {
                continue;
            }

            if (!world.VisibilityCheck.CheckSight(actor, threat))
                continue;

            MbfMonsterTargetMemory.RememberCurrentEnemy(
                compatibility,
                world.Options.MbfOptions.MonstersRemember,
                actor,
                threat);

            actor.Target = threat;
            actor.Threshold = RescueThreshold;
            return true;
        }

        return false;
    }

    public static bool CanAssistAgainst(
        GameCompatibility compatibility,
        Mobj actor,
        Mobj threat)
    {
        if (!GameCompatibilityFeatures.SupportsMbfHelpFriends(compatibility) ||
            actor == null ||
            threat == null ||
            threat.Player != null ||
            threat.Health <= 0 ||
            !IsMonsterTarget(threat))
        {
            return false;
        }

        // P_HelpFriend ultimately routes through MBF's monster target finder,
        // which only accepts the opposite FRIEND class. Ordinary hostile-vs-
        // hostile infighting is therefore not a rescue target.
        return MbfFriendTargeting.IsFriendly(compatibility, actor) !=
               MbfFriendTargeting.IsFriendly(compatibility, threat);
    }

    public static bool ShouldStopAtHealthyFriend(int randomByte) =>
        (randomByte & 0xff) < HealthyFriendStopThreshold;

    public static bool IsReciprocallyEngaged(Mobj threat) =>
        threat?.Target != null &&
        object.ReferenceEquals(threat.Target.Target, threat);

    public static bool ShouldSkipEngagedThreat(
        GameCompatibility compatibility,
        Mobj threat,
        int randomByte)
    {
        if (!GameCompatibilityFeatures.SupportsMbfHelpFriends(compatibility) ||
            !IsReciprocallyEngaged(threat))
        {
            return false;
        }

        var opponent = threat.Target;
        if ((randomByte & 0xff) <= 100 ||
            opponent.Info == null ||
            opponent.Health <= 0 ||
            MbfFriendTargeting.IsFriendly(compatibility, threat) ==
                MbfFriendTargeting.IsFriendly(compatibility, opponent))
        {
            return false;
        }

        return (long)opponent.Health * 2 >= opponent.Info.SpawnHealth;
    }

    private static bool AreFactionAllies(
        GameCompatibility compatibility,
        Mobj first,
        Mobj second) =>
        MbfFriendTargeting.IsFriendly(compatibility, first) ==
        MbfFriendTargeting.IsFriendly(compatibility, second);

    private static bool IsSentientMonster(Mobj actor) =>
        actor.Player == null &&
        actor.Health > 0 &&
        actor.Info != null &&
        actor.Info.SeeState != MobjState.Null;

    private static bool IsMonsterTarget(Mobj candidate) =>
        candidate.Info != null &&
        (((candidate.Flags & MobjFlags.CountKill) != 0) || candidate.Type == MobjType.Skull);
}
