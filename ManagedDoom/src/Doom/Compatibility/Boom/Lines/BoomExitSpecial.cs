namespace ManagedDoom.Compatibility.Boom.Lines;

public enum BoomExitType
{
    Normal,
    Secret
}

public readonly struct BoomExitSpecial
{
    public BoomExitSpecial(BoomExitType type, BoomTriggerType trigger)
    {
        Type = type;
        Trigger = trigger;
    }

    public BoomExitType Type { get; }

    public BoomTriggerType Trigger { get; }
}
