using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Scrolling;

public static class BoomScrollerSpawner
{
    private const int ScrollShift = 5;
    private static readonly Fixed CarryFactor = new Fixed(Fixed.FracUnit * 3 / 32);

    public static void SpawnStaticScrollers(World world)
    {
        SpawnScrollers(world);
    }

    public static void SpawnScrollers(World world)
    {
        if (!GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility))
            return;

        var thinkers = world.Thinkers;

        foreach (var line in world.Map.Lines)
        {
            switch ((int)line.Special)
            {
                case 48:
                    AddSideScroller(thinkers, line.FrontSide, Fixed.One, Fixed.Zero);
                    break;

                case 85:
                    AddSideScroller(thinkers, line.FrontSide, -Fixed.One, Fixed.Zero);
                    break;

                case 214:
                    AddTaggedAccelerativePlaneScrollers(world, line, BoomScrollerType.Ceiling);
                    break;

                case 215:
                    AddTaggedAccelerativePlaneScrollers(world, line, BoomScrollerType.Floor);
                    break;

                case 216:
                    AddTaggedAccelerativeCarryScrollers(world, line);
                    break;

                case 217:
                    AddTaggedAccelerativePlaneScrollers(world, line, BoomScrollerType.Floor);
                    AddTaggedAccelerativeCarryScrollers(world, line);
                    break;

                case 218:
                    AddTaggedAccelerativeWallScrollers(world, line);
                    break;

                case 245:
                    AddTaggedDisplacementPlaneScrollers(world, line, BoomScrollerType.Ceiling);
                    break;

                case 246:
                    AddTaggedDisplacementPlaneScrollers(world, line, BoomScrollerType.Floor);
                    break;

                case 247:
                    AddTaggedDisplacementCarryScrollers(world, line);
                    break;

                case 248:
                    AddTaggedDisplacementPlaneScrollers(world, line, BoomScrollerType.Floor);
                    AddTaggedDisplacementCarryScrollers(world, line);
                    break;

                case 249:
                    AddTaggedDisplacementWallScrollers(world, line);
                    break;

                case 250:
                    AddTaggedPlaneScrollers(world, line, BoomScrollerType.Ceiling);
                    break;

                case 251:
                    AddTaggedPlaneScrollers(world, line, BoomScrollerType.Floor);
                    break;

                case 252:
                    AddTaggedCarryScrollers(world, line);
                    break;

                case 253:
                    AddTaggedPlaneScrollers(world, line, BoomScrollerType.Floor);
                    AddTaggedCarryScrollers(world, line);
                    break;

                case 254:
                    AddTaggedWallScrollers(world, line);
                    break;

                case 255:
                {
                    var side = line.FrontSide;
                    if (side != null)
                        AddSideScroller(thinkers, side, -side.TextureOffset, side.RowOffset);
                    break;
                }
            }
        }
    }

    private static void AddTaggedPlaneScrollers(World world, LineDef line, BoomScrollerType type)
    {
        var dx = -(line.Dx >> ScrollShift);
        var dy = line.Dy >> ScrollShift;

        if (dx == Fixed.Zero && dy == Fixed.Zero)
            return;

        foreach (var sector in world.Map.BoomTags.GetSectors(line.Tag))
            world.Thinkers.Add(new BoomScroller(type, sector, dx, dy));
    }

    private static void AddTaggedCarryScrollers(World world, LineDef line)
    {
        var dx = (line.Dx >> ScrollShift) * CarryFactor;
        var dy = (line.Dy >> ScrollShift) * CarryFactor;

        if (dx == Fixed.Zero && dy == Fixed.Zero)
            return;

        foreach (var sector in world.Map.BoomTags.GetSectors(line.Tag))
            world.Thinkers.Add(new BoomScroller(BoomScrollerType.Carry, sector, dx, dy));
    }

    private static void AddTaggedWallScrollers(World world, LineDef line)
    {
        var dx = line.Dx >> ScrollShift;
        var dy = line.Dy >> ScrollShift;

        if (dx == Fixed.Zero && dy == Fixed.Zero)
            return;

        foreach (var target in world.Map.BoomTags.GetLines(line.Tag))
        {
            if (object.ReferenceEquals(target, line) || target.FrontSide == null)
                continue;

            var vector = BoomScrollerVector.ResolveWall(dx, dy, target);
            if (vector.X == Fixed.Zero && vector.Y == Fixed.Zero)
                continue;

            world.Thinkers.Add(
                new BoomScroller(
                    BoomScrollerType.Side,
                    target.FrontSide,
                    vector.X,
                    vector.Y));
        }
    }

    private static void AddTaggedAccelerativePlaneScrollers(
        World world,
        LineDef line,
        BoomScrollerType type)
    {
        var controlSector = line.FrontSide?.Sector;
        if (controlSector == null)
            return;

        var dx = -(line.Dx >> ScrollShift);
        var dy = line.Dy >> ScrollShift;

        if (dx == Fixed.Zero && dy == Fixed.Zero)
            return;

        foreach (var sector in world.Map.BoomTags.GetSectors(line.Tag))
            world.Thinkers.Add(new BoomScroller(type, sector, controlSector, dx, dy, true));
    }

    private static void AddTaggedAccelerativeCarryScrollers(World world, LineDef line)
    {
        var controlSector = line.FrontSide?.Sector;
        if (controlSector == null)
            return;

        var dx = (line.Dx >> ScrollShift) * CarryFactor;
        var dy = (line.Dy >> ScrollShift) * CarryFactor;

        if (dx == Fixed.Zero && dy == Fixed.Zero)
            return;

        foreach (var sector in world.Map.BoomTags.GetSectors(line.Tag))
            world.Thinkers.Add(
                new BoomScroller(BoomScrollerType.Carry, sector, controlSector, dx, dy, true));
    }

    private static void AddTaggedAccelerativeWallScrollers(World world, LineDef line)
    {
        var controlSector = line.FrontSide?.Sector;
        if (controlSector == null)
            return;

        var dx = line.Dx >> ScrollShift;
        var dy = line.Dy >> ScrollShift;

        if (dx == Fixed.Zero && dy == Fixed.Zero)
            return;

        foreach (var target in world.Map.BoomTags.GetLines(line.Tag))
        {
            if (object.ReferenceEquals(target, line) || target.FrontSide == null)
                continue;

            var vector = BoomScrollerVector.ResolveWall(dx, dy, target);
            if (vector.X == Fixed.Zero && vector.Y == Fixed.Zero)
                continue;

            world.Thinkers.Add(
                new BoomScroller(
                    BoomScrollerType.Side,
                    target.FrontSide,
                    controlSector,
                    vector.X,
                    vector.Y,
                    true));
        }
    }

    private static void AddTaggedDisplacementPlaneScrollers(
        World world,
        LineDef line,
        BoomScrollerType type)
    {
        var controlSector = line.FrontSide?.Sector;
        if (controlSector == null)
            return;

        var dx = -(line.Dx >> ScrollShift);
        var dy = line.Dy >> ScrollShift;

        if (dx == Fixed.Zero && dy == Fixed.Zero)
            return;

        foreach (var sector in world.Map.BoomTags.GetSectors(line.Tag))
            world.Thinkers.Add(new BoomScroller(type, sector, controlSector, dx, dy));
    }

    private static void AddTaggedDisplacementCarryScrollers(World world, LineDef line)
    {
        var controlSector = line.FrontSide?.Sector;
        if (controlSector == null)
            return;

        var dx = (line.Dx >> ScrollShift) * CarryFactor;
        var dy = (line.Dy >> ScrollShift) * CarryFactor;

        if (dx == Fixed.Zero && dy == Fixed.Zero)
            return;

        foreach (var sector in world.Map.BoomTags.GetSectors(line.Tag))
            world.Thinkers.Add(new BoomScroller(BoomScrollerType.Carry, sector, controlSector, dx, dy));
    }

    private static void AddTaggedDisplacementWallScrollers(World world, LineDef line)
    {
        var controlSector = line.FrontSide?.Sector;
        if (controlSector == null)
            return;

        var dx = line.Dx >> ScrollShift;
        var dy = line.Dy >> ScrollShift;

        if (dx == Fixed.Zero && dy == Fixed.Zero)
            return;

        foreach (var target in world.Map.BoomTags.GetLines(line.Tag))
        {
            if (object.ReferenceEquals(target, line) || target.FrontSide == null)
                continue;

            var vector = BoomScrollerVector.ResolveWall(dx, dy, target);
            if (vector.X == Fixed.Zero && vector.Y == Fixed.Zero)
                continue;

            world.Thinkers.Add(
                new BoomScroller(
                    BoomScrollerType.Side,
                    target.FrontSide,
                    controlSector,
                    vector.X,
                    vector.Y));
        }
    }

    private static void AddSideScroller(Thinkers thinkers, SideDef side, Fixed dx, Fixed dy)
    {
        if (side == null || dx == Fixed.Zero && dy == Fixed.Zero)
            return;

        thinkers.Add(new BoomScroller(BoomScrollerType.Side, side, dx, dy));
    }
}
