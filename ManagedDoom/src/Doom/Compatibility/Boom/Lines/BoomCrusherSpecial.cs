namespace ManagedDoom.Compatibility.Boom.Lines;

public readonly struct BoomCrusherSpecial
{
    public BoomCrusherSpecial(BoomActionSpecification common, bool silent)
    {
        Common = common;
        Silent = silent;
    }

    public BoomActionSpecification Common { get; }

    public BoomTriggerType Trigger => Common.Trigger;

    public BoomActionSpeed Speed => Common.Speed;

    public bool AllowsMonsters => Common.AllowsMonsters;

    public bool Repeatable => Common.Repeatable;

    public bool UsesTagForTargeting => Common.UsesTagForTargeting;

    public bool Silent { get; }
}
