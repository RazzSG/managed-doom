using ManagedDoom;

namespace ManagedDoom.Compatibility.Mbf.Things;

/// <summary>
/// MBF A_Mushroom code pointer.
/// </summary>
public static class MbfMushroomCodePointer
{
    private static readonly Fixed DefaultVerticalScale = Fixed.FromInt(4);
    private static readonly Fixed DefaultMomentumScale = new(Fixed.FracUnit / 2);

    public static bool Execute(World world, Mobj actor)
    {
        if (!CanExecute(world, actor))
            return false;

        var state = actor.State;
        var verticalScale = state.Misc1 != 0
            ? new Fixed(state.Misc1)
            : DefaultVerticalScale;
        var momentumScale = state.Misc2 != 0
            ? new Fixed(state.Misc2)
            : DefaultMomentumScale;
        var range = actor.Info.Damage;

        // MBF first performs the normal A_Explode effect.
        world.MonsterBehavior.Explode(actor);

        // Then launch FatShot actors toward a square grid of synthetic
        // targets. Misc1/Misc2 are 16.16 fixed-point values in MBF.
        for (var i = -range; i <= range; i += 8)
        {
            for (var j = -range; j <= range; j += 8)
            {
                var targetX = actor.X + Fixed.FromInt(i);
                var targetY = actor.Y + Fixed.FromInt(j);
                var horizontalDistance = Geometry.AproxDistance(
                    Fixed.FromInt(i),
                    Fixed.FromInt(j));

                // P_SpawnMissile only needs the target position and shadow
                // flag here. The original MBF code used a shallow actor copy.
                var target = new Mobj(world)
                {
                    X = targetX,
                    Y = targetY,
                    Z = actor.Z + horizontalDistance * verticalScale,
                    Flags = actor.Flags
                };

                var missile = world.ThingAllocation.SpawnMissile(
                    actor,
                    target,
                    MobjType.Fatshot);

                missile.MomX *= momentumScale;
                missile.MomY *= momentumScale;
                missile.MomZ *= momentumScale;
                missile.Flags &= ~MobjFlags.NoGravity;
            }
        }

        return true;
    }

    private static bool CanExecute(World world, Mobj actor)
    {
        return world != null &&
               actor != null &&
               actor.State != null &&
               actor.Info != null &&
               GameCompatibilityFeatures.SupportsMbfMushroomCodePointer(world.Options.Compatibility);
    }
}
