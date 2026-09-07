namespace ManagedDoom.Compatibility.Boom.Lines;

public enum BoomDoorKind
{
    OpenWaitClose = 0,
    OpenStay = 1,
    CloseWaitOpen = 2,
    CloseStay = 3
}

public readonly struct BoomDoorSpecial
{
    public BoomDoorSpecial(BoomActionSpecification common, BoomDoorKind kind, int waitTics)
    {
        Common = common;
        Kind = kind;
        WaitTics = waitTics;
    }

    public BoomActionSpecification Common { get; }
    public BoomTriggerType Trigger => Common.Trigger;
    public BoomActionSpeed Speed => Common.Speed;
    public bool AllowsMonsters => Common.AllowsMonsters;
    public bool Repeatable => Common.Repeatable;
    public bool UsesTagForTargeting => Common.UsesTagForTargeting;
    public BoomDoorKind Kind { get; }
    public int WaitTics { get; }
}
