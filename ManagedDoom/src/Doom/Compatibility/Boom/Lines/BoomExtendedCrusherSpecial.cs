namespace ManagedDoom.Compatibility.Boom.Lines;

public enum BoomExtendedCrusherAction
{
    Slow,
    Fast,
    Silent,
    Stop
}

public readonly struct BoomExtendedCrusherSpecial
{
    public BoomExtendedCrusherSpecial(BoomExtendedCrusherAction action, BoomTriggerType trigger)
    {
        Action = action;
        Trigger = trigger;
    }

    public BoomExtendedCrusherAction Action { get; }

    public BoomTriggerType Trigger { get; }

    public bool Repeatable => Trigger.IsRepeatable();
}
