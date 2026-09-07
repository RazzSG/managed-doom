namespace ManagedDoom.Compatibility.Boom.Lines;

public enum BoomLockedDoorKind
{
    OpenWaitClose = 0,
    OpenStay = 1
}

public enum BoomLockedDoorKey
{
    Any = 0,
    RedCard = 1,
    BlueCard = 2,
    YellowCard = 3,
    RedSkull = 4,
    BlueSkull = 5,
    YellowSkull = 6,
    All = 7
}

public readonly struct BoomLockedDoorSpecial
{
    public BoomLockedDoorSpecial(
        BoomActionSpecification common,
        BoomLockedDoorKind kind,
        BoomLockedDoorKey key,
        bool skullIsCard)
    {
        Common = common;
        Kind = kind;
        Key = key;
        SkullIsCard = skullIsCard;
    }

    public BoomActionSpecification Common { get; }
    public BoomTriggerType Trigger => Common.Trigger;
    public BoomActionSpeed Speed => Common.Speed;
    public bool Repeatable => Common.Repeatable;
    public bool UsesTagForTargeting => Common.UsesTagForTargeting;
    public BoomLockedDoorKind Kind { get; }
    public BoomLockedDoorKey Key { get; }
    public bool SkullIsCard { get; }
}
