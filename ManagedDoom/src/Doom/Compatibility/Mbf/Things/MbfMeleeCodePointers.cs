using ManagedDoom;

namespace ManagedDoom.Compatibility.Mbf.Things;

/// <summary>
/// MBF actor code pointers A_Scratch and A_BetaSkullAttack.
/// </summary>
public static class MbfMeleeCodePointers
{
    public static bool ScratchFromState(World world, Mobj actor)
    {
        if (!CanExecute(world, actor))
            return false;

        if (actor.Target == null)
            return true;

        world.MonsterBehavior.FaceTarget(actor);

        if (!world.MonsterBehavior.CheckMeleeRange(actor))
            return true;

        if (actor.State.Misc2 != 0)
            world.StartSound(actor, (Sfx)actor.State.Misc2, SfxType.Weapon);

        world.ThingInteraction.DamageMobj(
            actor.Target,
            actor,
            actor,
            actor.State.Misc1);

        return true;
    }

    public static bool BetaSkullAttack(World world, Mobj actor)
    {
        if (!CanExecute(world, actor))
            return false;

        var target = actor.Target;
        if (target == null || target.Type == MobjType.Skull)
            return true;

        world.StartSound(actor, actor.Info.AttackSound, SfxType.Voice);
        world.MonsterBehavior.FaceTarget(actor);

        var damage = (world.Random.Next() % 8 + 1) * actor.Info.Damage;
        world.ThingInteraction.DamageMobj(target, actor, actor, damage);

        return true;
    }

    private static bool CanExecute(World world, Mobj actor)
    {
        return world != null &&
               actor != null &&
               actor.State != null &&
               GameCompatibilityFeatures.SupportsMbfMeleeCodePointers(world.Options.Compatibility);
    }
}
