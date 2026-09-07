namespace ManagedDoom.Compatibility.Boom.Lines;

public readonly struct BoomStairSpecial
{
    public BoomStairSpecial(
        BoomActionSpecification common,
        BoomPlaneDirection direction,
        int stepSize,
        bool ignoreTexture)
    {
        Common = common;
        Direction = direction;
        StepSize = stepSize;
        IgnoreTexture = ignoreTexture;
    }

    public BoomActionSpecification Common { get; }
    public BoomTriggerType Trigger => Common.Trigger;
    public BoomActionSpeed Speed => Common.Speed;
    public bool AllowsMonsters => Common.AllowsMonsters;
    public bool Repeatable => Common.Repeatable;
    public bool UsesTagForTargeting => Common.UsesTagForTargeting;
    public BoomPlaneDirection Direction { get; }
    public int StepSize { get; }
    public bool IgnoreTexture { get; }
}
