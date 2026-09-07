namespace ManagedDoom.Compatibility.Boom.Lines;

public enum BoomExtendedStairAction
{
    Build8,
    Turbo16
}

public readonly struct BoomExtendedStairSpecial
{
    public BoomExtendedStairSpecial(BoomExtendedStairAction action, BoomTriggerType trigger)
    {
        Action = action;
        Trigger = trigger;
    }

    public BoomExtendedStairAction Action { get; }

    public BoomTriggerType Trigger { get; }

    public bool Repeatable => Trigger.IsRepeatable();
}
