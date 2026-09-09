using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.AI;

public static class MbfFriendTargeting
{
    public static bool IsFriendly(GameCompatibility compatibility, Mobj actor)
    {
        if (!GameCompatibilityFeatures.SupportsMbfFriendAi(compatibility) || actor == null)
            return false;

        return actor.Player != null || (actor.Flags & MobjFlags.Friend) != 0;
    }

    public static bool AreOnSameSide(
        GameCompatibility compatibility,
        Mobj first,
        Mobj second)
    {
        if (!GameCompatibilityFeatures.SupportsMbfFriendAi(compatibility) ||
            first == null ||
            second == null)
        {
            return false;
        }

        // MBF keeps the classic monster-infighting default enabled. Friendly
        // actors form an allied side with players, but two ordinary hostile
        // monsters are still allowed to become enemies of each other.
        return IsFriendly(compatibility, first) && IsFriendly(compatibility, second);
    }

    public static bool CanAcquireTarget(
        GameCompatibility compatibility,
        Mobj actor,
        Mobj candidate)
    {
        // Preserve the pre-MBF path exactly. The caller already applies
        // the classic Doom validity checks before asking this helper.
        if (!GameCompatibilityFeatures.SupportsMbfFriendAi(compatibility))
            return true;

        if (actor == null ||
            candidate == null ||
            object.ReferenceEquals(actor, candidate) ||
            candidate.Health <= 0 ||
            (candidate.Flags & MobjFlags.Shootable) == 0)
        {
            return false;
        }

        return !AreOnSameSide(compatibility, actor, candidate);
    }

    public static bool TryFindHostileMonster(
        World world,
        Mobj actor,
        bool allAround,
        out Mobj target)
    {
        target = null;

        if (world == null ||
            actor == null ||
            !GameCompatibilityFeatures.SupportsMbfFriendAi(world.Options.Compatibility) ||
            !IsFriendly(world.Options.Compatibility, actor))
        {
            return false;
        }

        foreach (var thinker in world.Thinkers)
        {
            if (thinker is not Mobj candidate ||
                candidate.Player != null ||
                !IsMonsterTarget(candidate) ||
                !CanAcquireTarget(world.Options.Compatibility, actor, candidate))
            {
                continue;
            }

            if (!IsVisible(world, actor, candidate, allAround))
                continue;

            target = candidate;
            return true;
        }

        return false;
    }

    public static bool TryFindPlayerToFollow(
        World world,
        Mobj actor,
        bool allAround,
        out Mobj target)
    {
        target = null;

        if (world == null ||
            actor == null ||
            !GameCompatibilityFeatures.SupportsMbfFriendAi(world.Options.Compatibility) ||
            !IsFriendly(world.Options.Compatibility, actor))
        {
            return false;
        }

        var players = world.Options.Players;

        // Prefer a live player that is currently visible. If none can be seen,
        // MBF friends still return to a live player instead of going dormant.
        for (var pass = 0; pass < 2; pass++)
        {
            for (var i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (!player.InGame || player.Health <= 0 || player.Mobj == null)
                    continue;

                if (pass == 0 && !IsVisible(world, actor, player.Mobj, allAround))
                    continue;

                target = player.Mobj;
                return true;
            }
        }

        return false;
    }

    private static bool IsMonsterTarget(Mobj candidate) =>
        candidate.Health > 0 &&
        (((candidate.Flags & MobjFlags.CountKill) != 0) || candidate.Type == MobjType.Skull);

    private static bool IsVisible(
        World world,
        Mobj actor,
        Mobj candidate,
        bool allAround)
    {
        if (!allAround && IsBehindBeyondAwarenessRange(actor, candidate))
            return false;

        return world.VisibilityCheck.CheckSight(actor, candidate);
    }

    private static bool IsBehindBeyondAwarenessRange(Mobj actor, Mobj candidate)
    {
        var angle = Geometry.PointToAngle(
            actor.X,
            actor.Y,
            candidate.X,
            candidate.Y) - actor.Angle;

        if (angle <= Angle.Ang90 || angle >= Angle.Ang270)
            return false;

        var distance = Geometry.AproxDistance(
            candidate.X - actor.X,
            candidate.Y - actor.Y);

        return distance > WeaponBehavior.MeleeRange;
    }
}
