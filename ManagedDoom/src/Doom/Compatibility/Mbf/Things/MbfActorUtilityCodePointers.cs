using ManagedDoom;

namespace ManagedDoom.Compatibility.Mbf.Things;

/// <summary>
/// Parameterless MBF actor utility code pointers: A_Die, A_Detonate and A_Stop.
/// </summary>
public static class MbfActorUtilityCodePointers
{
    public static bool Die(World world, Mobj actor)
    {
        if (!CanExecute(world, actor))
            return false;

        world.ThingInteraction.DamageMobj(actor, null, null, actor.Health);
        return true;
    }

    public static bool Detonate(World world, Mobj actor)
    {
        if (!CanExecute(world, actor))
            return false;

        world.ThingInteraction.RadiusAttack(actor, actor.Target, actor.Info.Damage);
        return true;
    }

    public static bool Stop(World world, Mobj actor)
    {
        if (!CanExecute(world, actor))
            return false;

        actor.MomX = Fixed.Zero;
        actor.MomY = Fixed.Zero;
        actor.MomZ = Fixed.Zero;
        return true;
    }

    private static bool CanExecute(World world, Mobj actor)
    {
        return world != null &&
               actor != null &&
               GameCompatibilityFeatures.SupportsMbfActorUtilityCodePointers(world.Options.Compatibility);
    }
}
