namespace ManagedDoom.Compatibility.Boom.Lines;

public enum BoomLiftTarget
{
    LowestNeighborFloor = 0,
    NextLowestNeighborFloor = 1,
    LowestNeighborCeiling = 2,
    LowestHighestFloorPerpetual = 3
}

public readonly struct BoomLiftSpecial
{
    public BoomLiftSpecial(BoomActionSpecification common, BoomLiftTarget target, int waitTics)
    {
        Common = common;
        Target = target;
        WaitTics = waitTics;
    }

    public BoomActionSpecification Common { get; }
    public BoomTriggerType Trigger => Common.Trigger;
    public BoomActionSpeed Speed => Common.Speed;
    public bool AllowsMonsters => Common.AllowsMonsters;
    public bool Repeatable => Common.Repeatable;
    public bool UsesTagForTargeting => Common.UsesTagForTargeting;
    public BoomLiftTarget Target { get; }
    public int WaitTics { get; }
    public bool IsPerpetual => Target == BoomLiftTarget.LowestHighestFloorPerpetual;
}
