using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Movement;

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

        var deltaX = Fixed.Zero;
        var deltaY = Fixed.Zero;

        // Boom's T_Pusher does not use only thing->subsector->sector for
        // constant pushers. Each affected sector walks its touching_thinglist,
        // so a player straddling a sector boundary can receive wind from a
        // sector even when the player's origin is in the neighbouring sector.
        // Our touching-sector list is the inverse representation of that same
        // relationship, therefore accumulate every active touched sector here.
        var node = thing.TouchingSectorList;
        if (node != null)
        {
            for (; node != null; node = node.ThingNext)
            {
                AddSectorWind(thing, node.Sector, ref deltaX, ref deltaY);
            }
        }
        else
        {
            // Defensive fallback for partially constructed/dummy objects.
            AddSectorWind(thing, thing.Subsector.Sector, ref deltaX, ref deltaY);
        }

        thing.MomX += deltaX;
        thing.MomY += deltaY;
        MbfLedgeBlockCompatibility.MarkScrollingMovement(thing, deltaX, deltaY);
    }

    private static void AddSectorWind(Mobj thing, Sector sector, ref Fixed deltaX, ref Fixed deltaY)
    {
        if (sector == null || ((int)sector.Special & BoomPusherTranslator.PushMask) == 0)
            return;

        Fixed x;
        Fixed y;

        var heightSector = sector.HeightSector;
        if (heightSector == null)
        {
            // PrBoom/Boom: ordinary sector -> full force while airborne,
            // half force while standing on the floor.
            if (thing.Z > thing.FloorZ)
            {
                x = sector.WindAboveX;
                y = sector.WindAboveY;
            }
            else
            {
                x = sector.WindGroundX;
                y = sector.WindGroundY;
            }
        }
        else
        {
            // PrBoom heightsec/deep-water rule:
            //   above the fake floor -> full wind;
            //   view below it        -> no wind;
            //   otherwise            -> half wind (wading).
            var height = heightSector.FloorHeight;
            if (thing.Z > height)
            {
                x = sector.WindAboveX;
                y = sector.WindAboveY;
            }
            else if (thing.Player.ViewZ < height)
            {
                x = Fixed.Zero;
                y = Fixed.Zero;
            }
            else
            {
                x = sector.WindGroundX;
                y = sector.WindGroundY;
            }
        }

        deltaX += x;
        deltaY += y;
    }
}
