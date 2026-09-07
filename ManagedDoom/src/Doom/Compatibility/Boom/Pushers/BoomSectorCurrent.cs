using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Pushers;

public static class BoomSectorCurrent
{
    public static void Initialize(World world)
    {
        if (!GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility))
            return;

        foreach (var sector in world.Map.Sectors)
        {
            sector.CurrentX = Fixed.Zero;
            sector.CurrentY = Fixed.Zero;
        }

        foreach (var line in world.Map.Lines)
        {
            if ((int)line.Special != 225)
                continue;

            var current = BoomPusherTranslator.ResolveCurrent(line.Dx, line.Dy);
            foreach (var sector in world.Map.BoomTags.GetSectors(line.Tag))
            {
                sector.CurrentX += current.X;
                sector.CurrentY += current.Y;
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

        // Boom currents do not affect things above the sector floor.
        if (thing.Z > sector.FloorHeight)
            return;

        thing.MomX += sector.CurrentX;
        thing.MomY += sector.CurrentY;
    }
}
