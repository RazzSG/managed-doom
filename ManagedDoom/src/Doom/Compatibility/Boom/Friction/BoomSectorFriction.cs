using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Friction;

public static class BoomSectorFriction
{
    public static void Initialize(World world)
    {
        if (!GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility))
            return;

        foreach (var sector in world.Map.Sectors)
        {
            sector.Friction = BoomFrictionTranslator.OriginalFriction;
            sector.MoveFactor = BoomFrictionTranslator.OriginalMoveFactor;
        }

        foreach (var line in world.Map.Lines)
        {
            if ((int)line.Special != 223)
                continue;

            var resolved = BoomFrictionTranslator.Resolve(line.Dx, line.Dy);
            foreach (var sector in world.Map.BoomTags.GetSectors(line.Tag))
            {
                sector.Friction = resolved.Friction;
                sector.MoveFactor = resolved.MoveFactor;
            }
        }
    }

    public static Fixed GetFriction(Mobj thing)
    {
        if (!TryGetSectorFriction(thing, out var sector))
            return BoomFrictionTranslator.OriginalFriction;

        return sector.Friction;
    }

    public static Fixed GetMoveFactor(Mobj thing)
    {
        if (!TryGetSectorFriction(thing, out var sector))
            return BoomFrictionTranslator.OriginalMoveFactor;

        var moveFactor = sector.MoveFactor;
        if (sector.Friction < BoomFrictionTranslator.OriginalFriction)
        {
            var momentum = Geometry.AproxDistance(thing.MomX, thing.MomY).Data;
            if (momentum > (BoomFrictionTranslator.MoreFrictionMomentumData << 2))
            {
                moveFactor <<= 3;
            }
            else if (momentum > (BoomFrictionTranslator.MoreFrictionMomentumData << 1))
            {
                moveFactor <<= 2;
            }
            else if (momentum > BoomFrictionTranslator.MoreFrictionMomentumData)
            {
                moveFactor <<= 1;
            }
        }

        return moveFactor;
    }

    private static bool TryGetSectorFriction(Mobj thing, out Sector sector)
    {
        sector = null;

        if (thing?.Player == null || thing.Subsector == null)
            return false;

        if ((thing.Flags & (MobjFlags.NoClip | MobjFlags.NoGravity)) != 0)
            return false;

        sector = thing.Subsector.Sector;
        if (((int)sector.Special & BoomFrictionTranslator.FrictionMask) == 0)
            return false;

        if (thing.Z > sector.FloorHeight)
            return false;

        return true;
    }
}
