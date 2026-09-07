namespace ManagedDoom.Compatibility.Boom.Sectors;

/// <summary>
/// Boom msecnode equivalent. A node participates in two linked lists:
/// sectors touched by one thing and things touching one sector.
/// </summary>
public sealed class BoomSectorTouchNode
{
    public Sector Sector { get; set; }
    public Mobj Thing { get; set; }

    public BoomSectorTouchNode ThingPrevious { get; set; }
    public BoomSectorTouchNode ThingNext { get; set; }

    public BoomSectorTouchNode SectorPrevious { get; set; }
    public BoomSectorTouchNode SectorNext { get; set; }

    public bool Visited { get; set; }

    internal bool Keep { get; set; }
}
