using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Pushers;

public static class BoomSectorWind
{
    public static void Initialize(World world)
    {
        if (!GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility))
            return;

        foreach (var sector in world.Map.Sectors)
        {
            sector.WindAboveX = Fixed.Zero;
            sector.WindAboveY = Fixed.Zero;
            sector.WindGroundX = Fixed.Zero;
            sector.WindGroundY = Fixed.Zero;
        }

        foreach (var line in world.Map.Lines)
        {
            if ((int)line.Special != 224)
                continue;

            var wind = BoomPusherTranslator.ResolveWind(line.Dx, line.Dy);
            foreach (var sector in world.Map.BoomTags.GetSectors(line.Tag))
            {
                sector.WindAboveX += wind.FullX;
                sector.WindAboveY += wind.FullY;
                sector.WindGroundX += wind.GroundX;
                sector.WindGroundY += wind.GroundY;
            }
        }
    }

    public static void Apply(Mobj thing)
    {
        if (thing?.Player == null || thing.Subsector == null)
            return;

        if ((thing.Flags & (MobjFlags.NoGravity | MobjFlags.NoClip)) != 0)
            return;

        var sector = thing.Subsector.Sector;
        if (((int)sector.Special & BoomPusherTranslator.PushMask) == 0)
            return;

        if (thing.Z > thing.FloorZ)
        {
            thing.MomX += sector.WindAboveX;
            thing.MomY += sector.WindAboveY;
        }
        else
        {
            thing.MomX += sector.WindGroundX;
            thing.MomY += sector.WindGroundY;
        }
    }
}
