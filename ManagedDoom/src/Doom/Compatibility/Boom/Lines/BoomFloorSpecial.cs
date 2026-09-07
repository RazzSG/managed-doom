namespace ManagedDoom.Compatibility.Boom.Lines;

public enum BoomFloorTarget
{
    HighestNeighborFloor = 0,
    LowestNeighborFloor = 1,
    NextNeighborFloor = 2,
    LowestNeighborCeiling = 3,
    Ceiling = 4,
    ShortestLowerTexture = 5,
    By24 = 6,
    By32 = 7,

    // Extended regular Boom-only targets.
    By512 = 8,
    None = 9
}

public readonly struct BoomFloorSpecial
{
    public BoomFloorSpecial(
        BoomActionSpecification common,
        BoomPlaneDirection direction,
        BoomFloorTarget target,
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
    public BoomFloorTarget Target { get; }
    public BoomChangeType Change { get; }
    public BoomModelType Model { get; }
    public bool Crush { get; }
}
