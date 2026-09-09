using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Movement;

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

        var deltaX = Fixed.Zero;
        var deltaY = Fixed.Zero;

        // Like wind, Boom currents are sector thinkers which iterate the
        // sector's touching_thinglist. Do not restrict the effect to the
        // sector containing the player's origin.
        var node = thing.TouchingSectorList;
        if (node != null)
        {
            for (; node != null; node = node.ThingNext)
            {
                AddSectorCurrent(thing, node.Sector, ref deltaX, ref deltaY);
            }
        }
        else
        {
            AddSectorCurrent(thing, thing.Subsector.Sector, ref deltaX, ref deltaY);
        }

        thing.MomX += deltaX;
        thing.MomY += deltaY;
        MbfLedgeBlockCompatibility.MarkScrollingMovement(thing, deltaX, deltaY);
    }

    private static void AddSectorCurrent(Mobj thing, Sector sector, ref Fixed deltaX, ref Fixed deltaY)
    {
        if (sector == null || ((int)sector.Special & BoomPusherTranslator.PushMask) == 0)
            return;

        // PrBoom/Boom currents affect players on or below the affected floor.
        // For a heightsec sector the control floor is the water surface used by
        // the pusher thinker instead of the real sector floor.
        var effectiveFloor = sector.HeightSector?.FloorHeight ?? sector.FloorHeight;
        if (thing.Z > effectiveFloor)
            return;

        deltaX += sector.CurrentX;
        deltaY += sector.CurrentY;
    }
}
