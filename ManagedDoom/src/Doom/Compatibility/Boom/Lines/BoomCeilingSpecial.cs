namespace ManagedDoom.Compatibility.Boom.Lines;

public enum BoomCeilingTarget
{
    HighestNeighborCeiling = 0,
    LowestNeighborCeiling = 1,
    NextNeighborCeiling = 2,
    HighestNeighborFloor = 3,
    Floor = 4,
    ShortestUpperTexture = 5,
    By24 = 6,
    By32 = 7,

    // Extended regular Boom-only target.
    FloorPlus8 = 8
}

public readonly struct BoomCeilingSpecial
{
    public BoomCeilingSpecial(
        BoomActionSpecification common,
        BoomPlaneDirection direction,
        BoomCeilingTarget target,
        BoomChangeType change,
        BoomModelType model,
        bool crush)
    {
        Common = common;
        Direction = direction;
        Target = target;
        Change = change;
        Model = model;
        Crush = crush;
    }

    public BoomActionSpecification Common { get; }
    public BoomTriggerType Trigger => Common.Trigger;
    public BoomActionSpeed Speed => Common.Speed;
    public bool AllowsMonsters => Common.AllowsMonsters;
    public bool Repeatable => Common.Repeatable;
    public bool UsesTagForTargeting => Common.UsesTagForTargeting;
    public BoomPlaneDirection Direction { get; }
    public BoomCeilingTarget Target { get; }
    public BoomChangeType Change { get; }
    public BoomModelType Model { get; }
    public bool Crush { get; }
}
