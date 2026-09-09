using ManagedDoom.Compatibility.Mbf.Movement;

namespace ManagedDoom.Compatibility.Boom.Scrolling;

public sealed class BoomScroller : Thinker
{
    private BoomScrollerType type;
    private SideDef side;
    private Sector sector;
    private Sector controlSector;
    private Fixed dx;
    private Fixed dy;
    private Fixed lastHeight;
    private Fixed velocityDx;
    private Fixed velocityDy;
    private bool accelerative;

    public BoomScroller()
    {
    }

    public BoomScroller(BoomScrollerType type, SideDef side, Fixed dx, Fixed dy)
    {
        this.type = type;
        this.side = side;
        this.dx = dx;
        this.dy = dy;
    }

    public BoomScroller(BoomScrollerType type, Sector sector, Fixed dx, Fixed dy)
    {
        this.type = type;
        this.sector = sector;
        this.dx = dx;
        this.dy = dy;
    }

    public BoomScroller(
        BoomScrollerType type,
        SideDef side,
        Sector controlSector,
        Fixed dx,
        Fixed dy,
        bool accelerative = false)
        : this(type, side, dx, dy)
    {
        SetControlSector(controlSector);
        this.accelerative = accelerative;
    }

    public BoomScroller(
        BoomScrollerType type,
        Sector sector,
        Sector controlSector,
        Fixed dx,
        Fixed dy,
        bool accelerative = false)
        : this(type, sector, dx, dy)
    {
        SetControlSector(controlSector);
        this.accelerative = accelerative;
    }

    public override void Run()
    {
        var scrollDx = dx;
        var scrollDy = dy;

        if (controlSector != null)
        {
            var height = GetControlHeight(controlSector);
            var delta = height - lastHeight;
            lastHeight = height;

            scrollDx *= delta;
            scrollDy *= delta;
        }

        if (accelerative)
        {
            scrollDx += velocityDx;
            scrollDy += velocityDy;
            velocityDx = scrollDx;
            velocityDy = scrollDy;
        }

        if (scrollDx == Fixed.Zero && scrollDy == Fixed.Zero)
            return;

        switch (type)
        {
            case BoomScrollerType.Side:
                side.TextureOffset += scrollDx;
                side.RowOffset += scrollDy;
                break;

            case BoomScrollerType.Floor:
                sector.FloorXOffset += scrollDx;
                sector.FloorYOffset += scrollDy;
                break;

            case BoomScrollerType.Ceiling:
                sector.CeilingXOffset += scrollDx;
                sector.CeilingYOffset += scrollDy;
                break;

            case BoomScrollerType.Carry:
                CarryThings(scrollDx, scrollDy);
                break;
        }
    }

    private void SetControlSector(Sector value)
    {
        controlSector = value;
        lastHeight = GetControlHeight(value);
    }

    private static Fixed GetControlHeight(Sector value)
    {
        return value.FloorHeight + value.CeilingHeight;
    }

    private void CarryThings(Fixed scrollDx, Fixed scrollDy)
    {
        var floorHeight = sector.FloorHeight;

        // Boom scrollers use the sector's touching-thing list rather than its
        // subsector/origin thing list. An object can have its origin just across
        // a sector boundary while its radius still overlaps the conveyor floor;
        // it must continue receiving carry momentum until it no longer touches
        // the affected sector.
        for (var node = sector.TouchingThingList; node != null; node = node.SectorNext)
        {
            var thing = node.Thing;

            if ((thing.Flags & MobjFlags.NoClip) != 0)
                continue;

            // Boom's basic conveyor rule: carry clipped, gravity-affected things
            // that are on (or marginally below) the affected sector floor.
            // Deep-water carrying through a height sector remains a separate
            // compatibility-polish item.
            if ((thing.Flags & MobjFlags.NoGravity) != 0 || thing.Z > floorHeight)
                continue;

            thing.MomX += scrollDx;
            thing.MomY += scrollDy;
            MbfLedgeBlockCompatibility.MarkScrollingMovement(thing, scrollDx, scrollDy);
        }
    }

    public BoomScrollerType Type
    {
        get => type;
        set => type = value;
    }

    public SideDef Side
    {
        get => side;
        set => side = value;
    }

    public Sector Sector
    {
        get => sector;
        set => sector = value;
    }

    public Sector ControlSector
    {
        get => controlSector;
        set
        {
            controlSector = value;
            if (value != null)
                lastHeight = GetControlHeight(value);
        }
    }

    public Fixed Dx
    {
        get => dx;
        set => dx = value;
    }

    public Fixed Dy
    {
        get => dy;
        set => dy = value;
    }

    public Fixed LastHeight
    {
        get => lastHeight;
        set => lastHeight = value;
    }

    public Fixed VelocityDx
    {
        get => velocityDx;
        set => velocityDx = value;
    }

    public Fixed VelocityDy
    {
        get => velocityDy;
        set => velocityDy = value;
    }

    public bool IsAccelerative
    {
        get => accelerative;
        set => accelerative = value;
    }
}
